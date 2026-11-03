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
#if NET8_0_OR_GREATER
    using System.Numerics;
    using System.Runtime.CompilerServices;
    using System.Runtime.Intrinsics;
#endif

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
        private readonly bool findDelimiters;

        private char[] buffer = new char[InitialBufferSize];
        private int[] delimiters;
        private int delimiterCount;
        private bool recordHasQuote;
        private bool recordRead;
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

#if NET8_0_OR_GREATER
            // with a single char delimiter (and without backslash escapes) the delimiters are found while looking for
            // the end of the record, in blocks of chars (see TryReadRecordAndDelimiters), so the fields are not scanned again
            findDelimiters = allowBackwardFieldStart && !backslash && delimiter.Length == 1
                && delimiter[0] != quote && delimiter[0] != '\r' && delimiter[0] != '\n';
            if (findDelimiters)
            {
                delimiters = new int[16];
            }
#endif
        }

        /// <summary>
        /// True when the positions of the delimiters of the current record are known (see Delimiters):
        /// the record has no quotes, so its fields are the chars between the delimiters.
        /// </summary>
        public bool HasDelimiters => findDelimiters && !recordHasQuote;

        /// <summary>
        /// The positions of the delimiters of the current record, from CurrentStart (see HasDelimiters)
        /// </summary>
        public int[] Delimiters => delimiters;

        /// <summary>
        /// The number of the delimiters of the current record (see HasDelimiters)
        /// </summary>
        public int DelimiterCount => delimiterCount;

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
#if NET8_0_OR_GREATER
            if (findDelimiters)
            {
                return TryReadRecordAndDelimiters();
            }
#endif
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

#if NET8_0_OR_GREATER
        private const int BlockSize = 32;
        private const int RecordFound = -1;
        private const int NeedData = -2;

        /// <summary>
        /// As TryReadRecord, finding also the delimiters of the record (see HasDelimiters).
        /// The chars are scanned in blocks of 32: a block is compared with the delimiter, the quote and the new lines
        /// at once, and only the chars found are handled one by one.
        /// </summary>
        /// <returns></returns>
        private bool TryReadRecordAndDelimiters()
        {
            if (recordRead)
            {
                // the first scan of a new record
                delimiterCount = 0;
                recordHasQuote = false;
                recordRead = false;
            }

            var d = delimiter[0];
            // without quoting the quote is not special: the delimiter is searched in its place
            var q = quoting ? quote : d;
            var i = scanPosition;
            while (i < length)
            {
                if (length - i >= BlockSize)
                {
                    var blockStart = i;
                    var found = SpecialChars(ref buffer[blockStart], d, q);
                    while (found != 0)
                    {
                        var position = blockStart + BitOperations.TrailingZeroCount(found);
                        found &= found - 1;
                        // a char already handled (i.e. the second quote of an escaped quote or the \n of \r\n)
                        if (position < i)
                        {
                            continue;
                        }
                        i = OnSpecialChar(position);
                        if (i < 0)
                        {
                            return i == RecordFound;
                        }
                    }
                    i = Math.Max(i, blockStart + BlockSize);
                    continue;
                }

                var c = buffer[i];
                if (c == d || c == q || c == '\r' || c == '\n')
                {
                    i = OnSpecialChar(i);
                    if (i < 0)
                    {
                        return i == RecordFound;
                    }
                }
                else
                {
                    i++;
                }
            }
            scanPosition = i;
            return false;
        }

        /// <summary>
        /// Handles the delimiter, the quote or the new line at the position, as TryReadRecord does.
        /// </summary>
        /// <param name="position"></param>
        /// <returns>the next position to scan, RecordFound or NeedData (then scanPosition is set)</returns>
        private int OnSpecialChar(int position)
        {
            var c = buffer[position];
            if (inQuotes)
            {
                // the delimiters and the new lines belong to the quoted field
                if (c != quote)
                {
                    return position + 1;
                }
                // a doubled quote is an escaped quote, otherwise it closes the field
                if (position + 1 < length)
                {
                    if (buffer[position + 1] == quote)
                    {
                        return position + 2;
                    }
                    inQuotes = false;
                    return position + 1;
                }
                if (endOfStream)
                {
                    inQuotes = false;
                    return position + 1;
                }
                scanPosition = position;
                return NeedData;
            }

            if (c == delimiter[0])
            {
                if (delimiterCount == delimiters.Length)
                {
                    Array.Resize(ref delimiters, delimiters.Length * 2);
                }
                delimiters[delimiterCount++] = position - recordStart;
                fieldStart = position + 1;
                return position + 1;
            }

            if (c == '\r' || c == '\n')
            {
                var next = position;
                if (c == '\r')
                {
                    if (position + 1 < length)
                    {
                        if (buffer[position + 1] == '\n') { next++; }
                    }
                    else if (!endOfStream)
                    {
                        scanPosition = position;
                        return NeedData;
                    }
                }
                CurrentStart = recordStart;
                CurrentLength = position - recordStart;
                recordStart = fieldStart = scanPosition = next + 1;
                recordRead = true;
                return RecordFound;
            }

            // the quote: it opens a quoted field only at the start of the field, elsewhere it is a literal char
            recordHasQuote = true;
            inQuotes = IsFieldStart(position);
            return position + 1;
        }

        /// <summary>
        /// The bits of the 32 chars from the reference that are the delimiter, the quote or a new line
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static uint SpecialChars(ref char chars, char delimiter, char quote)
        {
            ref var start = ref Unsafe.As<char, ushort>(ref chars);
            if (Vector512.IsHardwareAccelerated)
            {
                var v = Vector512.LoadUnsafe(ref start);
                var found = Vector512.Equals(v, Vector512.Create((ushort)delimiter))
                    | Vector512.Equals(v, Vector512.Create((ushort)quote))
                    | Vector512.Equals(v, Vector512.Create((ushort)'\r'))
                    | Vector512.Equals(v, Vector512.Create((ushort)'\n'));
                return (uint)found.ExtractMostSignificantBits();
            }
            if (Vector256.IsHardwareAccelerated)
            {
                return SpecialChars256(ref start, delimiter, quote)
                    | (SpecialChars256(ref Unsafe.Add(ref start, 16), delimiter, quote) << 16);
            }
            return SpecialChars128(ref start, delimiter, quote)
                | (SpecialChars128(ref Unsafe.Add(ref start, 8), delimiter, quote) << 8)
                | (SpecialChars128(ref Unsafe.Add(ref start, 16), delimiter, quote) << 16)
                | (SpecialChars128(ref Unsafe.Add(ref start, 24), delimiter, quote) << 24);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static uint SpecialChars256(ref ushort start, char delimiter, char quote)
        {
            var v = Vector256.LoadUnsafe(ref start);
            var found = Vector256.Equals(v, Vector256.Create((ushort)delimiter))
                | Vector256.Equals(v, Vector256.Create((ushort)quote))
                | Vector256.Equals(v, Vector256.Create((ushort)'\r'))
                | Vector256.Equals(v, Vector256.Create((ushort)'\n'));
            return found.ExtractMostSignificantBits();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static uint SpecialChars128(ref ushort start, char delimiter, char quote)
        {
            var v = Vector128.LoadUnsafe(ref start);
            var found = Vector128.Equals(v, Vector128.Create((ushort)delimiter))
                | Vector128.Equals(v, Vector128.Create((ushort)quote))
                | Vector128.Equals(v, Vector128.Create((ushort)'\r'))
                | Vector128.Equals(v, Vector128.Create((ushort)'\n'));
            return found.ExtractMostSignificantBits();
        }
#endif

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
            recordRead = true;
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
