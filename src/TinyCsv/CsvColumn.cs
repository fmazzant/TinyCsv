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

    public sealed class CsvColumn
    {
        public int ColumnIndex { get; internal set; }
        public string ColumnName { get; internal set; }
        public Type ColumnType { get; internal set; }
        public Expression ColumnExpression { get; internal set; }
        public string ColumnFormat { get; internal set; }

        private IFormatProvider _FormatProvider = null;
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
                        formatProvider = new FormatProvider(ColumnFormat);
                    }
                }
                return formatProvider;
            }
            set
            {
                _FormatProvider = value;
            }
        }
        
        private sealed class FormatProvider : IFormatProvider, ICustomFormatter
        {
            public string CustomFormat { get; set; }
            public FormatProvider(string customFormat)
            {
                CustomFormat = customFormat;
            }
            public object GetFormat(Type formatType)
            {
                return this;
            }
            public string Format(string format, object arg, IFormatProvider formatProvider)
            {
                return ((IFormattable)arg).ToString(CustomFormat, formatProvider);
            }
        }
    }
}
