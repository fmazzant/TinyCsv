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
        }

        /// <summary>
        /// Builds the line of the model
        /// </summary>
        /// <param name="model"></param>
        public void Build(T model)
        {
            length = 0;
            for (int i = 0; i < bindings.Length; i++)
            {
                if (i > 0)
                {
                    Append(delimiter);
                }
                var binding = bindings[i];
                binding.Accessor.AppendValue(model, this, binding);
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
        /// Appends a field, escaped if necessary. A null value is an empty field.
        /// </summary>
        /// <param name="value"></param>
        public void AppendField(string? value)
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
        /// Appends a value as string.Format(culture, "{0}", value) does, with the format of the column (if any)
        /// </summary>
        public void AppendFormatted<TValue>(TValue value, string? format, IFormatProvider culture)
            where TValue : struct
        {
#if NET6_0_OR_GREATER
            if (value is ISpanFormattable)
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
        private void AppendEscaped(ReadOnlySpan<char> value)
        {
            var enclosed = quoting
                && (delimiter.Length == 0
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
