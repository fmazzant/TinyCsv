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
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq.Expressions;
    using TinyCsv.Extensions;

    /// <summary>
    /// Csv Options Collection Columns
    /// </summary>
    /// <typeparam name="M"></typeparam>
    public sealed class CsvOptionsColumns<M> : IEnumerable<CsvColumn>
    {
        /// <summary>
        /// Columns
        /// </summary>
        private List<CsvColumn> Columns { get; set; }

        /// <summary>
        /// Create CsvOptionColumns
        /// </summary>
        public CsvOptionsColumns()
        {
            Columns = new List<CsvColumn>();
        }

        /// <summary>
        /// Add Column
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="columnIndex"></param>
        /// <param name="expression"></param>
        /// <param name="columnFormat"></param>
        public void AddColumn<T>(int columnIndex, Expression<Func<M, T>> expression, string columnFormat = null)
        {
            this.AddColumn(columnIndex, expression.GetPropertyName(), expression, columnFormat);
        }

        /// <summary>
        /// Add Column
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="expression"></param>
        /// <param name="columnFormat"></param>
        public void AddColumn<T>(Expression<Func<M, T>> expression, string columnFormat = null)
        {
            this.AddColumn(Columns.Count, expression.GetPropertyName(), expression, columnFormat);
        }

        /// <summary>
        /// Add Column
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="columnIndex"></param>
        /// <param name="columnName"></param>
        /// <param name="expression"></param>
        /// <param name="formatProvider"></param>
        public void AddColumn<T>(int columnIndex, string columnName, Expression<Func<M, T>> expression, IFormatProvider formatProvider)
        {
            this.AddColumn(columnIndex, columnName, expression, null, formatProvider);
        }

        /// <summary>
        /// Add Column
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="expression"></param>
        /// <param name="formatProvider"></param>
        public void AddColumn<T>(Expression<Func<M, T>> expression, IFormatProvider formatProvider)
        {
            this.AddColumn(Columns.Count, expression.GetPropertyName(), expression, null, formatProvider);
        }

        /// <summary>
        /// Add Column
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="columnIndex"></param>
        /// <param name="columnName"></param>
        /// <param name="expression"></param>
        /// <param name="columnFormat"></param>
        /// <param name="formatProvider"></param>
        private void AddColumn<T>(int columnIndex, string columnName, Expression<Func<M, T>> expression, string columnFormat = null, IFormatProvider formatProvider = null)
        {
            Columns.Add(new CsvColumn()
            {
                ColumnIndex = columnIndex,
                ColumnName = columnName,
                ColumnType = typeof(T),
                ColumnExpression = expression,
                ColumnFormat = columnFormat,
                ColumnFormatProvider = formatProvider
            });
        }

        /// <summary>
        /// Get columns
        /// </summary>
        /// <returns></returns>
        public IEnumerator<CsvColumn> GetEnumerator() => Columns.GetEnumerator();

        /// <summary>
        /// Get columns
        /// </summary>
        /// <returns></returns>
        IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
    }
}
