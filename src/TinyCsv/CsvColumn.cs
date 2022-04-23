using System;
using System.Globalization;
using System.Linq.Expressions;

namespace TinyCsv
{
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
