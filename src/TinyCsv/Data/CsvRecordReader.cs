/// <summary>
///
/// The MIT License (MIT)
///
/// Copyright (c) 2022 Federico Mazzanti
///
/// Permission is hereby granted, free of charge, to any person
/// obtaining a copy of this software and associated documentation
/// files (the "Software"), to deal in the Software without
/// restriction, including without limitation the rights to use,
/// copy, modify, merge, publish, distribute, sublicense, and/or sell
/// copies of the Software, and to permit persons to whom the
/// Software is furnished to do so, subject to the following
/// conditions:
///
/// The above copyright notice and this permission notice shall be
/// included in all copies or substantial portions of the Software.
///
/// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
/// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
/// OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
/// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
/// HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
/// WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
/// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
/// OTHER DEALINGS IN THE SOFTWARE.
///
/// </summary>

namespace TinyCsv.Data
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Reads logical CSV records from a TextReader.
    /// A record ends at a new line (\n, \r\n or \r) that is not inside a quoted field,
    /// so a quoted field may span multiple physical lines.
    /// The current record is always kept contiguous in the buffer, which grows only
    /// when a single record is larger than the buffer.
    /// </summary>
    internal sealed class CsvRecordReader
    {
        private const int InitialBufferSize = 4096;

        private readonly TextReader reader;
        private readonly bool quoting;
        private readonly bool backslash;
        private readonly bool trimData;
        private readonly char quote;
        private readonly char[] delimiter;
        private readonly char[] stops;
        private readonly char[] quotedStops;
        private readonly bool backwardFieldStart;

        private char[] buffer = new char[InitialBufferSize];
        private int length;
        private int recordStart;
        private int fieldStart;
        private int scanPosition;
        private bool inQuotes;
        private bool endOfStream;

        public CsvRecordReader(TextReader reader, ICsvOptions options)
            : this(reader, options, true)
        {
        }

        /// <summary>
        /// Create a CsvRecordReader
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="options"></param>
        /// <param name="allowBackwardFieldStart">false to always find the start of the fields scanning the delimiters (used by the tests)</param>
        internal CsvRecordReader(TextReader reader, ICsvOptions options, bool allowBackwardFieldStart)
        {
            this.reader = reader;
            quoting = options.AllowRowEnclosedInDoubleQuotesValues;
            backslash = options.AllowBackSlashToEscapeQuote;
            trimData = options.TrimData;
            quote = options.DoubleQuotes;
            delimiter = options.Delimiter.ToCharArray();

            // with a single char delimiter (and without backslash escapes) the start of a field is found
            // looking back from a quote (see IsFieldStartBackward), so the scan does not stop at each delimiter
            backwardFieldStart = allowBackwardFieldStart && quoting && !backslash && delimiter.Length == 1
                && delimiter[0] != quote && delimiter[0] != '\r' && delimiter[0] != '\n'
                && delimiter[0] != ' ' && delimiter[0] != '\t';

            var stopChars = new List<char> { '\r', '\n' };
            var quotedStopChars = new List<char> { quote };
            if (quoting)
            {
                // the delimiter is needed only to know where a field starts, that is where a quote opens a quoted field
                if (!backwardFieldStart)
                {
                    stopChars.Add(delimiter[0]);
                }
                stopChars.Add(quote);
            }
            if (backslash)
            {
                stopChars.Add('\\');
                quotedStopChars.Add('\\');
            }
            stops = stopChars.ToArray();
            quotedStops = quotedStopChars.ToArray();
        }

        /// <summary>
        /// Read the next record, without the trailing new line. Returns null at the end of the stream.
        /// </summary>
        /// <returns></returns>
        public string ReadRecord()
        {
            return Read() ? CurrentToString() : null;
        }

        /// <summary>
        /// Read the next record, without the trailing new line. Returns null at the end of the stream.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<string> ReadRecordAsync(CancellationToken cancellationToken = default)
        {
            return await ReadAsync(cancellationToken).ConfigureAwait(false) ? CurrentToString() : null;
        }

        /// <summary>
        /// The buffer of the current record (see Read): it is valid until the next read
        /// </summary>
        public char[] Buffer => buffer;

        /// <summary>
        /// The start of the current record in the Buffer
        /// </summary>
        public int CurrentStart { get; private set; }

        /// <summary>
        /// The length of the current record, without the trailing new line
        /// </summary>
        public int CurrentLength { get; private set; }

        /// <summary>
        /// The current record, as a string
        /// </summary>
        /// <returns></returns>
        public string CurrentToString()
        {
            return new string(buffer, CurrentStart, CurrentLength);
        }

        /// <summary>
        /// Read the next record without creating a string: the record is in Buffer, from CurrentStart for CurrentLength chars,
        /// until the next read. Returns false at the end of the stream.
        /// </summary>
        /// <returns></returns>
        public bool Read()
        {
            while (true)
            {
                if (TryReadRecord())
                {
                    return true;
                }
                if (endOfStream)
                {
                    return ReadLastRecord();
                }
                PrepareBuffer();
                OnRead(reader.Read(buffer, length, buffer.Length - length));
            }
        }

        /// <summary>
        /// Read the next record without creating a string (see Read). Returns false at the end of the stream.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<bool> ReadAsync(CancellationToken cancellationToken = default)
        {
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (TryReadRecord())
                {
                    return true;
                }
                if (endOfStream)
                {
                    return ReadLastRecord();
                }
                PrepareBuffer();
                OnRead(await reader.ReadAsync(buffer, length, buffer.Length - length).ConfigureAwait(false));
            }
        }

        /// <summary>
        /// Scan the buffered chars looking for the end of the current record.
        /// Returns false when more data is needed.
        /// </summary>
        /// <returns></returns>
        private bool TryReadRecord()
        {
            var i = scanPosition;
            while (i < length)
            {
                var offset = new ReadOnlySpan<char>(buffer, i, length - i).IndexOfAny(new ReadOnlySpan<char>(inQuotes ? quotedStops : stops));
                if (offset < 0)
                {
                    i = length;
                    break;
                }
                i += offset;
                var c = buffer[i];

                if (backslash && c == '\\')
                {
                    // the escaped char, whatever it is, belongs to the field
                    if (i + 1 < length) { i += 2; continue; }
                    if (endOfStream) { i++; continue; }
                    break;
                }

                if (inQuotes)
                {
                    // c is the quote: a doubled quote is an escaped quote, otherwise it closes the field
                    if (i + 1 < length)
                    {
                        if (buffer[i + 1] == quote) { i += 2; }
                        else { inQuotes = false; i++; }
                        continue;
                    }
                    if (endOfStream) { inQuotes = false; i++; continue; }
                    break;
                }

                if (quoting && c == quote)
                {
                    // a quote opens a quoted field only at the start of the field, elsewhere it is a literal char
                    inQuotes = backwardFieldStart ? IsFieldStartBackward(i) : IsFieldStart(i);
                    i++;
                    continue;
                }

                if (c == '\r' || c == '\n')
                {
                    var end = i;
                    if (c == '\r')
                    {
                        if (i + 1 < length)
                        {
                            if (buffer[i + 1] == '\n') { i++; }
                        }
                        else if (!endOfStream)
                        {
                            break;
                        }
                    }
                    CurrentStart = recordStart;
                    CurrentLength = end - recordStart;
                    recordStart = fieldStart = scanPosition = i + 1;
                    return true;
                }

                // first char of the delimiter
                if (i + delimiter.Length <= length)
                {
                    if (new ReadOnlySpan<char>(buffer, i, delimiter.Length).StartsWith(new ReadOnlySpan<char>(delimiter)))
                    {
                        i += delimiter.Length;
                        fieldStart = i;
                    }
                    else
                    {
                        i++;
                    }
                    continue;
                }
                if (endOfStream) { i++; continue; }
                break;
            }
            scanPosition = i;
            return false;
        }

        /// <summary>
        /// True when only (trimmable) spaces are between the field start and the position
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        private bool IsFieldStart(int position)
        {
            for (int k = fieldStart; k < position; k++)
            {
                if (!trimData || (buffer[k] != ' ' && buffer[k] != '\t'))
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// As IsFieldStart, looking back from the position: only (trimmable) spaces are between
        /// the start of the record or the previous delimiter and the position.
        /// The previous delimiter is not inside a quoted field, because a quoted field is closed by a quote
        /// that would be between the delimiter and the position.
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        private bool IsFieldStartBackward(int position)
        {
            var k = position - 1;
            if (trimData)
            {
                while (k >= recordStart && (buffer[k] == ' ' || buffer[k] == '\t'))
                {
                    k--;
                }
            }
            return k < recordStart || buffer[k] == delimiter[0];
        }

        /// <summary>
        /// Reads the last record, not terminated by a new line. Returns false when there is not.
        /// </summary>
        /// <returns></returns>
        private bool ReadLastRecord()
        {
            if (recordStart >= length)
            {
                return false;
            }
            CurrentStart = recordStart;
            CurrentLength = length - recordStart;
            recordStart = fieldStart = scanPosition = length;
            inQuotes = false;
            return true;
        }

        /// <summary>
        /// Move the current record to the beginning of the buffer and grow it if it is full
        /// </summary>
        private void PrepareBuffer()
        {
            if (recordStart > 0)
            {
                var pending = length - recordStart;
                Array.Copy(buffer, recordStart, buffer, 0, pending);
                length = pending;
                scanPosition -= recordStart;
                fieldStart -= recordStart;
                recordStart = 0;
            }
            if (length == buffer.Length)
            {
                Array.Resize(ref buffer, buffer.Length * 2);
            }
        }

        private void OnRead(int read)
        {
            if (read == 0)
            {
                endOfStream = true;
            }
            else
            {
                length += read;
            }
        }
    }
}
