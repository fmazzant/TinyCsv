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

namespace TinyCsv.Attributes
{
    using System;
    using System.Linq.Expressions;
    using TinyCsv.Conversions;

    /// <summary>
    /// Allows comment inside the content
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public sealed class ColumnAttribute : Attribute
    {
        /// <summary>
        /// Column's index
        /// </summary>
        public int ColumnIndex { get; internal set; }

        /// <summary>
        /// Column's name
        /// </summary>
        public string ColumnName { get; internal set; }

        /// <summary>
        /// Column's type
        /// </summary>
        public Type ColumnType { get; internal set; }

        /// <summary>
        /// Column expression
        /// </summary>
        public Expression ColumnExpression { get; internal set; }

        /// <summary>
        /// Column's value format
        /// </summary>
        public string ColumnFormat { get; internal set; }

        /// <summary>
        /// Column's Format Provider
        /// </summary>
        public IFormatProvider ColumnFormatProvider { get; internal set; }

        /// <summary>
        /// Converter
        /// </summary>
        public IValueConverter Converter { get; internal set; }

        /// <summary>
        /// Contructor
        /// </summary>
        /// <param name="index"></param>
        /// <param name="name"></param>
        /// <param name="type"></param>
        /// <param name="format"></param>
        /// <param name="converter"></param>
        public ColumnAttribute(int index = -1, string name = null, Type type = null, string format = null, Type formatProvider = null, Type converter = null)
            : base()
        {
            ColumnIndex = index;
            ColumnName = name;
            ColumnType = type;
            ColumnExpression = null;
            ColumnFormat = format;

            if (formatProvider != null)
            {
                ColumnFormatProvider = (IFormatProvider)Activator.CreateInstance(formatProvider);
            }

            if (converter != null)
            {
                Converter = (IValueConverter)Activator.CreateInstance(converter);
            }
        }
    }
}
