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

    /// <summary>
    /// A field of a record: a slice of the record, or the unescaped value of a quoted field.
    /// The string of the value is created only when it is needed, so the numbers and the dates
    /// are parsed without creating a string.
    /// </summary>
    internal readonly struct CsvField
    {
        private readonly string text;
        private readonly char[] chars;
        private readonly int start;
        private readonly int length;

        /// <summary>
        /// A slice of a string
        /// </summary>
        public CsvField(string text, int start, int length)
        {
            this.text = text;
            this.chars = null;
            this.start = start;
            this.length = length;
        }

        /// <summary>
        /// A slice of the buffer of a reader: it is valid until the buffer changes, that is until the next record is read
        /// </summary>
        public CsvField(char[] chars, int start, int length)
        {
            this.text = null;
            this.chars = chars;
            this.start = start;
            this.length = length;
        }

        public CsvField(string value)
            : this(value, 0, value.Length)
        {
        }

        public static readonly CsvField Empty = new CsvField(string.Empty);

        /// <summary>
        /// The value, without creating a string
        /// </summary>
        public ReadOnlySpan<char> Span => chars != null
            ? new ReadOnlySpan<char>(chars, start, length)
            : text.AsSpan().Slice(start, length);

        /// <summary>
        /// The value, as a string
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            if (chars != null)
            {
                return length == 0 ? string.Empty : new string(chars, start, length);
            }
            return start == 0 && length == text.Length ? text : text.Substring(start, length);
        }
    }
}
