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
        internal IFormatProvider ColumnFormatProvider
        {
            get
            {
                if (string.IsNullOrEmpty(ColumnFormat))
                {
                    return CultureInfo.InvariantCulture;
                }
                return new FormatProvider(ColumnFormat);
            }
        }
        internal class FormatProvider : IFormatProvider, ICustomFormatter
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
