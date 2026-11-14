/// <summary>
/// 
/// The MIT License (MIT)
/// 
/// Copyright (c) 2020 Federico Mazzanti
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

namespace Mafesoft.TinyCsv
{
    /// <summary>
    /// When the writer encloses the fields in quotes (see CsvOptions.QuoteMode)
    /// </summary>
    public enum CsvQuoteMode
    {
        /// <summary>
        /// Only the fields that contain the delimiter, a quote or a new line are enclosed in quotes, as RFC 4180 (default)
        /// </summary>
        Minimal = 0,

        /// <summary>
        /// Every field is enclosed in quotes, the header too. A null value is an empty field without quotes
        /// (see CsvOptions.NullValueOnWrite), while an empty string is <c>""</c>.
        /// </summary>
        All = 1,

        /// <summary>
        /// The fields of the columns that are not numbers (text, dates, booleans, ...) are enclosed in quotes, the header too.
        /// The numbers are quoted only when necessary, as <see cref="Minimal"/>.
        /// </summary>
        NonNumeric = 2,

        /// <summary>
        /// The fields are never enclosed in quotes: a value that contains the delimiter or a new line
        /// throws an InvalidColumnValueException, because the file could not be read back.
        /// </summary>
        None = 3,
    }
}
