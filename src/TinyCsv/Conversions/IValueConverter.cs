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

namespace Mafesoft.TinyCsv.Conversions
{
    using System;

    /// <summary>
    /// Defines the interface to convert the types of values to string and the string to types of values.
    /// <para>
    /// Writing: <see cref="Convert"/> receives the value of the property, that is null when the property is null,
    /// and it may return null: null and an empty string are written as an empty field.
    /// </para>
    /// <para>
    /// Reading: <see cref="ConvertBack"/> receives the text of the field, that is never null:
    /// an empty field is an empty string. It may return null (i.e. for a reference or a nullable type),
    /// that is assigned to the property.
    /// </para>
    /// </summary>
    public interface IValueConverter
    {
        /// <summary>
        /// Converts a value to string.
        /// </summary>
        /// <param name="value">The value of the property: null when the property is null.</param>
        /// <param name="parameter">The converter parameter: TinyCsv passes null.</param>
        /// <param name="provider">The IFormatProvider to use in the converter: the ColumnFormatProvider of the column,
        /// or the culture of the options with the format of the column, if any. It is never null.</param>
        /// <returns>The text of the field. Null and an empty string are written as an empty field.</returns>
        string? Convert(object? value, object? parameter, IFormatProvider provider);

        /// <summary>
        /// Converts a string to target type value.
        /// </summary>
        /// <param name="value">The text of the field, never null: an empty field is an empty string.</param>
        /// <param name="targetType">The type to convert to: the type of the column.</param>
        /// <param name="parameter">The converter parameter: TinyCsv passes null.</param>
        /// <param name="provider">The IFormatProvider to use in the converter: the ColumnFormatProvider of the column,
        /// or the culture of the options with the format of the column, if any. It is never null.</param>
        /// <returns>The value assigned to the property, that may be null.</returns>
        object? ConvertBack(string value, Type targetType, object? parameter, IFormatProvider provider);
    }
}