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
        private char[] buffer = new char[256];
        private int length;

        public CsvLineBuilder(CsvOptions<T> options)
        {
            this.options = options;
            bindings = CsvColumnBinding<T>.Create(options);
            delimiter = options.Delimiter ?? string.Empty;

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
            Append(NeedsEscape(value.AsSpan()) ? value.EnclosedInQuotesIfNecessary(options) : value);
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
                while (!((ISpanFormattable)value).TryFormat(new Span<char>(buffer, length, buffer.Length - length), out written, format, culture))
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
            return multiCharDelimiter && value.IndexOf(delimiter.AsSpan()) >= 0;
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
