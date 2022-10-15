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
    using System.Linq;
    using System.Linq.Expressions;
    using System.Reflection;
    using TinyCsv.Attributes;
    using TinyCsv.Conversions;
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
        /// Create CsvOptionColumns
        /// </summary>
        /// <param name="props"></param>
        public CsvOptionsColumns(Type type)
        {
            Columns = new List<CsvColumn>();
            AddColumnsFromType(type);
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
        /// <param name="columnIndex"></param>
        /// <param name="columnName"></param>
        /// <param name="expression"></param>
        /// <param name="converter"></param>
        public void AddColumn<T>(int columnIndex, string columnName, Expression<Func<M, T>> expression, IValueConverter converter)
        {
            this.AddColumn(columnIndex, columnName, expression, null, null, converter);
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
        /// <param name="expression"></param>
        /// <param name=""></param>
        /// <param name="converter"></param>
        public void AddColumn<T>(Expression<Func<M, T>> expression, IValueConverter converter)
        {
            this.AddColumn(Columns.Count, expression.GetPropertyName(), expression, null, null, converter);
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
        private void AddColumn<T>(int columnIndex, string columnName, Expression<Func<M, T>> expression, string columnFormat = null, IFormatProvider formatProvider = null, IValueConverter converter = null)
        {
            //Columns.Add(new CsvColumn()
            //{
            //    ColumnIndex = columnIndex,
            //    ColumnName = columnName,
            //    ColumnType = typeof(T),
            //    ColumnExpression = expression,
            //    ColumnFormat = columnFormat,
            //    ColumnFormatProvider = formatProvider,
            //    Converter = converter ?? new DefaultValueConverter()
            //});
            AddColumn(typeof(T), columnIndex, columnName, expression, columnFormat, formatProvider, converter);
        }

        /// <summary>
        /// Add Column
        /// </summary>
        /// <param name="columnIndex"></param>
        /// <param name="columnName"></param>
        /// <param name="expression"></param>
        /// <param name="columnFormat"></param>
        /// <param name="formatProvider"></param>
        private void AddColumn(Type type, int columnIndex, string columnName, Expression expression, string columnFormat = null, IFormatProvider formatProvider = null, IValueConverter converter = null)
        {
            var index = columnIndex < 0 ? Columns.Count : columnIndex;

            Columns.Add(new CsvColumn()
            {
                ColumnIndex = index,
                ColumnName = columnName,
                ColumnType = type,
                ColumnExpression = expression,
                ColumnFormat = columnFormat,
                ColumnFormatProvider = formatProvider,
                Converter = converter ?? new DefaultValueConverter()
            });
        }

        /// <summary>
        /// Add Columns From Properties
        /// </summary>
        /// <param name="props"></param>
        private void AddColumnsFromType(Type type)
        {
            var propertyWithColumnAttribute = type.GetProperties()
                .Where(prop => Attribute.IsDefined(prop, typeof(ColumnAttribute)));

            foreach (var property in propertyWithColumnAttribute)
            {
                AddColumnFromProperty(property);
            }
        }

        /// <summary>
        /// Add Column From Property
        /// </summary>
        /// <param name="propertyInfo"></param>
        private void AddColumnFromProperty(PropertyInfo propertyInfo)
        {
            if (propertyInfo != null)
            {
                var attribute = propertyInfo.GetCustomAttribute(typeof(ColumnAttribute));
                if (attribute is ColumnAttribute column)
                {
                    var columnIndex = column.ColumnIndex;
                    var columnName = column.ColumnName ?? propertyInfo.Name;
                    var columnFormat = column.ColumnFormat;
                    var formatProvider = column.ColumnFormatProvider;
                    var converter = column.Converter;

                    AddColumn(propertyInfo.PropertyType, columnIndex, columnName, null, columnFormat, formatProvider, converter);
                }
            }
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
