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

namespace TinyCsv.Conversions
{
    using System;
    using System.Globalization;
    using TinyCsv.Extensions;

    /// <summary>
    /// Provides a unified way of converting Int16 of values to string
    /// </summary>
    public sealed class Int16Converter : ParsableValueConverter<short>, IValueConverter
    {
        /// <summary>
        /// Parses a value that is not empty
        /// </summary>
        /// <param name="value">the value, not empty</param>
        /// <param name="provider">the format provider</param>
        /// <param name="result"></param>
        /// <returns></returns>
        protected override bool TryParse(string value, IFormatProvider provider, out short result)
        {
            // NumberStyles.Integer is much faster, and a value that it parses is the same with NumberStyles.Any
            return short.TryParse(value, NumberStyles.Integer, provider, out result)
                || short.TryParse(value, NumberStyles.Any, provider, out result);
        }

#if NETSTANDARD2_1 || NETCOREAPP
        /// <summary>
        /// Parses a value that is not empty, without creating a string
        /// </summary>
        internal override bool TryParseValue(ReadOnlySpan<char> value, IFormatProvider provider, out short result)
        {
            // NumberStyles.Integer is much faster, and a value that it parses is the same with NumberStyles.Any
            return short.TryParse(value, NumberStyles.Integer, provider, out result)
                || short.TryParse(value, NumberStyles.Any, provider, out result);
        }
#endif
    }
}
