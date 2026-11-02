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
    using TinyCsv.Exceptions;
    using TinyCsv.Extensions;

    using System.IO;

    /// <summary>
    /// Converts the records of a stream to models, raising the events.
    /// It is shared by the sync and async load methods and it keeps the state of one read:
    /// the record index and the fields of the columns found in the header record (see MapColumnsByHeader).
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal sealed class CsvModelReader<T> where T : class, new()
    {
        private readonly CsvOptions<T> options;
        private readonly int columnsCount;
        private int[] fieldIndexes;
        private int index;

        /// <summary>
        /// The reader of the records
        /// </summary>
        public TinyCsvDataReader<T> DataReader { get; }

        /// <summary>
        /// Create a CsvModelReader and raises the Start event
        /// </summary>
        /// <param name="options"></param>
        /// <param name="streamReader"></param>
        /// <exception cref="System.InvalidOperationException">MapColumnsByHeader without HasHeaderRecord</exception>
        public CsvModelReader(CsvOptions<T> options, StreamReader streamReader)
        {
            options.ThrowsIfMapColumnsByHeaderWithoutHeader();
            this.options = options;
            this.columnsCount = options.Columns.Count;
            DataReader = new TinyCsvDataReader<T>(options, streamReader);
            options.Handlers.OnStart();
        }

        /// <summary>
        /// Reads a record: the other records return true and the model, while the header record
        /// and the records with bad data passed to BadDataFound return false.
        /// After the model is used, call OnModelRead.
        /// </summary>
        /// <param name="line">the record</param>
        /// <param name="model">the model, when the method returns true</param>
        /// <returns></returns>
        /// <exception cref="CsvException">bad data, when BadDataFound is null</exception>
        public bool TryReadModel(string line, out T model)
        {
            var index = this.index++;
            var dataReader = DataReader;
            options.Handlers.Read.OnRowReading(index, line);

            if (index == 0 && options.HasHeaderRecord)
            {
                options.Handlers.Read.OnRowHeader(index, line);
                if (options.MapColumnsByHeader)
                {
                    fieldIndexes = options.GetFieldIndexesByHeader(dataReader.GetFieldsByLine(line, columnsCount));
                }
                model = null;
                return false;
            }

            try
            {
                var fields = dataReader.GetFieldsByLine(line, columnsCount);
                if (options.ValidateColumnCount && columnsCount > fields.Length)
                {
                    throw new InvalidColumnCountException($"Invalid column count {fields.Length}, required {columnsCount} columns.");
                }

                model = fields.GetModelFromStringArray(options, fieldIndexes);
                return true;
            }
            catch (CsvException exception)
            {
                exception.SetRow(index, line);
                var badDataFound = options.BadDataFound;
                if (badDataFound == null)
                {
                    throw;
                }

                badDataFound(exception);
                model = null;
                return false;
            }
        }

        /// <summary>
        /// Raises the RowRead event of the last record
        /// </summary>
        /// <param name="model"></param>
        /// <param name="line"></param>
        public void OnModelRead(T model, string line)
        {
            options.Handlers.Read.OnRowRead(index - 1, model, line);
        }

        /// <summary>
        /// Raises the Completed event
        /// </summary>
        public void OnCompleted()
        {
            options.Handlers.OnCompleted(index);
        }
    }
}
