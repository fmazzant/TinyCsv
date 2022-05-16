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
    using System.Text;

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
            var columnNumber = options.Columns.Count();
            var values = new string[columnNumber];
            var delimiter = options.Delimiter;
            var doubleQuotes = options.DoubleQuotes;
            int index = 0;
            int cursor = 0;

            bool openDoubleQuotes = false;
            while (index < columnNumber)
            {
                var sb = new StringBuilder();

                while (cursor < line.Length)
                {
                    var c = line[cursor++];

                    if (c == doubleQuotes)
                    {
                        openDoubleQuotes = !openDoubleQuotes;
                    }

                    if (c.ToString() == delimiter && !openDoubleQuotes)
                    {
                        break;
                    }
                    else
                    {
                        sb.Append(c);
                    }
                }

                values[index++] = sb.ToString();
            }

            return values;
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
            var encluseInQuotes = options.AllowRowEnclosedInDoubleQuotesValues && (value?.Contains(options.Delimiter) ?? false);
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
            var isEmpty = string.IsNullOrWhiteSpace(line);
            if (options.SkipEmptyRows && isEmpty)
            {
                return true;
            }

            var isComment = options.AllowComment && line.StartsWith(options.Comment.ToString());
            if (isComment)
            {
                return true;
            }

            return options.SkipRow?.Invoke(line, index) ?? false;
        }
    }
}
