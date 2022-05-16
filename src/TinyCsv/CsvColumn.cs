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

namespace TinyCsv
{
    using System;
    using System.Globalization;
    using System.Linq.Expressions;
    using TinyCsv.Conversions;

    /// <summary>
    /// The column definition.
    /// </summary>
    public sealed class CsvColumn
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
        /// Converter
        /// </summary>
        public IValueConverter Converter { get; internal set; } = new DefaultValueConverter();

        /// <summary>
        /// Value format provider
        /// </summary>
        private IFormatProvider _FormatProvider = null;

        /// <summary>
        /// Column's value format provider
        /// </summary>
        public IFormatProvider ColumnFormatProvider
        {
            get
            {
                var formatProvider = _FormatProvider;
                if (formatProvider == null)
                {
                    if (string.IsNullOrEmpty(ColumnFormat))
                    {
                        formatProvider = CultureInfo.InvariantCulture;
                    }
                    else
                    {
                        formatProvider = new DefaultFormatProvider(ColumnFormat);
                    }
                }
                return formatProvider;
            }
            set
            {
                _FormatProvider = value;
            }
        }

        /// <summary>
        /// Default Format Provider definition
        /// </summary>
        private sealed class DefaultFormatProvider : IFormatProvider, ICustomFormatter
        {
            /// <summary>
            /// Column's format
            /// </summary>
            public string CustomFormat { get; set; }

            /// <summary>
            /// Create Default Format Provider
            /// </summary>
            /// <param name="customFormat"></param>
            public DefaultFormatProvider(string customFormat)
            {
                CustomFormat = customFormat;
            }

            /// <summary>
            /// Get format
            /// </summary>
            /// <param name="formatType"></param>
            /// <returns></returns>
            public object GetFormat(Type formatType)
            {
                return this;
            }

            /// <summary>
            /// Format
            /// </summary>
            /// <param name="format"></param>
            /// <param name="arg"></param>
            /// <param name="formatProvider"></param>
            /// <returns></returns>
            public string Format(string format, object arg, IFormatProvider formatProvider)
            {
                return ((IFormattable)arg).ToString(CustomFormat, formatProvider);
            }
        }
    }
}
