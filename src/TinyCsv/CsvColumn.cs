using System;
using System.Linq.Expressions;

namespace TinyCsv
{
    public sealed class CsvColumn
    {
        public int ColumnIndex { get; internal set; }
        public string ColumnName { get; internal set; }
        public Type ColumnType { get; internal set; }
        public Expression ColumnExpression { get; internal set; }
    }
}
