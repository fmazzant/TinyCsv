﻿/// <summary>
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

    /* Unmerged change from project 'TinyCsv (net46)'
    Before:
        using TinyCsv.Extensions;
    After:
        using System;
    */

    /* Unmerged change from project 'TinyCsv (net47)'
    Before:
        using TinyCsv.Extensions;
    After:
        using System;
    */

    /* Unmerged change from project 'TinyCsv (net48)'
    Before:
        using TinyCsv.Extensions;
    After:
        using System;
    */

    /* Unmerged change from project 'TinyCsv (netstandard2.0)'
    Before:
        using TinyCsv.Extensions;
    After:
        using System;
    */

    /* Unmerged change from project 'TinyCsv (netstandard2.1)'
    Before:
        using TinyCsv.Extensions;
    After:
        using System;
    */

    /* Unmerged change from project 'TinyCsv (net5.0)'
    Before:
        using TinyCsv.Extensions;
    After:
        using System;
    */

    /* Unmerged change from project 'TinyCsv (net6.0)'
    Before:
        using TinyCsv.Extensions;
    After:
        using System;
    */
    using TinyCsv.Extensions;

    /// <summary>
    /// Provides a unified way of converting Uri of value to string
    /// </summary>
    public sealed class UriConverter : DefaultValueConverter, IValueConverter
    {
        /// <summary>
        /// Converts a string to target type value.
        /// </summary>
        /// <param name="value">The value that is produced by the binding target.</param>
        /// <param name="targetType">The type to convert to.</param>
        /// <param name="parameter">The converter parameter to use.</param>
        /// <returns>A converted value. If the method returns null, the valid null value is used.</returns>
        public override object ConvertBack(string value, Type targetType, object parameter, IFormatProvider provider)
        {
            if (Uri.TryCreate(value, UriKind.RelativeOrAbsolute, out var uri))
            {
                return uri;
            }
            return targetType.IsNullable() ? null : default(Uri);
        }
    }
}
