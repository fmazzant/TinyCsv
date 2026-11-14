
namespace Mafesoft.TinyCsv
{
    using System;
    using System.Collections.Generic;
    using System.Linq.Expressions;
    using Mafesoft.TinyCsv.Conversions;
    using Mafesoft.TinyCsv.Extensions;

    /// <summary>
    /// The columns and the options of a model, defined outside the model: the model has no attributes.
    /// Define the columns with <see cref="Map{TValue}(Expression{Func{T, TValue}})"/> in the constructor,
    /// and use the map with <see cref="CsvOptions{T}.UseClassMap(CsvClassMap{T})"/>
    /// or <see cref="TinyCsv{T}.TinyCsv(CsvClassMap{T})"/>.
    /// </summary>
    /// <example>
    /// public sealed class ProductMap : CsvClassMap&lt;Product&gt;
    /// {
    ///     public ProductMap()
    ///     {
    ///         Options(o => o.HasHeaderRecord = true);
    ///         Map(m => m.Id);
    ///         Map(m => m.Name).Name("product_name");
    ///         Map(m => m.Price).Format("0.00");
    ///     }
    /// }
    /// </example>
    /// <typeparam name="T">the model</typeparam>
    public abstract class CsvClassMap<T>
    {
        private readonly List<CsvMemberMap> members = new List<CsvMemberMap>();
        private Action<CsvOptions<T>>? configure;

        /// <summary>
        /// Maps a property of the model, or a nested property (m => m.Address.City), to a column.
        /// The index is the position of the map and the name is the property name, unless they are set.
        /// </summary>
        /// <typeparam name="TValue"></typeparam>
        /// <param name="expression"></param>
        /// <returns></returns>
        protected CsvMemberMap Map<TValue>(Expression<Func<T, TValue>> expression)
        {
            if (expression == null)
            {
                throw new ArgumentNullException(nameof(expression));
            }
            var member = new CsvMemberMap(typeof(TValue), expression);
            members.Add(member);
            return member;
        }

        /// <summary>
        /// Sets the options (i.e. the delimiter and the header), as the attributes of the model
        /// </summary>
        /// <param name="options"></param>
        protected void Options(Action<CsvOptions<T>> options)
        {
            configure += options;
        }

        /// <summary>
        /// Applies the options and adds the columns
        /// </summary>
        internal void Apply(CsvOptions<T> options)
        {
            configure?.Invoke(options);
            foreach (var member in members)
            {
                member.AddTo(options.Columns);
            }
        }
    }

    /// <summary>
    /// The column of a property, see <see cref="CsvClassMap{T}"/>
    /// </summary>
    public sealed class CsvMemberMap
    {
        private readonly Type type;
        private readonly Expression expression;
        private int index = -1;
        private string? name;
        private string? format;
        private IFormatProvider? formatProvider;
        private IValueConverter? converter;
        private string[]? nullValues;
        private bool ignore;
        private bool optional;
        private bool hasDefaultValue;
        private object? defaultValue;

        internal CsvMemberMap(Type type, Expression expression)
        {
            this.type = type;
            this.expression = expression;
        }

        /// <summary>
        /// The index of the column
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public CsvMemberMap Index(int index)
        {
            this.index = index;
            return this;
        }

        /// <summary>
        /// The name of the column
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public CsvMemberMap Name(string? name)
        {
            this.name = name;
            return this;
        }

        /// <summary>
        /// The format of the value (i.e. "0.00" or "yyyy-MM-dd")
        /// </summary>
        /// <param name="format"></param>
        /// <returns></returns>
        public CsvMemberMap Format(string? format)
        {
            this.format = format;
            return this;
        }

        /// <summary>
        /// The format provider of the value
        /// </summary>
        /// <param name="formatProvider"></param>
        /// <returns></returns>
        public CsvMemberMap FormatProvider(IFormatProvider? formatProvider)
        {
            this.formatProvider = formatProvider;
            return this;
        }

        /// <summary>
        /// The converter of the value
        /// </summary>
        /// <param name="converter"></param>
        /// <returns></returns>
        public CsvMemberMap Converter(IValueConverter? converter)
        {
            this.converter = converter;
            return this;
        }

        /// <summary>
        /// The values of the column that are read as null (i.e. "N/A"), instead of the NullValues of the options
        /// </summary>
        /// <param name="nullValues"></param>
        /// <returns></returns>
        public CsvMemberMap NullValues(params string[] nullValues)
        {
            this.nullValues = nullValues;
            return this;
        }

        /// <summary>
        /// The property is not a column: it is neither read nor written
        /// </summary>
        /// <returns></returns>
        public CsvMemberMap Ignore()
        {
            ignore = true;
            return this;
        }

        /// <summary>
        /// The column may be missing from the header record (see CsvOptions.MapColumnsByHeader):
        /// no MissingColumnException is thrown and the property keeps its default value
        /// </summary>
        /// <returns></returns>
        public CsvMemberMap Optional()
        {
            optional = true;
            return this;
        }

        /// <summary>
        /// The value of the property when the field is empty or missing. It is converted to the type of the property
        /// and a value that cannot be converted throws an ArgumentException when the map is used.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public CsvMemberMap Default(object? value)
        {
            hasDefaultValue = true;
            defaultValue = value;
            return this;
        }

        internal void AddTo<T>(CsvOptionsColumns<T> columns)
        {
            if (ignore)
            {
                return;
            }
            var column = columns.AddColumn(type, index, name ?? expression.GetColumnName(), expression, format, formatProvider, converter);
            column.NullValues = nullValues;
            column.IsOptional = optional;
            if (hasDefaultValue)
            {
                column.SetDefaultValue(defaultValue);
            }
        }
    }
}
