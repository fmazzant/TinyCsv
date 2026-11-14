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

namespace Mafesoft.TinyCsv
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.Linq.Expressions;
    using Mafesoft.TinyCsv.Conversions;
    using Mafesoft.TinyCsv.Exceptions;

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
        public string ColumnName { get; internal set; } = string.Empty;

        /// <summary>
        /// The other names of the column in the header record, see <see cref="ColumnNames"/>
        /// </summary>
        private string[] alternativeNames = new string[0];

        /// <summary>
        /// The names of the column in the header record (see CsvOptions.MapColumnsByHeader): the <see cref="ColumnName"/>,
        /// that is the name written in the header, and then the alternative names. When reading, the first name found wins.
        /// </summary>
        public IReadOnlyList<string> ColumnNames
        {
            get
            {
                var names = new string[alternativeNames.Length + 1];
                names[0] = ColumnName;
                alternativeNames.CopyTo(names, 1);
                return names;
            }
        }

        /// <summary>
        /// Adds alternative names of the column in the header record (i.e. "ID" and "Code" for the column "Id"),
        /// see <see cref="ColumnNames"/>. The header is written with the <see cref="ColumnName"/>.
        /// </summary>
        /// <param name="alternativeNames"></param>
        /// <returns>the column</returns>
        /// <exception cref="ArgumentNullException"></exception>
        public CsvColumn WithNames(params string[] alternativeNames)
        {
            if (alternativeNames == null)
            {
                throw new ArgumentNullException(nameof(alternativeNames));
            }
            var names = new string[this.alternativeNames.Length + alternativeNames.Length];
            this.alternativeNames.CopyTo(names, 0);
            alternativeNames.CopyTo(names, this.alternativeNames.Length);
            this.alternativeNames = names;
            return this;
        }

        /// <summary>
        /// The other names of the column, without the ColumnName
        /// </summary>
        internal string[] AlternativeNames => alternativeNames;

        /// <summary>
        /// Column's name
        /// </summary>
        internal string ColumnNameInternal { get; set; } = string.Empty;

        /// <summary>
        /// Column's type
        /// </summary>
        public Type ColumnType { get; internal set; } = typeof(object);

        /// <summary>
        /// Column expression
        /// </summary>
        public Expression? ColumnExpression { get; internal set; }

        /// <summary>
        /// Column's value format
        /// </summary>
        public string? ColumnFormat { get; internal set; }

        /// <summary>
        /// Converter
        /// </summary>
        public IValueConverter Converter { get; internal set; } = new DefaultValueConverter();

        /// <summary>
        /// The values of the column that are read as null, instead of CsvOptions.NullValues. Null uses the values of the options.
        /// </summary>
        public string[]? NullValues { get; internal set; }

        /// <summary>
        /// True when the column may be missing from the header record (see CsvOptions.MapColumnsByHeader):
        /// no MissingColumnException is thrown and the property keeps its default value
        /// </summary>
        public bool IsOptional { get; internal set; }

        /// <summary>
        /// True when the column has a <see cref="DefaultValue"/>
        /// </summary>
        public bool HasDefaultValue { get; private set; }

        /// <summary>
        /// The value of the property when the field is empty or missing, of the type of the column (see HasDefaultValue)
        /// </summary>
        public object? DefaultValue { get; private set; }

        /// <summary>
        /// Sets the default value, converted to the type of the column
        /// </summary>
        /// <param name="value"></param>
        /// <exception cref="ArgumentException">the value cannot be converted to the type of the column</exception>
        internal void SetDefaultValue(object? value)
        {
            DefaultValue = ConvertDefaultValue(value);
            HasDefaultValue = true;
        }

        private object? ConvertDefaultValue(object? value)
        {
            var type = Nullable.GetUnderlyingType(ColumnType) ?? ColumnType;
            if (value == null)
            {
                if (ColumnType.IsValueType && type == ColumnType)
                {
                    throw new ArgumentException($"The default value of the column {ColumnName} cannot be null: {ColumnType.Name} is not nullable.", nameof(value));
                }
                return null;
            }
            if (type.IsInstanceOfType(value))
            {
                return value;
            }
            try
            {
                if (type.IsEnum)
                {
                    return value is string name ? Enum.Parse(type, name, true) : Enum.ToObject(type, value);
                }
                if (value is string text)
                {
                    if (type == typeof(Guid))
                    {
                        return Guid.Parse(text);
                    }
                    if (type == typeof(TimeSpan))
                    {
                        return TimeSpan.Parse(text, CultureInfo.InvariantCulture);
                    }
                    if (type == typeof(DateTimeOffset))
                    {
                        return DateTimeOffset.Parse(text, CultureInfo.InvariantCulture);
                    }
                }
                if (value is IConvertible && typeof(IConvertible).IsAssignableFrom(type))
                {
                    return System.Convert.ChangeType(value, type, CultureInfo.InvariantCulture);
                }
            }
            catch (Exception exception) when (exception is FormatException || exception is InvalidCastException
                || exception is OverflowException || exception is ArgumentException)
            {
                throw new ArgumentException($"The default value '{value}' of the column {ColumnName} is not a valid {ColumnType.Name}.", nameof(value), exception);
            }
            throw new ArgumentException($"The default value '{value}' of the column {ColumnName} is not a valid {ColumnType.Name}.", nameof(value));
        }

        /// <summary>
        /// The accessor of the column (see Data.CsvColumnAccessor), created the first time the column is read or written
        /// </summary>
        internal object? Accessor { get; set; }

        /// <summary>
        /// The properties of a nested column (m => m.Address.City), null when the column is a property of the model
        /// </summary>
        internal System.Reflection.PropertyInfo[]? PropertyPath { get; set; }

        /// <summary>
        /// Value format provider
        /// </summary>
        private IFormatProvider? _FormatProvider = null;

        /// <summary>
        /// Column's value format provider. When it is not set (or it is set to null),
        /// it is the invariant culture with the ColumnFormat, if any.
        /// </summary>
        [AllowNull]
        public IFormatProvider ColumnFormatProvider
        {
            get
            {
                var formatProvider = _FormatProvider;
                if (formatProvider == null)
                {
                    var columnFormat = ColumnFormat;
                    if (columnFormat == null || columnFormat.Length == 0)
                    {
                        formatProvider = CultureInfo.InvariantCulture;
                    }
                    else
                    {
                        formatProvider = new DefaultFormatProvider(columnFormat);
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
        private DefaultFormatProvider? _CultureFormatProvider = null;

        /// <summary>
        /// Converts the value of the column. With validateValues (see CsvOptions.ValidateValues), a value that is not valid
        /// for the type of the column throws an InvalidColumnValueException, otherwise it is converted to the default value.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="provider">the format provider of the column, see GetFormatProvider</param>
        /// <param name="validateValues"></param>
        /// <returns></returns>
        /// <exception cref="InvalidColumnValueException"></exception>
        internal object? ConvertBack(string value, IFormatProvider provider, bool validateValues)
        {
            if (validateValues && Converter is DefaultValueConverter converter)
            {
                if (!converter.TryConvertBack(value, ColumnType, null, provider, out var result))
                {
                    throw NotValidValueException(value);
                }
                return result;
            }
            return Converter.ConvertBack(value, ColumnType, null, provider);
        }

        /// <summary>
        /// The exception of a value that is not valid for the type of the column (see CsvOptions.ValidateValues)
        /// </summary>
        internal InvalidColumnValueException NotValidValueException(string value)
        {
            return new InvalidColumnValueException(ColumnName, value, $"The value '{value}' is not a valid {ColumnType.Name}.");
        }

        /// <summary>
        /// The exception of a value that cannot be converted, i.e. by a custom converter or by the setter of the property
        /// </summary>
        internal InvalidColumnValueException NotConvertibleValueException(string value, Exception exception)
        {
            return new InvalidColumnValueException(ColumnName, value,
                $"The value '{value}' cannot be converted to {ColumnType.Name}: {exception.Message}", exception);
        }

        /// <summary>
        /// Returns the format provider used to read and write the values of the column:
        /// the ColumnFormatProvider when it is set, otherwise the culture with the ColumnFormat, if any.
        /// </summary>
        /// <param name="culture">the culture of the options (see CsvOptions.Culture)</param>
        /// <returns></returns>
        internal IFormatProvider GetFormatProvider(CultureInfo? culture)
        {
            if (_FormatProvider != null)
            {
                return _FormatProvider;
            }

            culture = culture ?? CultureInfo.InvariantCulture;
            var columnFormat = ColumnFormat;
            if (columnFormat == null || columnFormat.Length == 0)
            {
                return culture;
            }

            var formatProvider = _CultureFormatProvider;
            if (formatProvider == null || !ReferenceEquals(formatProvider.Culture, culture) || formatProvider.CustomFormat != columnFormat)
            {
                formatProvider = new DefaultFormatProvider(columnFormat, culture);
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
            public DefaultFormatProvider(string customFormat, CultureInfo? culture)
            {
                CustomFormat = customFormat;
                Culture = culture ?? CultureInfo.InvariantCulture;
            }

            /// <summary>
            /// Get format: the custom formatter, or the number and date formats of the culture
            /// </summary>
            /// <param name="formatType"></param>
            /// <returns></returns>
            public object? GetFormat(Type? formatType)
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
            public string Format(string? format, object? arg, IFormatProvider? formatProvider)
            {
                return arg is IFormattable formattable
                    ? formattable.ToString(CustomFormat, Culture)
                    : arg?.ToString() ?? string.Empty;
            }
        }
    }
}
