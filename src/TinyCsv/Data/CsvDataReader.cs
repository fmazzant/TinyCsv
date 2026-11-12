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

namespace Mafesoft.TinyCsv.Data
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Runtime.CompilerServices;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Mafesoft.TinyCsv.Exceptions;

    /// <summary>
    /// TinyCsvDataReader
    /// </summary>
    public class TinyCsvDataReader<T> : ICsvDataReader<T>
    {
        private static readonly string[] EmptyFields = new string[0];

        private readonly ICsvOptions options;
        private readonly TextReader reader;
        private readonly bool quoting;
        private readonly bool backslash;
        private readonly bool trimData;
        private readonly char quote;
        private readonly char[] delimiter;
        private readonly StringBuilder builder = new StringBuilder();
        private CsvField[] fieldsBuffer = Array.Empty<CsvField>();

        /// <summary>
        /// Create a TinyCsvDataReader instance
        /// </summary>
        /// <param name="options"></param>
        /// <param name="reader"></param>
        public TinyCsvDataReader(ICsvOptions options, StreamReader reader)
            : this(options, (TextReader)reader)
        {
        }

        /// <summary>
        /// Create a TinyCsvDataReader instance
        /// </summary>
        /// <param name="options"></param>
        /// <param name="reader">i.e. a StreamReader or a StringReader</param>
        internal TinyCsvDataReader(ICsvOptions options, TextReader reader)
        {
            this.options = options;
            this.reader = reader;
            this.quoting = options.AllowRowEnclosedInDoubleQuotesValues;
            this.backslash = options.AllowBackSlashToEscapeQuote;
            this.trimData = options.TrimData;
            this.quote = options.DoubleQuotes;
            this.delimiter = options.Delimiter.ToCharArray();
        }

        /// <summary>
        /// Read the records. A record is a logical row: a quoted field may span multiple lines.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<string> ReadLines()
        {
            var records = new CsvRecordReader(reader, options);
            var index = 0;
            string? record;
            while ((record = records.ReadRecord()) != null)
            {
                if (IsToSkip(record, index++))
                {
                    continue;
                }

                yield return record;
            }
        }

        /// <summary>
        /// Get fields by line
        /// </summary>
        /// <param name="line"></param>
        /// <returns></returns>
        public string[] GetFieldsByLine(string line)
        {
            return GetFieldsByLine(line, 0);
        }

        /// <summary>
        /// Get fields by line
        /// </summary>
        /// <param name="line">the record, it may contain new lines inside quoted fields</param>
        /// <param name="columnsCount">the expected number of columns, used as initial capacity</param>
        /// <returns></returns>
        public string[] GetFieldsByLine(string line, int columnsCount)
        {
            var count = ReadFields(line, columnsCount, ref fieldsBuffer);
            if (count == 0)
            {
                return EmptyFields;
            }
            var result = new string[count];
            for (int i = 0; i < count; i++)
            {
                result[i] = fieldsBuffer[i].ToString();
            }
            return result;
        }

        /// <summary>
        /// Reads the fields of the line in a buffer, that is reused for all the lines
        /// </summary>
        /// <param name="line">the record, it may contain new lines inside quoted fields</param>
        /// <param name="columnsCount">the expected number of columns, used as initial capacity</param>
        /// <param name="fields">the buffer of the fields: it grows when it is too small</param>
        /// <returns>the number of fields</returns>
        internal int ReadFields(string line, int columnsCount, ref CsvField[] fields)
        {
            if (string.IsNullOrEmpty(line))
            {
                return 0;
            }
            return ReadFields(line.AsSpan(), new RecordSource(line, null, 0), columnsCount, ref fields);
        }

        /// <summary>
        /// Reads the fields of the record that is in a buffer (see CsvRecordReader.Read).
        /// The fields refer to the buffer, so they are valid until the next record is read.
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="start">the start of the record in the buffer</param>
        /// <param name="length">the length of the record</param>
        /// <param name="columnsCount">the expected number of columns, used as initial capacity</param>
        /// <param name="fields">the buffer of the fields: it grows when it is too small</param>
        /// <returns>the number of fields</returns>
        internal int ReadFields(char[] buffer, int start, int length, int columnsCount, ref CsvField[] fields)
        {
            if (length == 0)
            {
                return 0;
            }
            return ReadFields(new ReadOnlySpan<char>(buffer, start, length), new RecordSource(null, buffer, start), columnsCount, ref fields);
        }

        /// <summary>
        /// Reads the fields of the current record of the reader, when its delimiters are known (see CsvRecordReader.HasDelimiters),
        /// with the same result of ReadFields
        /// </summary>
        /// <param name="records"></param>
        /// <param name="columnsCount">the expected number of columns, used as initial capacity</param>
        /// <param name="fields">the buffer of the fields: it grows when it is too small</param>
        /// <returns>the number of fields</returns>
        internal int ReadFields(CsvRecordReader records, int columnsCount, ref CsvField[] fields)
        {
            var count = CountFields(records.Buffer, records.CurrentStart, records.CurrentLength, records.Delimiters, records.DelimiterCount, columnsCount);
            if (fields == null || fields.Length < count)
            {
                fields = new CsvField[Math.Max(count, Math.Max(columnsCount, 8))];
            }
            for (int index = 0; index < count; index++)
            {
                fields[index] = GetField(records, index);
            }
            return count;
        }

        /// <summary>
        /// The field at the index of the current record of the reader, when its delimiters are known (see CsvRecordReader.HasDelimiters):
        /// a quoted field is the value between the quotes, with the escaped quotes unescaped
        /// </summary>
        /// <param name="records"></param>
        /// <param name="index">the index of the field, less than the number of the fields</param>
        /// <returns></returns>
        internal CsvField GetField(CsvRecordReader records, int index)
        {
            var quotedFields = records.QuotedFields;
            for (int k = 0; k < records.QuotedFieldCount; k++)
            {
                var quoted = quotedFields[k];
                if (quoted >> 1 != index)
                {
                    continue;
                }
                var delimiters = records.Delimiters;
                var fieldStart = index == 0 ? 0 : delimiters[index - 1] + 1;
                var fieldEnd = index < records.DelimiterCount ? delimiters[index] : records.CurrentLength;
                // the value between the quotes, that are the first and the last char of the field
                var start = records.CurrentStart + fieldStart + 1;
                var length = fieldEnd - fieldStart - 2;
                return (quoted & 1) == 0
                    ? new CsvField(records.Buffer, start, length)
                    : new CsvField(Unescape(records.Buffer, start, length));
            }
            return GetField(records.Buffer, records.CurrentStart, records.CurrentLength, records.Delimiters, records.DelimiterCount, index);
        }

        /// <summary>
        /// The value of a quoted field with the doubled quotes as single quotes
        /// </summary>
        private string Unescape(char[] buffer, int start, int length)
        {
#if NET8_0_OR_GREATER
            // the quotes are doubled, so the value has half of them
            var quotes = new ReadOnlySpan<char>(buffer, start, length).Count(quote);
            return string.Create(length - quotes / 2, (buffer, start, length, quote), (chars, state) =>
            {
                var count = 0;
                var end = state.start + state.length;
                for (int i = state.start; i < end; i++)
                {
                    chars[count++] = state.buffer[i];
                    if (state.buffer[i] == state.quote)
                    {
                        // the second quote of a doubled quote
                        i++;
                    }
                }
            });
#else
            var chars = new char[length];
            var count = 0;
            var end = start + length;
            for (int i = start; i < end; i++)
            {
                chars[count++] = buffer[i];
                if (buffer[i] == quote)
                {
                    // the second quote of a doubled quote
                    i++;
                }
            }
            return new string(chars, 0, count);
#endif
        }

        /// <summary>
        /// The number of the fields of a record without quotes whose delimiters are known, as ReadFields:
        /// the fields are not created, see GetField.
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="start">the start of the record in the buffer</param>
        /// <param name="length">the length of the record</param>
        /// <param name="delimiters">the positions of the delimiters, from the start of the record</param>
        /// <param name="delimiterCount">the number of the delimiters</param>
        /// <param name="columnsCount">the expected number of columns</param>
        /// <returns></returns>
        internal int CountFields(char[] buffer, int start, int length, int[] delimiters, int delimiterCount, int columnsCount)
        {
            if (length == 0)
            {
                return 0;
            }
            if (buffer[start] == options.Comment && !options.AllowComment)
            {
                throw new NotAllowCommentException();
            }
            if (delimiterCount > 0 && delimiters[delimiterCount - 1] == length - 1)
            {
                // "a;b;" with 3 columns: the last field is empty, it is not an end of line delimiter
                if (delimiterCount < columnsCount)
                {
                    return delimiterCount + 1;
                }
                if (!options.EndOfLineDelimiterChar)
                {
                    throw new EndOfLineDelimiterCharException($"The delimiter {options.Delimiter} in the end of line is not valid!");
                }
                return delimiterCount;
            }
            return delimiterCount + 1;
        }

        /// <summary>
        /// The field at the index of a record without quotes whose delimiters are known (see CountFields)
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="start">the start of the record in the buffer</param>
        /// <param name="length">the length of the record</param>
        /// <param name="delimiters">the positions of the delimiters, from the start of the record</param>
        /// <param name="delimiterCount">the number of the delimiters</param>
        /// <param name="index">the index of the field, less than the number of the fields</param>
        /// <returns></returns>
        internal CsvField GetField(char[] buffer, int start, int length, int[] delimiters, int delimiterCount, int index)
        {
            var fieldStart = index == 0 ? 0 : delimiters[index - 1] + 1;
            var fieldEnd = index < delimiterCount ? delimiters[index] : length;
            var fieldLength = fieldEnd - fieldStart;
            fieldStart += start;
            if (trimData)
            {
                TrimSpaces(new ReadOnlySpan<char>(buffer), ref fieldStart, ref fieldLength);
            }
            return new CsvField(buffer, fieldStart, fieldLength);
        }

        private int ReadFields(ReadOnlySpan<char> span, RecordSource source, int columnsCount, ref CsvField[] fields)
        {
            if (span[0] == options.Comment && !options.AllowComment)
            {
                throw new NotAllowCommentException();
            }

            if (fields == null || fields.Length == 0)
            {
                fields = new CsvField[columnsCount > 0 ? columnsCount : 8];
            }

            var count = 0;
            var position = 0;
            while (true)
            {
                Add(ref fields, ref count, ReadField(source, span, ref position, out var hasDelimiter));
                if (!hasDelimiter)
                {
                    break;
                }
                if (position == span.Length)
                {
                    // "a;b;" with 3 columns: the last field is empty, it is not an end of line delimiter
                    if (count < columnsCount)
                    {
                        Add(ref fields, ref count, CsvField.Empty);
                        break;
                    }
                    if (!options.EndOfLineDelimiterChar)
                    {
                        throw new EndOfLineDelimiterCharException($"The delimiter {options.Delimiter} in the end of line is not valid!");
                    }
                    break;
                }
            }

            return count;
        }

        private static void Add(ref CsvField[] fields, ref int count, CsvField field)
        {
            if (count == fields.Length)
            {
                Array.Resize(ref fields, Math.Max(8, fields.Length * 2));
            }
            fields[count++] = field;
        }

        /// <summary>
        /// Read the field starting at position. On return position is after the delimiter, if any.
        /// Simple fields are sliced from the line, the others are unescaped by ReadEscapedField.
        /// </summary>
        private CsvField ReadField(RecordSource source, ReadOnlySpan<char> line, ref int position, out bool hasDelimiter)
        {
            var start = trimData ? SkipSpaces(line, position) : position;

            if (quoting && start < line.Length && line[start] == quote)
            {
                // fast path: "value" followed by the delimiter or the end of line, without escapes
                var content = line.Slice(start + 1);
                var close = content.IndexOf(quote);
                var escaped = close < 0
                    || (close + 1 < content.Length && content[close + 1] == quote)
                    || (backslash && content.Slice(0, close).IndexOf('\\') >= 0);
                if (!escaped)
                {
                    var after = start + 1 + close + 1;
                    if (trimData)
                    {
                        after = SkipSpaces(line, after);
                    }
                    if (after == line.Length)
                    {
                        hasDelimiter = false;
                        position = after;
                        return source.Slice(start + 1, close);
                    }
                    if (IsDelimiterAt(line, after))
                    {
                        hasDelimiter = true;
                        position = after + delimiter.Length;
                        return source.Slice(start + 1, close);
                    }
                }
                return new CsvField(ReadEscapedField(line, ref position, out hasDelimiter));
            }

            // fast path: unquoted value
            var rest = line.Slice(position);
            var end = delimiter.Length == 1 ? rest.IndexOf(delimiter[0]) : rest.IndexOf(new ReadOnlySpan<char>(delimiter));
            var value = end >= 0 ? rest.Slice(0, end) : rest;
            if (backslash && value.IndexOf('\\') >= 0)
            {
                return new CsvField(ReadEscapedField(line, ref position, out hasDelimiter));
            }
            hasDelimiter = end >= 0;
            var valueStart = position;
            var valueLength = value.Length;
            position += value.Length + (hasDelimiter ? delimiter.Length : 0);
            if (trimData)
            {
                TrimSpaces(line, ref valueStart, ref valueLength);
            }
            return source.Slice(valueStart, valueLength);
        }

        /// <summary>
        /// The record of the fields: a string, or a slice of the buffer of the reader
        /// </summary>
        private readonly struct RecordSource
        {
            private readonly string? text;
            private readonly char[]? chars;
            private readonly int offset;

            public RecordSource(string? text, char[]? chars, int offset)
            {
                this.text = text;
                this.chars = chars;
                this.offset = offset;
            }

            /// <summary>
            /// The field at the position of the record
            /// </summary>
            public CsvField Slice(int start, int length)
            {
                // text is set when chars is null
                return chars != null ? new CsvField(chars, offset + start, length) : new CsvField(text ?? string.Empty, start, length);
            }
        }

        /// <summary>
        /// Read a field handling doubled quotes and backslash escapes
        /// </summary>
        private string ReadEscapedField(ReadOnlySpan<char> line, ref int position, out bool hasDelimiter)
        {
            builder.Clear();
            hasDelimiter = false;

            var i = trimData ? SkipSpaces(line, position) : position;
            var quoted = quoting && i < line.Length && line[i] == quote;
            i = quoted ? i + 1 : position;

            var inQuotes = quoted;
            while (i < line.Length)
            {
                var c = line[i];
                if (backslash && c == '\\')
                {
                    if (i + 1 < line.Length)
                    {
                        builder.Append(line[i + 1]);
                        i += 2;
                    }
                    else
                    {
                        builder.Append(c);
                        i++;
                    }
                    continue;
                }

                if (inQuotes)
                {
                    if (c == quote)
                    {
                        if (i + 1 < line.Length && line[i + 1] == quote)
                        {
                            builder.Append(quote);
                            i += 2;
                        }
                        else
                        {
                            inQuotes = false;
                            i++;
                        }
                        continue;
                    }
                    builder.Append(c);
                    i++;
                    continue;
                }

                if (IsDelimiterAt(line, i))
                {
                    hasDelimiter = true;
                    i += delimiter.Length;
                    break;
                }

                // spaces after the closing quote
                if (quoted && trimData && IsSpace(c))
                {
                    i++;
                    continue;
                }

                builder.Append(c);
                i++;
            }

            position = i;
            var value = builder.ToString();
            return !quoted && trimData ? value.Trim(' ', '\t') : value;
        }

        private bool IsDelimiterAt(ReadOnlySpan<char> line, int position)
        {
            return line.Slice(position).StartsWith(new ReadOnlySpan<char>(delimiter));
        }

        private static bool IsSpace(char c)
        {
            return c == ' ' || c == '\t';
        }

        private static int SkipSpaces(ReadOnlySpan<char> line, int position)
        {
            while (position < line.Length && IsSpace(line[position]))
            {
                position++;
            }
            return position;
        }

        /// <summary>
        /// Removes the spaces at the start and at the end of the value, that is line[start..start+length]
        /// </summary>
        private static void TrimSpaces(ReadOnlySpan<char> line, ref int start, ref int length)
        {
            var end = start + length;
            while (start < end && IsSpace(line[start]))
            {
                start++;
            }
            while (end > start && IsSpace(line[end - 1]))
            {
                end--;
            }
            length = end - start;
        }

        /// <summary>
        /// Defines if the record is to skip: the first RowsToSkip records, the comments
        /// (when AllowComment, otherwise they are rejected by GetFieldsByLine) and the records selected by SkipRow
        /// </summary>
        /// <param name="record"></param>
        /// <param name="index">the record index, starting from 0</param>
        /// <returns></returns>
        private bool IsToSkip(string record, int index)
        {
            if (index < options.RowsToSkip)
            {
                return true;
            }
            if (options.AllowComment && record.Length > 0 && record[0] == options.Comment)
            {
                return true;
            }
            return options.SkipRow?.Invoke(record, index) ?? false;
        }

        /// <summary>
        /// Get lines field
        /// </summary>
        /// <returns></returns>
        public IEnumerable<string[]> ReadLinesAndFields()
        {
            foreach (var line in ReadLines())
            {
                if (!string.IsNullOrWhiteSpace(line))
                {
                    var fields = GetFieldsByLine(line);
                    yield return fields;
                }
            }
        }


        /// <summary>
        /// Read the records. A record is a logical row: a quoted field may span multiple lines.
        /// </summary>
        /// <returns></returns>
        public async IAsyncEnumerable<string> ReadLinesAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var records = new CsvRecordReader(reader, options);
            var index = 0;
            string? record;
            while ((record = await records.ReadRecordAsync(cancellationToken).ConfigureAwait(false)) != null)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (IsToSkip(record, index++))
                {
                    continue;
                }

                yield return record;
            }
        }

        /// <summary>
        /// Get lines field asynchronously
        /// </summary>
        /// <returns></returns>
        public async IAsyncEnumerable<string[]> ReadLinesAndFieldsAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await foreach (var line in ReadLinesAsync(cancellationToken).ConfigureAwait(false))
            {
                if (!string.IsNullOrWhiteSpace(line))
                {
                    yield return GetFieldsByLine(line);
                }
            }
        }
    }
}
