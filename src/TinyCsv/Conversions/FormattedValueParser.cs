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
    using TinyCsv.Extensions;

    /// <summary>
    /// Parses a value with the format of the column (ParseExact) or with the culture (Parse)
    /// </summary>
    internal static class FormattedValueParser
    {
        /// <summary>
        /// Parses the value with a format
        /// </summary>
        public delegate bool TryParseExactHandler<TValue>(string value, string format, IFormatProvider provider, out TValue result);

        /// <summary>
        /// Parses the value without a format
        /// </summary>
        public delegate bool TryParseHandler<TValue>(string value, IFormatProvider provider, out TValue result);

        /// <summary>
        /// Converts a string to the target type: with the format of the column, if any, otherwise with the provider.
        /// An empty or invalid value returns null for a nullable target type, otherwise the default value.
        /// </summary>
        /// <typeparam name="TValue"></typeparam>
        /// <param name="value"></param>
        /// <param name="targetType"></param>
        /// <param name="provider"></param>
        /// <param name="tryParseExact"></param>
        /// <param name="tryParse"></param>
        /// <returns></returns>
        public static object ConvertBack<TValue>(string value, Type targetType, IFormatProvider provider,
            TryParseExactHandler<TValue> tryParseExact, TryParseHandler<TValue> tryParse)
            where TValue : struct
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                var parsed = provider is CsvColumn.DefaultFormatProvider defaultFormatProvider
                    ? tryParseExact(value, defaultFormatProvider.CustomFormat, provider, out var result)
                    : tryParse(value, provider, out result);
                if (parsed)
                {
                    return result;
                }
            }
            return targetType.IsNullable() ? null : (object)default(TValue);
        }
    }
}
