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
    using System.Runtime.CompilerServices;
    using System.Text;
    using System.Threading;
    using TinyCsv.Exceptions;

    /// <summary>
    /// TinyCsvDataReader
    /// </summary>
    public class TinyCsvDataReader<T> : ICsvDataReader<T>
    {
        private static readonly string[] EmptyFields = new string[0];

        private readonly ICsvOptions options;
        private readonly StreamReader reader;
        private readonly bool quoting;
        private readonly bool backslash;
        private readonly bool trimData;
        private readonly char quote;
        private readonly char[] delimiter;
        private readonly StringBuilder builder = new StringBuilder();

        /// <summary>
        /// Create a TinyCsvDataReader instance
        /// </summary>
        /// <param name="options"></param>
        /// <param name="reader"></param>
        public TinyCsvDataReader(ICsvOptions options, StreamReader reader)
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
            string record;
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
            if (string.IsNullOrEmpty(line))
            {
                return EmptyFields;
            }

            if (line[0] == options.Comment && !options.AllowComment)
            {
                throw new NotAllowCommentException();
            }

            var span = line.AsSpan();
            var result = new List<string>(columnsCount > 0 ? columnsCount : 8);
            var position = 0;
            while (true)
            {
                result.Add(ReadField(span, ref position, out var hasDelimiter));
                if (!hasDelimiter)
                {
                    break;
                }
                if (position == span.Length)
                {
                    // "a;b;" with 3 columns: the last field is empty, it is not an end of line delimiter
                    if (result.Count < columnsCount)
                    {
                        result.Add(string.Empty);
                        break;
                    }
                    if (!options.EndOfLineDelimiterChar)
                    {
                        throw new EndOfLineDelimiterCharException($"The delimiter {options.Delimiter} in the end of line is not valid!");
                    }
                    break;
                }
            }

            return result.ToArray();
        }

        /// <summary>
        /// Read the field starting at position. On return position is after the delimiter, if any.
        /// Simple fields are sliced from the line, the others are unescaped by ReadEscapedField.
        /// </summary>
        private string ReadField(ReadOnlySpan<char> line, ref int position, out bool hasDelimiter)
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
                        return content.Slice(0, close).ToString();
                    }
                    if (IsDelimiterAt(line, after))
                    {
                        hasDelimiter = true;
                        position = after + delimiter.Length;
                        return content.Slice(0, close).ToString();
                    }
                }
                return ReadEscapedField(line, ref position, out hasDelimiter);
            }

            // fast path: unquoted value
            var rest = line.Slice(position);
            var end = rest.IndexOf(new ReadOnlySpan<char>(delimiter));
            var value = end >= 0 ? rest.Slice(0, end) : rest;
            if (backslash && value.IndexOf('\\') >= 0)
            {
                return ReadEscapedField(line, ref position, out hasDelimiter);
            }
            hasDelimiter = end >= 0;
            position += value.Length + (hasDelimiter ? delimiter.Length : 0);
            return (trimData ? TrimSpaces(value) : value).ToString();
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

        private static ReadOnlySpan<char> TrimSpaces(ReadOnlySpan<char> value)
        {
            var start = 0;
            var end = value.Length;
            while (start < end && IsSpace(value[start]))
            {
                start++;
            }
            while (end > start && IsSpace(value[end - 1]))
            {
                end--;
            }
            return value.Slice(start, end - start);
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


#if NET5_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
        /// <summary>
        /// Read the records. A record is a logical row: a quoted field may span multiple lines.
        /// </summary>
        /// <returns></returns>
        public async IAsyncEnumerable<string> ReadLinesAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var records = new CsvRecordReader(reader, options);
            var index = 0;
            string record;
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
#endif
    }
}
