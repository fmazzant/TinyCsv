using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using TinyCsv.Extentsions;

namespace TinyCsv
{
    public sealed class CsvOptionsColumns<M> : IEnumerable<CsvColumn>
    {
        public List<CsvColumn> Columns { get; private set; }

        public CsvOptionsColumns()
        {
            Columns = new List<CsvColumn>();
        }

        public void AddColumn<T>(int columnIndex, Expression<Func<M, T>> expression)
        {
            this.AddColumn(columnIndex, expression.GetPropertyName(), expression);
        }
        public void AddColumn<T>(Expression<Func<M, T>> expression)
        {
            this.AddColumn(Columns.Count, expression.GetPropertyName(), expression);
        }
        private void AddColumn<T>(int columnIndex, string columnName, Expression<Func<M, T>> expression)
        {
            Columns.Add(new CsvColumn()
            {
                ColumnIndex = columnIndex,
                ColumnName = columnName,
                ColumnType = typeof(T),
                ColumnExpression = expression
            });
        }
        public IEnumerator<CsvColumn> GetEnumerator() => Columns.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
    }
}
