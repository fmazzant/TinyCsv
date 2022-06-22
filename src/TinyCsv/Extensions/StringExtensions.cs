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

namespace TinyCsv.Extensions
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;
    using TinyCsv.Exceptions;

    /// <summary>
    /// String extensions
    /// </summary>
    public static class StringExtensions
    {
        /// <summary>
        /// Trim value if trimData is true
        /// </summary>
        /// <param name="value"></param>
        /// <param name="trimData"></param>
        /// <returns></returns>
        public static string TrimData<T>(this string value, CsvOptions<T> options)
        {
            if (!string.IsNullOrEmpty(value))
            {
                var chars = new List<char> { '"', '\'' };
                if (options.TrimData)
                {
                    chars.Add(' ');
                }
                return value.Trim(chars.ToArray());
            }
            return value;
        }

        /// <summary>
        /// Split line with delimiter separator and return list of values
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="line"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        public static string[] SplitLine<T>(this string line, CsvOptions<T> options)
        {
            var validateColumnCount = options.ValidateColumnCount;
            var allowBackSlashToEscapeQuote = options.AllowBackSlashToEscapeQuote;
            var columnCount = options.Columns.Count();
            var delimiter = options.Delimiter;

            var expression = $"{delimiter}(?=(?:[^\"]*\"[^\"]*\")*(?![^\"]*\"))";
            var regx = new Regex(expression);
            var values = regx.Split(line);

            if (validateColumnCount)
            {
                var endOfLineDelimiterChar = options.EndOfLineDelimiterChar;
                var endWithDelimiter = endOfLineDelimiterChar && line.EndsWith(delimiter);
                var valuesCount = values.Length - (endWithDelimiter ? 1 : 0);
                if (valuesCount != columnCount)
                {
                    throw new InvalidColumnCountException($"Invalid column count. Expected {columnCount} columns but found {valuesCount}.");
                }
            }

            if (!allowBackSlashToEscapeQuote)
            {
                values.ThrowsIfAnyEscapedQuotes();
            }

            return values;
        }

        /// <summary>
        /// Throw exception if any escaped quotes are found
        /// </summary>
        /// <param name="values"></param>
        /// <exception cref="InvalidColumnValueException"></exception>
        private static void ThrowsIfAnyEscapedQuotes(this string[] values)
        {
            var count = values.Count(x => x.Contains("\\\""));
            if (count > 0)
            {
                throw new InvalidColumnValueException($"Invalid column value. Found {count} columns with escaped quotes.");
            }
        }

        /// <summary>
        /// Enclosed in double quotes if the value contains the delimiter value
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        public static string EnclosedInQuotesIfNecessary<T>(this string value, CsvOptions<T> options)
        {
            var delimiterIsContined = value?.Contains(options.Delimiter) ?? false;
            var specialCharIsContined = value?.Contains("\"") ?? false;
            var encluseInQuotes = options.AllowRowEnclosedInDoubleQuotesValues && (delimiterIsContined || specialCharIsContined);
            return encluseInQuotes ? $"\"{value}\"" : value;
        }

        /// <summary>
        /// Defines that the line/row is to skip or not
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        public static bool SkipRow<T>(this string line, int index, CsvOptions<T> options)
        {
            var skipEmptyRows = options.SkipEmptyRows;
            var isEmpty = string.IsNullOrWhiteSpace(line);
            if (skipEmptyRows && isEmpty)
            {
                return true;
            }

            var allowComment = options.AllowComment;
            var isComment = line.StartsWith(options.Comment.ToString());
            if (isComment)
            {
                if (allowComment)
                {
                    return true;
                }
                else
                {
                    throw new NotAllowCommentException($"Invalid comment found at row {index}.");
                }
            }

            return options.SkipRow?.Invoke(line, index) ?? false;
        }
    }
}