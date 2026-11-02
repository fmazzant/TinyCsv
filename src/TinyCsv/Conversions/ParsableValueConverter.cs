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
    /// The base of the converters of a value type that is parsed from a string, i.e. numbers and dates.
    /// An empty value is converted to null for a nullable type, otherwise to the default value,
    /// and a value that cannot be parsed is not valid (see CsvOptions.ValidateValues).
    /// </summary>
    /// <typeparam name="TValue"></typeparam>
    public abstract class ParsableValueConverter<TValue> : DefaultValueConverter
        where TValue : struct
    {
        /// <summary>
        /// Converts a string to target type value: a value that cannot be parsed returns the default value.
        /// </summary>
        /// <param name="value">The value that is produced by the binding target.</param>
        /// <param name="targetType">The type to convert to.</param>
        /// <param name="parameter">The converter parameter to use.</param>
        /// <param name="provider">The format provider.</param>
        /// <returns>A converted value. If the method returns null, the valid null value is used.</returns>
        public sealed override object ConvertBack(string value, Type targetType, object parameter, IFormatProvider provider)
        {
            TryConvertBack(value, targetType, parameter, provider, out var result);
            return result;
        }

        /// <summary>
        /// Converts a string to target type value, returning false when the value cannot be parsed.
        /// </summary>
        /// <param name="value">The value that is produced by the binding target.</param>
        /// <param name="targetType">The type to convert to.</param>
        /// <param name="parameter">The converter parameter to use.</param>
        /// <param name="provider">The format provider.</param>
        /// <param name="result">The converted value, or the default value when the value is not valid.</param>
        /// <returns>false when the value is not empty and it cannot be parsed</returns>
        public sealed override bool TryConvertBack(string value, Type targetType, object parameter, IFormatProvider provider, out object result)
        {
            var isEmpty = string.IsNullOrWhiteSpace(value);
            if (!isEmpty && TryParse(value, provider, out var parsed))
            {
                result = parsed;
                return true;
            }
            result = targetType.IsNullable() ? null : (object)default(TValue);
            return isEmpty;
        }

        /// <summary>
        /// Parses a value that is not empty
        /// </summary>
        /// <param name="value">the value, not empty</param>
        /// <param name="provider">the format provider: a column with a format uses CsvColumn.DefaultFormatProvider</param>
        /// <param name="result"></param>
        /// <returns></returns>
        protected abstract bool TryParse(string value, IFormatProvider provider, out TValue result);

        /// <summary>
        /// Parses a value that is not empty, without boxing the result (see CsvColumnAccessor)
        /// </summary>
        /// <param name="value">the value, not empty</param>
        /// <param name="provider">the format provider</param>
        /// <param name="result"></param>
        /// <returns></returns>
        internal bool TryParseValue(string value, IFormatProvider provider, out TValue result)
        {
            return TryParse(value, provider, out result);
        }

        /// <summary>
        /// Parses a value that is not empty, without boxing the result (see CsvColumnAccessor).
        /// The built-in converters parse the span, the others parse it as a string.
        /// </summary>
        /// <param name="value">the value, not empty</param>
        /// <param name="provider">the format provider</param>
        /// <param name="result"></param>
        /// <returns></returns>
        internal virtual bool TryParseValue(ReadOnlySpan<char> value, IFormatProvider provider, out TValue result)
        {
            return TryParse(value.ToString(), provider, out result);
        }

        /// <summary>
        /// Returns the format of the column, if any
        /// </summary>
        /// <param name="provider"></param>
        /// <returns></returns>
        protected static string GetColumnFormat(IFormatProvider provider)
        {
            return (provider as CsvColumn.DefaultFormatProvider)?.CustomFormat;
        }
    }
}
