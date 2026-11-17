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
    /// Builds a line field by field in a reusable buffer, so a line is written without creating strings.
    /// The fields are escaped as EnclosedInQuotesIfNecessary does, with the QuoteMode, the CSV injection protection
    /// and the NullValueOnWrite of the options. Used by the typed writer (<see cref="CsvLineBuilder{T}"/>)
    /// and by the writer of the records without a model.
    /// </summary>
    internal class CsvFieldBuilder
    {
        private readonly CsvOptions options;
        private readonly char[] escapeChars;
        private readonly bool multiCharDelimiter;
        private readonly bool alwaysEscape;
        private readonly bool backslash;
        private readonly char quote;
        // true when the escaped field is written in the buffer (see AppendEscaped),
        // false in the cases where only EnclosedInQuotesIfNecessary gives the same result
        private readonly bool escapeInBuffer;
        private readonly CsvInjectionProtection injection;
        private readonly char[] injectionChars;
        private char[] buffer = new char[256];

        protected readonly string delimiter;
        protected readonly bool quoting;
        protected readonly CsvQuoteMode quoteMode;
        protected int length;

        /// <summary>
        /// True when the field that is appended is always enclosed in quotes (see CsvQuoteMode All and NonNumeric)
        /// </summary>
        protected bool currentQuoted;

        /// <summary>
        /// The name of the column of the field that is appended, for the exceptions
        /// </summary>
        protected string? currentColumnName;

        private readonly string nullValueOnWrite;

        public CsvFieldBuilder(CsvOptions options)
        {
            this.options = options;
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
        /// True when the fields of a column of the type are always enclosed in quotes with the QuoteMode of the options
        /// </summary>
        /// <param name="type">the type of the column, or null when it is not known</param>
        /// <returns></returns>
        public bool IsQuoted(Type? type)
        {
            return quoteMode == CsvQuoteMode.All || (quoteMode == CsvQuoteMode.NonNumeric && (type == null || !IsNumber(type)));
        }

        /// <summary>
        /// True when the type is a number: with QuoteMode.NonNumeric its fields are quoted only when necessary
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        internal static bool IsNumber(Type type)
        {
            type = Nullable.GetUnderlyingType(type) ?? type;
            return type == typeof(int) || type == typeof(long) || type == typeof(short) || type == typeof(sbyte)
                || type == typeof(uint) || type == typeof(ulong) || type == typeof(ushort) || type == typeof(byte)
                || type == typeof(decimal) || type == typeof(double) || type == typeof(float)
                || type == typeof(System.Numerics.BigInteger);
        }

        /// <summary>
        /// Starts a new line
        /// </summary>
        public void Clear()
        {
            length = 0;
        }

        /// <summary>
        /// Appends the delimiter, between two fields
        /// </summary>
        public void AppendDelimiter()
        {
            Append(delimiter);
        }

        /// <summary>
        /// Sets the column of the fields that are appended next: its name, for the exceptions,
        /// and if its fields are always enclosed in quotes (see <see cref="IsQuoted(Type)"/>)
        /// </summary>
        /// <param name="columnName"></param>
        /// <param name="quoted"></param>
        public void SetColumn(string? columnName, bool quoted)
        {
            currentColumnName = columnName;
            currentQuoted = quoted;
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
                    throw new CsvInjectionException(currentColumnName ?? string.Empty, value);
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
                AppendFieldByMode(value, currentQuoted);
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
                Append(StringExtensions.EnclosedInQuotesIfNecessary(value, options));
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
                    throw new InvalidColumnValueException(currentColumnName ?? string.Empty, value,
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
        /// Appends the NullValueOnWrite of the options, without quotes, when it is set
        /// </summary>
        /// <returns>false when NullValueOnWrite is not set: nothing is appended</returns>
        public bool TryAppendNullValueOnWrite()
        {
            if (nullValueOnWrite.Length == 0)
            {
                return false;
            }
            if (quoteMode == CsvQuoteMode.None)
            {
                AppendFieldByMode(nullValueOnWrite, false);
            }
            else
            {
                AppendMinimal(nullValueOnWrite);
            }
            return true;
        }

        /// <summary>
        /// Appends a name of the header, enclosed in quotes in any case: QuoteMode All and NonNumeric
        /// </summary>
        /// <param name="name"></param>
        public void AppendHeaderName(string? name)
        {
            AppendEscaped((name ?? string.Empty).AsSpan(), true);
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

        protected void Append(string value)
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
