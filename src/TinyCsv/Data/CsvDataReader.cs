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
    using System.Collections.Generic;
    using System.IO;
    using System.Runtime.CompilerServices;
    using System.Text;
    using System.Threading;
    using TinyCsv.Exceptions;
    using TinyCsv.Extensions;

    /// <summary>
    /// TinyCsvDataReader
    /// </summary>
    public class TinyCsvDataReader<T> : ICsvDataReader<T>
    {
        private readonly ICsvOptions options;
        private readonly StreamReader reader;

        /// <summary>
        /// Create a TinyCsvDataReader instance
        /// </summary>
        /// <param name="options"></param>
        /// <param name="reader"></param>
        public TinyCsvDataReader(ICsvOptions options, StreamReader reader)
        {
            this.options = options;
            this.reader = reader;
        }

        /// <summary>
        /// Read Lines 
        /// </summary>
        /// <returns></returns>
        public IEnumerable<string> ReadLines()
        {
            var index = 0;
            var line = string.Empty;
            while ((line = reader.ReadLine()) != null)
            {
                if (options.RowsToSkip > 0 && options.RowsToSkip > index++)
                {
                    continue;
                }

                if (options.SkipRow(line, index++))
                {
                    continue;
                }

                yield return line;
            }
        }

        /// <summary>
        /// Get fields by line
        /// </summary>
        /// <param name="line"></param>
        /// <returns></returns>
        public string[] GetFieldsByLine(string line)
        {
            var result = new List<string>();
            var sb = new StringBuilder(string.Empty);
            bool inQuotes = false;

            var charArrayLenght = line.Length;
            char[] charArray = line.ToCharArray();

            var allowBackSlash = options.AllowBackSlashToEscapeQuote;
            var allowRowEnclosed = options.AllowRowEnclosedInDoubleQuotesValues;
            var delimiter = options.Delimiter[0];
            var isComment = charArray[0] == options.Comment;

            if (isComment && !options.AllowComment)
            {
                throw new NotAllowCommentException();
            }

            for (int i = 0; i < charArrayLenght; i++)
            {
                var isLast = i == charArrayLenght - 1;
                var isBackslash = charArray[i] == '\\' && allowBackSlash;
                var isQuotes = charArray[i] == '\"' && allowRowEnclosed;
                var isDelimiter = charArray[i] == delimiter;

                if (isQuotes)
                {
                    if (!isLast && charArray[i + 1] == '\"')
                    {
                        sb.Append(charArray[i]);
                    }
                    else
                    {
                        inQuotes = !inQuotes;
                    }
                }
                else if (isBackslash)
                {
                    sb.Append(charArray[i + 1]);
                    i++;
                }
                else if (isDelimiter)
                {
                    if (i < charArrayLenght - 1)
                    {
                        if (!inQuotes)
                        {
                            result.Add(sb.ToString().TrimData(options));
                            sb.Clear();
                        }
                        else
                        {
                            sb.Append(charArray[i]);
                        }
                    }
                    else
                    {
                        if (!options.EndOfLineDelimiterChar)
                        {
                            throw new EndOfLineDelimiterCharException($"The delimiter {options.Delimiter} in the end of line is not valid!");
                        }
                    }
                }
                else
                {
                    sb.Append(charArray[i]);
                }
            }

            result.Add(sb.ToString().TrimData(options));

            if (options.EndOfLineDelimiterChar)
            {
                result.Add(string.Empty);
            }

            return result.ToArray();
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
        /// Read Lines 
        /// </summary>
        /// <returns></returns>
        public async IAsyncEnumerable<string> ReadLinesAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var index = 0;
            var line = string.Empty;
            while ((line = await reader.ReadLineAsync()) != null)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (options.RowsToSkip > 0 && options.RowsToSkip > index++)
                {
                    continue;
                }

                if (options.SkipRow(line, index))
                {
                    continue;
                }

                yield return line;
            }
        }
#endif
    }
}