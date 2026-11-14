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

namespace Mafesoft.TinyCsv.Data
{
    using System;
    using System.Collections.Generic;
    using System.IO;
#if NET6_0_OR_GREATER
    using System.Runtime.CompilerServices;
#endif
    using System.Threading.Tasks;
    using Mafesoft.TinyCsv.Exceptions;
    using Mafesoft.TinyCsv.Extensions;

    /// <summary>
    /// Builds the lines of the models in a reusable buffer, so a line is written without creating strings.
    /// The fields are escaped as EnclosedInQuotesIfNecessary does.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal sealed class CsvLineBuilder<T>
    {
        private readonly CsvOptions<T> options;
        private readonly CsvColumnBinding<T>[] bindings;
        private readonly string delimiter;
        private readonly char[] escapeChars;
        private readonly bool multiCharDelimiter;
        private readonly bool alwaysEscape;
        private readonly bool quoting;
        private readonly bool backslash;
        private readonly char quote;
        // true when the escaped field is written in the buffer (see AppendEscaped),
        // false in the cases where only EnclosedInQuotesIfNecessary gives the same result
        private readonly bool escapeInBuffer;
        private readonly CsvInjectionProtection injection;
        private readonly char[] injectionChars;
        private readonly CsvQuoteMode quoteMode;
        private readonly string nullValueOnWrite;
        // the column of the field that is appended, see Build
        private CsvColumnBinding<T>? current;
        private char[] buffer = new char[256];
        private int length;

        public CsvLineBuilder(CsvOptions<T> options)
        {
            this.options = options;
            bindings = CsvColumnBinding<T>.Create(options);
            delimiter = options.Delimiter ?? string.Empty;
            quoting = options.AllowRowEnclosedInDoubleQuotesValues;
            backslash = options.AllowBackSlashToEscapeQuote;
            quote = options.DoubleQuotes;
            // EnclosedInQuotesIfNecessary looks for the delimiter after doubling the backslashes,
            // and throws with a null delimiter
            escapeInBuffer = options.Delimiter != null
                && !(backslash && (quote == '\\' || delimiter.IndexOf('\\') >= 0));

            // the chars that may require EnclosedInQuotesIfNecessary
            var chars = new List<char>();
            if (options.AllowBackSlashToEscapeQuote)
            {
                chars.Add('\\');
            }
            if (options.AllowRowEnclosedInDoubleQuotesValues)
            {
                chars.Add(options.DoubleQuotes);
                chars.Add('\r');
                chars.Add('\n');
                if (delimiter.Length == 1)
                {
                    chars.Add(delimiter[0]);
                }
                multiCharDelimiter = delimiter.Length > 1;
                alwaysEscape = delimiter.Length == 0;
            }
            escapeChars = chars.ToArray();
            injection = options.InjectionProtection;
            injectionChars = options.InjectionCharacters ?? CsvOptions.DefaultInjectionCharacters;
            quoteMode = options.QuoteMode;
            nullValueOnWrite = options.NullValueOnWrite ?? string.Empty;
        }

        /// <summary>
        /// Builds the line of the model
        /// </summary>
        /// <param name="model"></param>
        /// <param name="rowIndex">the index of the row, set in the exceptions (i.e. CsvInjectionException)</param>
        /// <exception cref="CsvException"></exception>
        public void Build(T model, int rowIndex)
        {
            length = 0;
            try
            {
                for (int i = 0; i < bindings.Length; i++)
                {
                    if (i > 0)
                    {
                        Append(delimiter);
                    }
                    var binding = bindings[i];
                    current = binding;
                    binding.Accessor.AppendValue(model, this, binding);
                }
            }
            catch (CsvException exception)
            {
                exception.SetRowIndex(rowIndex);
                throw;
            }
        }

        /// <summary>
        /// The line, as a string
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return new string(buffer, 0, length);
        }

        /// <summary>
        /// Writes the line, without the new line
        /// </summary>
        /// <param name="writer"></param>
        public void WriteTo(TextWriter writer)
        {
            writer.Write(buffer, 0, length);
        }

        /// <summary>
        /// Writes the line, without the new line
        /// </summary>
        /// <param name="writer"></param>
        public Task WriteToAsync(TextWriter writer)
        {
            return writer.WriteAsync(buffer, 0, length);
        }

        /// <summary>
        /// Appends a text field (the value of a column of type string), escaped if necessary and protected
        /// from CSV injection (see CsvOptions.InjectionProtection). A null value is an empty field.
        /// </summary>
        /// <param name="value"></param>
        /// <exception cref="CsvInjectionException"></exception>
        public void AppendText(string? value)
        {
            if (injection != CsvInjectionProtection.None
                && value != null
                && value.Length > 0
                && Array.IndexOf(injectionChars, value[0]) >= 0)
            {
                AppendProtected(value);
                return;
            }
            AppendField(value);
        }

        /// <summary>
        /// Appends the converted value of a column: a text field when the column is of type string, otherwise a field
        /// </summary>
        /// <param name="value"></param>
        /// <param name="binding"></param>
        public void AppendValue(string? value, CsvColumnBinding<T> binding)
        {
            if (binding.IsText)
            {
                AppendText(value);
            }
            else
            {
                AppendField(value);
            }
        }

        /// <summary>
        /// Appends a text value that starts with an injection character
        /// </summary>
        private void AppendProtected(string value)
        {
            switch (injection)
            {
                case CsvInjectionProtection.Strip:
                    AppendField(value.TrimStart(injectionChars));
                    break;
                case CsvInjectionProtection.Throw:
                    throw new CsvInjectionException(current?.Column.ColumnName ?? string.Empty, value);
                default:
                    // the apostrophe makes the value a text, and the quotes keep it in the same cell
                    var escaped = "'" + value;
                    if (quoting && quoteMode != CsvQuoteMode.None)
                    {
                        AppendEscaped(escaped.AsSpan(), true);
                    }
                    else
                    {
                        AppendField(escaped);
                    }
                    break;
            }
        }

        /// <summary>
        /// Appends a field, escaped if necessary. A null value is an empty field.
        /// </summary>
        /// <param name="value"></param>
        public void AppendField(string? value)
        {
            if (quoteMode != CsvQuoteMode.Minimal)
            {
                AppendFieldByMode(value, current != null && current.Quoted);
                return;
            }
            AppendMinimal(value);
        }

        /// <summary>
        /// Appends a field as QuoteMode.Minimal: escaped if necessary. A null value is an empty field.
        /// </summary>
        private void AppendMinimal(string? value)
        {
            if (value == null || value.Length == 0)
            {
                return;
            }
            if (!NeedsEscape(value.AsSpan()))
            {
                Append(value);
            }
            else if (escapeInBuffer)
            {
                AppendEscaped(value.AsSpan());
            }
            else
            {
                Append(value.EnclosedInQuotesIfNecessary(options));
            }
        }

        /// <summary>
        /// Appends a field with the QuoteMode of the options (not Minimal)
        /// </summary>
        /// <param name="value"></param>
        /// <param name="enclose">true to enclose the field in quotes in any case (QuoteMode All and NonNumeric)</param>
        /// <exception cref="InvalidColumnValueException">QuoteMode.None, a value that contains the delimiter or a new line</exception>
        private void AppendFieldByMode(string? value, bool enclose)
        {
            if (quoteMode == CsvQuoteMode.None)
            {
                if (value == null || value.Length == 0)
                {
                    return;
                }
                if (value.IndexOf('\r') >= 0
                    || value.IndexOf('\n') >= 0
                    || (delimiter.Length > 0 && value.IndexOf(delimiter, StringComparison.Ordinal) >= 0))
                {
                    throw new InvalidColumnValueException(current?.Column.ColumnName ?? string.Empty, value,
                        $"The value '{value}' contains the delimiter or a new line and it cannot be written without quotes ({nameof(CsvQuoteMode)}.{nameof(CsvQuoteMode.None)}).");
                }
                Append(value);
                return;
            }
            if (enclose && quoting)
            {
                AppendEscaped((value ?? string.Empty).AsSpan(), true);
                return;
            }
            AppendMinimal(value);
        }

        /// <summary>
        /// Appends a null value of a column: the NullValueOnWrite of the options, when it is set, without quotes;
        /// otherwise the text of the converter (an empty field when it is null or empty, as before)
        /// </summary>
        /// <param name="converted">the text of the converter for the null value</param>
        /// <param name="binding"></param>
        public void AppendNull(string? converted, CsvColumnBinding<T> binding)
        {
            if (nullValueOnWrite.Length > 0)
            {
                if (quoteMode == CsvQuoteMode.None)
                {
                    AppendFieldByMode(nullValueOnWrite, false);
                }
                else
                {
                    AppendMinimal(nullValueOnWrite);
                }
                return;
            }
            if (converted != null && converted.Length > 0)
            {
                AppendValue(converted, binding);
            }
        }

        /// <summary>
        /// The header line: the names of the columns, enclosed in quotes with QuoteMode All and NonNumeric,
        /// otherwise as CsvOptions.AsColumnsHeaderLine
        /// </summary>
        /// <returns></returns>
        public string BuildHeader()
        {
            if (!quoting || (quoteMode != CsvQuoteMode.All && quoteMode != CsvQuoteMode.NonNumeric))
            {
                return options.AsColumnsHeaderLine();
            }
            length = 0;
            for (int i = 0; i < bindings.Length; i++)
            {
                if (i > 0)
                {
                    Append(delimiter);
                }
                AppendEscaped((bindings[i].Column.ColumnName ?? string.Empty).AsSpan(), true);
            }
            return ToString();
        }

        /// <summary>
        /// Appends a value as string.Format(culture, "{0}", value) does, with the format of the column (if any)
        /// </summary>
        public void AppendFormatted<TValue>(TValue value, string? format, IFormatProvider culture)
            where TValue : struct
        {
#if NET6_0_OR_GREATER
            if (value is ISpanFormattable && quoteMode == CsvQuoteMode.Minimal)
            {
                var start = length;
                int written;
                while (!TryFormat(value, new Span<char>(buffer, length, buffer.Length - length), out written, format, culture))
                {
                    Grow(buffer.Length);
                }
                length += written;
                if (written > 0 && NeedsEscape(new ReadOnlySpan<char>(buffer, start, written)))
                {
                    var field = new string(buffer, start, written);
                    length = start;
                    AppendField(field);
                }
                return;
            }
#endif
            AppendField(value is IFormattable formattable ? formattable.ToString(format, culture) : value.ToString());
        }

#if NET6_0_OR_GREATER
        /// <summary>
        /// ISpanFormattable.TryFormat of the value: the common types call their TryFormat directly,
        /// so the value is not boxed (the checks of typeof(TValue) are removed by the JIT)
        /// </summary>
        private static bool TryFormat<TValue>(TValue value, Span<char> destination, out int written, string? format, IFormatProvider culture)
            where TValue : struct
        {
            if (typeof(TValue) == typeof(int)) return Unsafe.As<TValue, int>(ref value).TryFormat(destination, out written, format, culture);
            if (typeof(TValue) == typeof(long)) return Unsafe.As<TValue, long>(ref value).TryFormat(destination, out written, format, culture);
            if (typeof(TValue) == typeof(decimal)) return Unsafe.As<TValue, decimal>(ref value).TryFormat(destination, out written, format, culture);
            if (typeof(TValue) == typeof(double)) return Unsafe.As<TValue, double>(ref value).TryFormat(destination, out written, format, culture);
            if (typeof(TValue) == typeof(float)) return Unsafe.As<TValue, float>(ref value).TryFormat(destination, out written, format, culture);
            if (typeof(TValue) == typeof(DateTime)) return Unsafe.As<TValue, DateTime>(ref value).TryFormat(destination, out written, format, culture);
            if (typeof(TValue) == typeof(DateTimeOffset)) return Unsafe.As<TValue, DateTimeOffset>(ref value).TryFormat(destination, out written, format, culture);
            if (typeof(TValue) == typeof(short)) return Unsafe.As<TValue, short>(ref value).TryFormat(destination, out written, format, culture);
            if (typeof(TValue) == typeof(byte)) return Unsafe.As<TValue, byte>(ref value).TryFormat(destination, out written, format, culture);
            if (typeof(TValue) == typeof(uint)) return Unsafe.As<TValue, uint>(ref value).TryFormat(destination, out written, format, culture);
            if (typeof(TValue) == typeof(ulong)) return Unsafe.As<TValue, ulong>(ref value).TryFormat(destination, out written, format, culture);
            if (typeof(TValue) == typeof(ushort)) return Unsafe.As<TValue, ushort>(ref value).TryFormat(destination, out written, format, culture);
            if (typeof(TValue) == typeof(sbyte)) return Unsafe.As<TValue, sbyte>(ref value).TryFormat(destination, out written, format, culture);
            if (typeof(TValue) == typeof(TimeSpan)) return Unsafe.As<TValue, TimeSpan>(ref value).TryFormat(destination, out written, format, culture);
            if (typeof(TValue) == typeof(DateOnly)) return Unsafe.As<TValue, DateOnly>(ref value).TryFormat(destination, out written, format, culture);
            if (typeof(TValue) == typeof(TimeOnly)) return Unsafe.As<TValue, TimeOnly>(ref value).TryFormat(destination, out written, format, culture);
            return ((ISpanFormattable)value).TryFormat(destination, out written, format, culture);
        }
#endif

        /// <summary>
        /// Appends the field escaped as EnclosedInQuotesIfNecessary does, without creating the escaped string:
        /// the backslashes doubled (AllowBackSlashToEscapeQuote) and, when the field contains the delimiter,
        /// a quote or a new line, the field enclosed in quotes with the quotes escaped.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="enclose">true to enclose the field in quotes in any case</param>
        private void AppendEscaped(ReadOnlySpan<char> value, bool enclose = false)
        {
            var enclosed = quoting
                && (enclose
                    || delimiter.Length == 0
                    || value.IndexOf(delimiter.AsSpan()) >= 0
                    || value.IndexOf(quote) >= 0
                    || value.IndexOfAny('\r', '\n') >= 0
                    || (multiCharDelimiter && StringExtensions.EndsWithDelimiterStart(value, delimiter.AsSpan())));

            // at most each char escaped, and the quotes
            var required = value.Length * 2 + 2;
            if (length + required > buffer.Length)
            {
                Grow(required);
            }
            var chars = buffer;
            var position = length;
            if (enclosed)
            {
                chars[position++] = quote;
            }
            for (int i = 0; i < value.Length; i++)
            {
                var c = value[i];
                if (backslash && c == '\\')
                {
                    chars[position++] = '\\';
                }
                else if (enclosed && c == quote)
                {
                    chars[position++] = backslash ? '\\' : quote;
                }
                chars[position++] = c;
            }
            if (enclosed)
            {
                chars[position++] = quote;
            }
            length = position;
        }

        private bool NeedsEscape(ReadOnlySpan<char> value)
        {
            if (alwaysEscape)
            {
                return true;
            }
            if (escapeChars.Length > 0 && value.IndexOfAny(new ReadOnlySpan<char>(escapeChars)) >= 0)
            {
                return true;
            }
            return multiCharDelimiter
                && (value.IndexOf(delimiter.AsSpan()) >= 0 || StringExtensions.EndsWithDelimiterStart(value, delimiter.AsSpan()));
        }

        private void Append(string value)
        {
            if (length + value.Length > buffer.Length)
            {
                Grow(value.Length);
            }
            value.CopyTo(0, buffer, length, value.Length);
            length += value.Length;
        }

        private void Grow(int required)
        {
            Array.Resize(ref buffer, Math.Max(buffer.Length * 2, length + required));
        }
    }
}
