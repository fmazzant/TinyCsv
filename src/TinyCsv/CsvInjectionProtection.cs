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

namespace Mafesoft.TinyCsv
{
    /// <summary>
    /// How the writer protects the text values from CSV injection (formula injection): a value that starts with
    /// one of <see cref="CsvOptions.InjectionCharacters"/> (i.e. <c>=</c>, <c>+</c>, <c>-</c>, <c>@</c>) is run as a formula
    /// by Excel and LibreOffice. Only the text values (the columns of type string) are checked, so the numbers are written as they are.
    /// </summary>
    public enum CsvInjectionProtection
    {
        /// <summary>
        /// The values are written as they are (default)
        /// </summary>
        None = 0,

        /// <summary>
        /// The value is prefixed with an apostrophe (<c>'</c>) and enclosed in quotes, so it is shown as text
        /// </summary>
        Escape = 1,

        /// <summary>
        /// The leading injection characters are removed from the value
        /// </summary>
        Strip = 2,

        /// <summary>
        /// A <see cref="Exceptions.CsvInjectionException"/> is thrown, with the row and the column of the value
        /// </summary>
        Throw = 3,
    }
}
