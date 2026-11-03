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
    using TinyCsv.Factory;

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
        /// Number of Columns
        /// </summary>
        public int Count => Columns.Count;

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
            this.AddColumn(columnIndex, expression.GetColumnName(), expression, columnFormat);
        }

        /// <summary>
        /// Add Column
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="columnName"></param>
        /// <param name="expression"></param>
        /// <param name="columnFormat"></param>
        public void AddColumn<T>(string columnName, Expression<Func<M, T>> expression, string columnFormat = null)
        {
            this.AddColumn(Columns.Count, columnName, expression, columnFormat);
        }

        /// <summary>
        /// Add Column
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="expression"></param>
        /// <param name="columnFormat"></param>
        public void AddColumn<T>(Expression<Func<M, T>> expression, string columnFormat = null)
        {
            this.AddColumn(Columns.Count, expression.GetColumnName(), expression, columnFormat);
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
            this.AddColumn(Columns.Count, expression.GetColumnName(), expression, null, formatProvider);
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
            this.AddColumn(Columns.Count, expression.GetColumnName(), expression, null, null, converter);
        }

        /// <summary>
        /// Add Column
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="columnName"></param>
        /// <param name="expression"></param>
        /// <param name=""></param>
        /// <param name="converter"></param>
        public void AddColumn<T>(string columnName, Expression<Func<M, T>> expression, IValueConverter converter)
        {
            this.AddColumn(Columns.Count, columnName, expression, null, null, converter);
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
        /// <param name="converter"></param>
        /// <param name="propertyName">the model's property, when the column is not defined by an expression</param>
        /// <param name="propertyPath">the properties of a nested property, when the column is not defined by an expression</param>
        internal void AddColumn(Type type, int columnIndex, string columnName, Expression expression, string columnFormat = null, IFormatProvider formatProvider = null, IValueConverter converter = null, string propertyName = null, PropertyInfo[] propertyPath = null)
        {
            var index = columnIndex < 0 ? Columns.Count : columnIndex;
            var path = propertyPath ?? expression?.GetNestedPropertyPath();
            if (path != null)
            {
                propertyName = ExpressionExtensions.GetPropertyPathName(path);
            }

            Columns.Add(new CsvColumn()
            {
                ColumnIndex = index,
                ColumnName = columnName,
                ColumnNameInternal = propertyName ?? expression?.GetPropertyName() ?? columnName,
                ColumnType = type,
                ColumnExpression = expression,
                ColumnFormat = columnFormat,
                ColumnFormatProvider = formatProvider,
                Converter = converter ?? ValueConverterFactory.CreateValueConverterByType(type),
                PropertyPath = path
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
        /// Adds a column for each public property of the type that can be read and written
        /// and whose type has a value converter, in the order of declaration.
        /// The column name is the property name. The properties of a class with a public parameterless constructor
        /// are columns too, named as the nested property: Address.City is Address_City. Collections are ignored.
        /// </summary>
        /// <param name="type"></param>
        internal void AddColumnsFromProperties(Type type)
        {
            AddColumnsFromProperties(type, new PropertyInfo[0], new HashSet<Type> { type }, new HashSet<string>());
        }

        private void AddColumnsFromProperties(Type type, PropertyInfo[] parent, HashSet<Type> types, HashSet<string> names)
        {
            foreach (var property in type.GetProperties(BindingFlags.Instance | BindingFlags.Public))
            {
                var path = new PropertyInfo[parent.Length + 1];
                parent.CopyTo(path, 0);
                path[parent.Length] = property;
                var name = ExpressionExtensions.GetColumnName(path);

                if (property.GetIndexParameters().Length > 0
                    || property.GetGetMethod() == null
                    || property.GetSetMethod() == null
                    || !names.Add(name))
                {
                    continue;
                }

                var converter = ValueConverterFactory.CreateValueConverterByType(property.PropertyType);
                if (converter.GetType() != typeof(DefaultValueConverter))
                {
                    AddColumn(property.PropertyType, Columns.Count, name, null, converter: converter, propertyName: property.Name, propertyPath: parent.Length > 0 ? path : null);
                }
                else if (ExpressionExtensions.IsNestedType(property.PropertyType) && types.Add(property.PropertyType))
                {
                    // a type that contains itself (i.e. Person.Parent) is not expanded again
                    AddColumnsFromProperties(property.PropertyType, path, types, names);
                    types.Remove(property.PropertyType);
                }
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

                    AddColumn(propertyInfo.PropertyType, columnIndex, columnName, null, columnFormat, formatProvider, converter, propertyInfo.Name);
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
