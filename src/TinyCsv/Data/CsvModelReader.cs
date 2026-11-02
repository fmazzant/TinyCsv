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

    using System;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Converts the records of a stream to models, raising the events.
    /// It is shared by the sync and async load methods and it keeps the state of one read:
    /// the record index and the fields of the columns found in the header record (see MapColumnsByHeader).
    /// The string of a record is created only when it is needed: by the events, by a custom SkipRow and by the exceptions.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal sealed class CsvModelReader<T> where T : class, new()
    {
        private readonly CsvOptions<T> options;
        private readonly int columnsCount;
        private readonly CsvColumnBinding<T>[] bindings;
        private readonly CsvRecordReader records;
        private CsvField[] fields;
        private int[] fieldIndexes;
        private int recordIndex;
        private int index;
        private string line;

        /// <summary>
        /// The reader of the fields
        /// </summary>
        public TinyCsvDataReader<T> DataReader { get; }

        /// <summary>
        /// Create a CsvModelReader and raises the Start event
        /// </summary>
        /// <param name="options"></param>
        /// <param name="textReader"></param>
        /// <exception cref="System.InvalidOperationException">MapColumnsByHeader without HasHeaderRecord</exception>
        public CsvModelReader(CsvOptions<T> options, TextReader textReader)
        {
            options.ThrowsIfMapColumnsByHeaderWithoutHeader();
            this.options = options;
            this.columnsCount = options.Columns.Count;
            this.bindings = CsvColumnBinding<T>.Create(options);
            this.records = new CsvRecordReader(textReader, options);
            DataReader = new TinyCsvDataReader<T>(options, textReader);
            options.Handlers.OnStart();
        }

        /// <summary>
        /// The current record, as a string. It is created the first time it is used.
        /// </summary>
        public string Line => line ?? (line = records.CurrentToString());

        /// <summary>
        /// Reads the next record that is not skipped. Returns false at the end of the stream.
        /// </summary>
        /// <returns></returns>
        public bool Read()
        {
            while (records.Read())
            {
                line = null;
                if (!IsToSkip(recordIndex++))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Reads the next record that is not skipped. Returns false at the end of the stream.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<bool> ReadAsync(CancellationToken cancellationToken)
        {
            while (await records.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                cancellationToken.ThrowIfCancellationRequested();
                line = null;
                if (!IsToSkip(recordIndex++))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Defines if the current record is to skip: the first RowsToSkip records, the comments
        /// (when AllowComment, otherwise they are rejected by ReadFields) and the records selected by SkipRow
        /// </summary>
        /// <param name="recordIndex">the record index, starting from 0</param>
        /// <returns></returns>
        private bool IsToSkip(int recordIndex)
        {
            if (recordIndex < options.RowsToSkip)
            {
                return true;
            }
            if (options.AllowComment && records.CurrentLength > 0 && records.Buffer[records.CurrentStart] == options.Comment)
            {
                return true;
            }
            var skipRow = options.SkipRow;
            if (skipRow == null || ReferenceEquals(skipRow, CsvOptions.NotSkipRow))
            {
                return false;
            }
            return skipRow(Line, recordIndex);
        }

        /// <summary>
        /// Reads the current record: the other records return true and the model, while the header record
        /// and the records with bad data passed to BadDataFound return false.
        /// After the model is used, call OnModelRead.
        /// </summary>
        /// <param name="model">the model, when the method returns true</param>
        /// <returns></returns>
        /// <exception cref="CsvException">bad data, when BadDataFound is null</exception>
        public bool TryReadModel(out T model)
        {
            var index = this.index++;
            var handlers = options.Handlers.Read;
            if (handlers.Enabled)
            {
                handlers.OnRowReading(index, Line);
            }

            if (index == 0 && options.HasHeaderRecord)
            {
                if (handlers.Enabled)
                {
                    handlers.OnRowHeader(index, Line);
                }
                if (options.MapColumnsByHeader)
                {
                    fieldIndexes = options.GetFieldIndexesByHeader(DataReader.GetFieldsByLine(Line, columnsCount));
                }
                model = null;
                return false;
            }

            try
            {
                var count = DataReader.ReadFields(records.Buffer, records.CurrentStart, records.CurrentLength, columnsCount, ref fields);
                if (options.ValidateColumnCount && columnsCount > count)
                {
                    throw new InvalidColumnCountException($"Invalid column count {count}, required {columnsCount} columns.");
                }

                model = CreateModel(count);
                return true;
            }
            catch (CsvException exception)
            {
                exception.SetRow(index, Line);
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
        /// Creates the model from the fields of the record, as GetModelFromStringArray does
        /// </summary>
        /// <param name="count">the number of fields</param>
        /// <returns></returns>
        /// <exception cref="InvalidColumnValueException"></exception>
        private T CreateModel(int count)
        {
            var model = new T();
            var validateValues = options.ValidateValues;
            for (int position = 0; position < bindings.Length; position++)
            {
                var binding = bindings[position];
                var fieldIndex = fieldIndexes == null ? binding.Column.ColumnIndex : fieldIndexes[position];
                if (fieldIndex < count)
                {
                    var value = fields[fieldIndex];
                    try
                    {
                        binding.Accessor.SetValue(model, value, binding, validateValues);
                    }
                    catch (Exception exception) when (!(exception is CsvException))
                    {
                        var column = binding.Column;
                        var text = value.ToString();
                        throw new InvalidColumnValueException(column.ColumnName, text,
                            $"The value '{text}' cannot be converted to {column.ColumnType.Name}: {exception.Message}", exception);
                    }
                }
            }
            return model;
        }

        /// <summary>
        /// Raises the RowRead event of the last record
        /// </summary>
        /// <param name="model"></param>
        public void OnModelRead(T model)
        {
            var handlers = options.Handlers.Read;
            if (handlers.Enabled)
            {
                handlers.OnRowRead(index - 1, model, Line);
            }
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
