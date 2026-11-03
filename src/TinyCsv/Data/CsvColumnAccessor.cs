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

namespace TinyCsv.Data
{
    using System;
    using System.Globalization;
    using System.Linq.Expressions;
    using System.Reflection;
    using TinyCsv.Conversions;
    using TinyCsv.Exceptions;
    using TinyCsv.Helpers;

    /// <summary>
    /// The column of a read or a write: the column, its accessor and its format provider,
    /// resolved once for all the rows.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal sealed class CsvColumnBinding<T>
    {
        public CsvColumnBinding(CsvColumn column, CsvOptions<T> options)
        {
            Column = column;
            Accessor = CsvColumnAccessor<T>.Get(column, options);
            Provider = column.GetFormatProvider(options.Culture);

            // the format providers whose result is known: the others are used by the converter
            if (Provider is CsvColumn.DefaultFormatProvider formatProvider)
            {
                Format = formatProvider.CustomFormat;
                Culture = formatProvider.Culture;
            }
            else if (Provider != null && Provider.GetType() == typeof(CultureInfo))
            {
                Culture = (CultureInfo)Provider;
            }
        }

        public CsvColumn Column { get; }

        public CsvColumnAccessor<T> Accessor { get; }

        public IFormatProvider Provider { get; }

        /// <summary>
        /// The format of the column, when the provider is a DefaultFormatProvider
        /// </summary>
        public string Format { get; }

        /// <summary>
        /// The culture used to format the value, or null when the value is formatted by the converter
        /// </summary>
        public CultureInfo Culture { get; }

        /// <summary>
        /// Creates the bindings of the columns of the options
        /// </summary>
        /// <param name="options"></param>
        /// <returns></returns>
        public static CsvColumnBinding<T>[] Create(CsvOptions<T> options)
        {
            var bindings = new CsvColumnBinding<T>[options.Columns.Count];
            var index = 0;
            foreach (var column in options.Columns)
            {
                bindings[index++] = new CsvColumnBinding<T>(column, options);
            }
            return bindings;
        }
    }

    /// <summary>
    /// Reads and writes the value of a column of the model.
    /// The built-in converters of strings and value types (numbers, dates, ...) use typed delegates,
    /// so the values are not boxed. The other columns use the converter and the property helpers.
    /// The result is always the same of the converter.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal abstract class CsvColumnAccessor<T>
    {
        /// <summary>
        /// Sets the property of the model with the value of the field
        /// </summary>
        public abstract void SetValue(T model, CsvField value, CsvColumnBinding<T> binding, bool validateValues);

        /// <summary>
        /// Appends the value of the property of the model to the line
        /// </summary>
        public abstract void AppendValue(T model, CsvLineBuilder<T> line, CsvColumnBinding<T> binding);

        /// <summary>
        /// Returns the accessor of the column, created the first time
        /// </summary>
        /// <param name="column"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        public static CsvColumnAccessor<T> Get(CsvColumn column, CsvOptions<T> options)
        {
            if (column.Accessor is CsvColumnAccessor<T> accessor)
            {
                return accessor;
            }
            accessor = column.PropertyPath != null
                ? new NestedColumnAccessor<T>(column.PropertyPath)
                : CreateTyped(column, options) ?? new ConverterColumnAccessor<T>(column, options);
            column.Accessor = accessor;
            return accessor;
        }

        private static CsvColumnAccessor<T> CreateTyped(CsvColumn column, CsvOptions<T> options)
        {
            if (typeof(T).IsValueType
                || column.Converter == null
                || options.Properties == null
                || !options.Properties.TryGetValue(column.ColumnNameInternal ?? string.Empty, out var property)
                || property.PropertyType != column.ColumnType
                || property.GetIndexParameters().Length > 0
                || property.GetGetMethod() == null
                || property.GetSetMethod() == null)
            {
                return null;
            }

            try
            {
                if (column.Converter is StringConverter && property.PropertyType == typeof(string))
                {
                    return new StringColumnAccessor<T>(property);
                }

                var converterType = column.Converter.GetType();
                for (var type = converterType; type != null; type = type.BaseType)
                {
                    if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(ParsableValueConverter<>))
                    {
                        var valueType = type.GetGenericArguments()[0];
                        if (valueType != property.PropertyType)
                        {
                            return null;
                        }
                        var accessorType = typeof(ParsableColumnAccessor<,>).MakeGenericType(typeof(T), valueType);
                        return (CsvColumnAccessor<T>)Activator.CreateInstance(accessorType, property, column.Converter, IsDefaultConvert(converterType));
                    }
                }
            }
            catch (ArgumentException)
            {
                // the delegates cannot be created (i.e. the property is declared by an interface)
            }
            return null;
        }

        /// <summary>
        /// True when the converter formats the values as DefaultValueConverter, that is with string.Format
        /// </summary>
        private static bool IsDefaultConvert(Type converterType)
        {
            var convert = converterType.GetMethod(nameof(DefaultValueConverter.Convert), new[] { typeof(object), typeof(object), typeof(IFormatProvider) });
            return convert != null && convert.DeclaringType == typeof(DefaultValueConverter);
        }

        internal static TDelegate CreateDelegate<TDelegate>(MethodInfo method) where TDelegate : class
        {
            return (TDelegate)(object)Delegate.CreateDelegate(typeof(TDelegate), method);
        }
    }

    /// <summary>
    /// A column of type string with the StringConverter
    /// </summary>
    internal sealed class StringColumnAccessor<T> : CsvColumnAccessor<T>
    {
        private readonly Func<T, string> getter;
        private readonly Action<T, string> setter;

        public StringColumnAccessor(PropertyInfo property)
        {
            getter = CreateDelegate<Func<T, string>>(property.GetGetMethod());
            setter = CreateDelegate<Action<T, string>>(property.GetSetMethod());
        }

        public override void SetValue(T model, CsvField value, CsvColumnBinding<T> binding, bool validateValues)
        {
            // StringConverter.ConvertBack returns the value, that is always valid
            setter(model, value.ToString());
        }

        public override void AppendValue(T model, CsvLineBuilder<T> line, CsvColumnBinding<T> binding)
        {
            if (binding.Culture == null)
            {
                line.AppendField(binding.Column.Converter.Convert(getter(model), null, binding.Provider));
                return;
            }
            // string.Format(provider, "{0}", value) returns the value with a culture and with DefaultFormatProvider
            line.AppendField(getter(model));
        }
    }

    /// <summary>
    /// A column of a value type with a ParsableValueConverter (numbers, dates, ...)
    /// </summary>
    internal sealed class ParsableColumnAccessor<T, TValue> : CsvColumnAccessor<T>
        where TValue : struct
    {
        private readonly Func<T, TValue> getter;
        private readonly Action<T, TValue> setter;
        private readonly ParsableValueConverter<TValue> converter;
        private readonly bool defaultConvert;

        public ParsableColumnAccessor(PropertyInfo property, ParsableValueConverter<TValue> converter, bool defaultConvert)
        {
            getter = CreateDelegate<Func<T, TValue>>(property.GetGetMethod());
            setter = CreateDelegate<Action<T, TValue>>(property.GetSetMethod());
            this.converter = converter;
            this.defaultConvert = defaultConvert;
        }

        public override void SetValue(T model, CsvField value, CsvColumnBinding<T> binding, bool validateValues)
        {
            // as ParsableValueConverter.TryConvertBack: an empty value is the default value
            var span = value.Span;
            var isEmpty = IsWhiteSpace(span);
            if (!isEmpty && converter.TryParseValue(span, binding.Provider, out var parsed))
            {
                setter(model, parsed);
                return;
            }
            if (validateValues && !isEmpty)
            {
                throw binding.Column.NotValidValueException(value.ToString());
            }
            setter(model, default);
        }

        /// <summary>
        /// As string.IsNullOrWhiteSpace
        /// </summary>
        private static bool IsWhiteSpace(ReadOnlySpan<char> value)
        {
            for (int i = 0; i < value.Length; i++)
            {
                if (!char.IsWhiteSpace(value[i]))
                {
                    return false;
                }
            }
            return true;
        }

        public override void AppendValue(T model, CsvLineBuilder<T> line, CsvColumnBinding<T> binding)
        {
            var value = getter(model);
            if (!defaultConvert || binding.Culture == null)
            {
                line.AppendField(converter.Convert(value, null, binding.Provider));
                return;
            }
            // string.Format(provider, "{0}", value) is value.ToString(format, culture), see DefaultFormatProvider
            line.AppendFormatted(value, binding.Format, binding.Culture);
        }
    }

    /// <summary>
    /// A column read and written by the converter, with the property helpers
    /// </summary>
    internal sealed class ConverterColumnAccessor<T> : CsvColumnAccessor<T>
    {
        private readonly CsvOptions<T> options;
        private readonly string propertyName;
        private readonly PropertyHelper<T> property;

        public ConverterColumnAccessor(CsvColumn column, CsvOptions<T> options)
        {
            this.options = options;
            propertyName = column.ColumnNameInternal;
            if (propertyName != null && options.FasterProperties != null)
            {
                options.FasterProperties.TryGetValue(propertyName, out property);
            }
        }

        // a property that is not found throws, as the property helpers do
        private PropertyHelper<T> Property => property ?? options.FasterProperties[propertyName];

        public override void SetValue(T model, CsvField value, CsvColumnBinding<T> binding, bool validateValues)
        {
            Property.Setter(model, binding.Column.ConvertBack(value.ToString(), binding.Provider, validateValues));
        }

        public override void AppendValue(T model, CsvLineBuilder<T> line, CsvColumnBinding<T> binding)
        {
            var value = Property.Getter(model);
            line.AppendField(binding.Column.Converter.Convert(value, null, binding.Provider));
        }
    }
}
