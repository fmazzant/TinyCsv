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
    using System;
    using System.Diagnostics;
    using System.Linq;

    /// <summary>
    /// String extensions
    /// </summary>
    public static partial class StringArrayExtensions
    {
        /// <summary>
        /// Get model from string array
        /// </summary>
        /// <param name="value"></param>
        /// <param name="trimData"></param>
        /// <returns></returns>
        public static T GetModelFromStringArray<T>(this string[] values, CsvOptions<T> options)
            where T : new()
        {
            var model = new T();

            var count = options.Columns.Count;
            for (int i = 0; i < count; i++)
            {
                var column = options.Columns.ElementAt(i);
                var value = values[column.ColumnIndex];
                var propertyName = column.ColumnNameInternal;
                var property = options.FasterProperties[propertyName];
                var typedValue = column.Converter.ConvertBack(value, column.ColumnType, null, column.ColumnFormatProvider);
                property.Setter(model, typedValue);
            }
            //foreach (var column in options.Columns)
            //{
            //    var value = values[column.ColumnIndex];
            //    var propertyName = column.ColumnNameInternal;
            //    var property = options.FasterProperties[propertyName];
            //    var typedValue = column.Converter.ConvertBack(value, column.ColumnType, null, column.ColumnFormatProvider);
            //    property.Setter(model, typedValue);
            //}

            return model;
        }
    }
}