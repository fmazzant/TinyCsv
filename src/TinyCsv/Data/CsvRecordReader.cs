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

        private char[] buffer = new char[InitialBufferSize];
        private int length;
        private int recordStart;
        private int fieldStart;
        private int scanPosition;
        private bool inQuotes;
        private bool endOfStream;

        public CsvRecordReader(TextReader reader, ICsvOptions options)
        {
            this.reader = reader;
            quoting = options.AllowRowEnclosedInDoubleQuotesValues;
            backslash = options.AllowBackSlashToEscapeQuote;
            trimData = options.TrimData;
            quote = options.DoubleQuotes;
            delimiter = options.Delimiter.ToCharArray();

            var stopChars = new List<char> { '\r', '\n' };
            var quotedStopChars = new List<char> { quote };
            if (quoting)
            {
                // the delimiter is needed only to know where a field starts, that is where a quote opens a quoted field
                stopChars.Add(delimiter[0]);
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
            while (true)
            {
                if (TryReadRecord(out var record))
                {
                    return record;
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
        /// Read the next record, without the trailing new line. Returns null at the end of the stream.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<string> ReadRecordAsync(CancellationToken cancellationToken = default)
        {
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (TryReadRecord(out var record))
                {
                    return record;
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
        /// <param name="record"></param>
        /// <returns></returns>
        private bool TryReadRecord(out string record)
        {
            record = null;
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
                    inQuotes = IsFieldStart(i);
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
                    record = new string(buffer, recordStart, end - recordStart);
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
        /// Returns the last record, not terminated by a new line
        /// </summary>
        /// <returns></returns>
        private string ReadLastRecord()
        {
            if (recordStart >= length)
            {
                return null;
            }
            var record = new string(buffer, recordStart, length - recordStart);
            recordStart = fieldStart = scanPosition = length;
            inQuotes = false;
            return record;
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
