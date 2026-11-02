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
        /// Column's name
        /// </summary>
        internal string ColumnNameInternal { get; set; }

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
        public IValueConverter Converter { get; internal set; }

        /// <summary>
        /// The accessor of the column (see Data.CsvColumnAccessor), created the first time the column is read or written
        /// </summary>
        internal object Accessor { get; set; }

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
        /// The format provider of the last culture, see GetFormatProvider
        /// </summary>
        private DefaultFormatProvider _CultureFormatProvider = null;

        /// <summary>
        /// Returns the format provider used to read and write the values of the column:
        /// the ColumnFormatProvider when it is set, otherwise the culture with the ColumnFormat, if any.
        /// </summary>
        /// <param name="culture">the culture of the options (see CsvOptions.Culture)</param>
        /// <returns></returns>
        internal IFormatProvider GetFormatProvider(CultureInfo culture)
        {
            if (_FormatProvider != null)
            {
                return _FormatProvider;
            }

            culture = culture ?? CultureInfo.InvariantCulture;
            if (string.IsNullOrEmpty(ColumnFormat))
            {
                return culture;
            }

            var formatProvider = _CultureFormatProvider;
            if (formatProvider == null || !ReferenceEquals(formatProvider.Culture, culture) || formatProvider.CustomFormat != ColumnFormat)
            {
                formatProvider = new DefaultFormatProvider(ColumnFormat, culture);
                _CultureFormatProvider = formatProvider;
            }
            return formatProvider;
        }

        /// <summary>
        /// Default Format Provider definition
        /// </summary>
        internal sealed class DefaultFormatProvider : IFormatProvider, ICustomFormatter
        {
            /// <summary>
            /// Column's format
            /// </summary>
            public string CustomFormat { get; set; }

            /// <summary>
            /// The culture used to format and parse the values (see CsvOptions.Culture)
            /// </summary>
            public CultureInfo Culture { get; }

            /// <summary>
            /// Create Default Format Provider
            /// </summary>
            /// <param name="customFormat"></param>
            public DefaultFormatProvider(string customFormat)
                : this(customFormat, CultureInfo.InvariantCulture)
            {
            }

            /// <summary>
            /// Create Default Format Provider
            /// </summary>
            /// <param name="customFormat"></param>
            /// <param name="culture"></param>
            public DefaultFormatProvider(string customFormat, CultureInfo culture)
            {
                CustomFormat = customFormat;
                Culture = culture ?? CultureInfo.InvariantCulture;
            }

            /// <summary>
            /// Get format: the custom formatter, or the number and date formats of the culture
            /// </summary>
            /// <param name="formatType"></param>
            /// <returns></returns>
            public object GetFormat(Type formatType)
            {
                return formatType == typeof(ICustomFormatter) ? this : Culture.GetFormat(formatType);
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
                return arg is IFormattable formattable
                    ? formattable.ToString(CustomFormat, Culture)
                    : arg?.ToString();
            }
        }
    }
}
