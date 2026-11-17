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
    using Mafesoft.TinyCsv.Exceptions;

    /// <summary>
    /// Builds the lines of the models in a reusable buffer, so a line is written without creating strings.
    /// The fields are escaped as EnclosedInQuotesIfNecessary does (see <see cref="CsvFieldBuilder"/>).
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal sealed class CsvLineBuilder<T> : CsvFieldBuilder
    {
        private readonly CsvOptions<T> options;
        private readonly CsvColumnBinding<T>[] bindings;

        public CsvLineBuilder(CsvOptions<T> options)
            : base(options)
        {
            this.options = options;
            bindings = CsvColumnBinding<T>.Create(options);
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
                    currentQuoted = binding.Quoted;
                    currentColumnName = binding.Column.ColumnName;
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
        /// Appends a null value of a column: the NullValueOnWrite of the options, when it is set, without quotes;
        /// otherwise the text of the converter (an empty field when it is null or empty, as before)
        /// </summary>
        /// <param name="converted">the text of the converter for the null value</param>
        /// <param name="binding"></param>
        public void AppendNull(string? converted, CsvColumnBinding<T> binding)
        {
            if (TryAppendNullValueOnWrite())
            {
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
                AppendHeaderName(bindings[i].Column.ColumnName);
            }
            return ToString();
        }
    }
}
