
using System.Linq;
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
    /// <summary>
    /// Generic Type extensions
    /// </summary>
    public static class GenericTypeExtensions
    {
        /// <summary>
        /// Get fields from model
        /// </summary>
        /// <param name="value"></param>
        /// <param name="trimData"></param>
        /// <returns></returns>
        public static string[] GetFieldsFromGenericType<T>(this T model, CsvOptions<T> options)
            where T : new()
        {
            var values = options.Columns.Select(column =>
            {
                var propertyName = column.ColumnNameInternal;
                //var property = options.Properties[propertyName];
                //var value = property.GetValue(model);

                var property = options.FasterProperties[propertyName];
                var value = property.Getter(model);

                var stringValue = column.Converter.Convert(value, null, column.ColumnFormatProvider);
                return stringValue?.EnclosedInQuotesIfNecessary(options);
            });
            return values.ToArray();
        }

        /// <summary>
        /// Get line from model
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="model"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        public static string GetLineFromGenericType<T>(this T model, CsvOptions<T> options)
            where T : new()
        {
            var values = model.GetFieldsFromGenericType(options);
            var line = string.Join(options.Delimiter, values);
            return line;
        }
    }
}