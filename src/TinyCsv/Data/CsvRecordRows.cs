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
    using System.Collections;
    using System.Collections.Generic;
    using System.Data;
    using System.Data.Common;
    using System.Linq;
    using System.Reflection;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// The rows of the records written without a model (see TinyCsvRecords): the names of the columns,
    /// their types when they are known, and the values of the current row
    /// </summary>
    internal abstract class CsvRecordRows
    {
        /// <summary>
        /// The names of the columns, in the order they are written
        /// </summary>
        public string[] Names { get; protected set; } = Array.Empty<string>();

        /// <summary>
        /// The type of each column, or null when it is not known (the type of the value is used)
        /// </summary>
        public Type?[] Types { get; protected set; } = Array.Empty<Type?>();

        /// <summary>
        /// Reads the first row, if any, to know the columns. Called once, before the other methods.
        /// </summary>
        public abstract void Start();

        /// <summary>
        /// <see cref="Start"/>, asynchronously
        /// </summary>
        public virtual Task StartAsync(CancellationToken cancellationToken)
        {
            Start();
            return Task.CompletedTask;
        }

        /// <summary>
        /// Moves to the next row
        /// </summary>
        public abstract bool Read();

        /// <summary>
        /// Moves to the next row asynchronously
        /// </summary>
        public virtual Task<bool> ReadAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(Read());
        }

        /// <summary>
        /// The value of the column of the current row: null or DBNull for a null value
        /// </summary>
        public abstract object? GetValue(int column);

        /// <summary>
        /// The indexes of the columns to write: all of them, or the columns of the list, in its order
        /// </summary>
        /// <param name="all">the names of all the columns</param>
        /// <param name="columns">the columns to write, or null for all of them</param>
        /// <param name="required">true when a column of the list must exist (DataTable, IDataReader)</param>
        /// <exception cref="ArgumentException">a column of the list does not exist</exception>
        protected static int[] Select(IList<string> all, IList<string>? columns, bool required)
        {
            if (columns == null)
            {
                return Enumerable.Range(0, all.Count).ToArray();
            }
            var indexes = new int[columns.Count];
            for (int i = 0; i < columns.Count; i++)
            {
                indexes[i] = -1;
                for (int j = 0; j < all.Count; j++)
                {
                    if (string.Equals(all[j], columns[i], StringComparison.OrdinalIgnoreCase))
                    {
                        indexes[i] = j;
                        break;
                    }
                }
                if (indexes[i] < 0 && required)
                {
                    throw new ArgumentException($"The column '{columns[i]}' does not exist.", nameof(CsvRecordOptions.ColumnsToWrite));
                }
            }
            return indexes;
        }
    }

    /// <summary>
    /// The rows of a DataTable: the deleted rows are not written
    /// </summary>
    internal sealed class CsvDataTableRows : CsvRecordRows
    {
        private readonly DataTable table;
        private readonly IList<string>? columns;
        private int[] indexes = Array.Empty<int>();
        private int row = -1;

        public CsvDataTableRows(DataTable table, IList<string>? columns)
        {
            this.table = table ?? throw new ArgumentNullException(nameof(table));
            this.columns = columns;
        }

        public override void Start()
        {
            var all = table.Columns.Cast<DataColumn>().ToArray();
            indexes = Select(all.Select(c => c.ColumnName).ToArray(), columns, true);
            Names = indexes.Select(i => all[i].ColumnName).ToArray();
            Types = indexes.Select(i => (Type?)all[i].DataType).ToArray();
        }

        public override bool Read()
        {
            while (++row < table.Rows.Count)
            {
                if (table.Rows[row].RowState != DataRowState.Deleted)
                {
                    return true;
                }
            }
            return false;
        }

        public override object? GetValue(int column) => table.Rows[row][indexes[column]];
    }

    /// <summary>
    /// The rows of an IDataReader, read one at a time (streamed)
    /// </summary>
    internal sealed class CsvDataReaderRows : CsvRecordRows
    {
        private readonly IDataReader reader;
        private readonly IList<string>? columns;
        private int[] indexes = Array.Empty<int>();

        public CsvDataReaderRows(IDataReader reader, IList<string>? columns)
        {
            this.reader = reader ?? throw new ArgumentNullException(nameof(reader));
            this.columns = columns;
        }

        public override void Start()
        {
            var all = Enumerable.Range(0, reader.FieldCount).Select(reader.GetName).ToArray();
            indexes = Select(all, columns, true);
            Names = indexes.Select(i => all[i]).ToArray();
            Types = indexes.Select(i => (Type?)reader.GetFieldType(i)).ToArray();
        }

        public override bool Read() => reader.Read();

        public override Task<bool> ReadAsync(CancellationToken cancellationToken)
        {
            return reader is DbDataReader dbDataReader ? dbDataReader.ReadAsync(cancellationToken) : base.ReadAsync(cancellationToken);
        }

        public override object? GetValue(int column) => reader.GetValue(indexes[column]);
    }

    /// <summary>
    /// The rows of a sequence of records with named values: dictionaries, ExpandoObject (dynamic) or objects
    /// with public properties (i.e. anonymous types). The columns are the keys of the first record:
    /// a key missing from a record is a null value, a key that is not a column is not written.
    /// </summary>
    internal sealed class CsvKeyedRows : CsvRecordRows
    {
        private readonly IEnumerator enumerator;
        private readonly IList<string>? columns;
        private bool pending;
        private bool hasPending;
        private Func<string, object?> current = _ => null;

        public CsvKeyedRows(IEnumerable records, IList<string>? columns)
        {
            enumerator = (records ?? throw new ArgumentNullException(nameof(records))).GetEnumerator();
            this.columns = columns;
        }

        public override void Start()
        {
            // the first record gives the columns, and it is written as the first row
            pending = enumerator.MoveNext();
            hasPending = true;
            IList<string> keys = Array.Empty<string>();
            if (pending)
            {
                current = Lookup(enumerator.Current, out keys);
            }
            Names = columns?.ToArray() ?? keys.ToArray();
            Types = new Type?[Names.Length];
        }

        public override bool Read()
        {
            if (hasPending)
            {
                hasPending = false;
                return pending;
            }
            if (!enumerator.MoveNext())
            {
                return false;
            }
            current = Lookup(enumerator.Current, out _);
            return true;
        }

        public override object? GetValue(int column) => current(Names[column]);

        /// <summary>
        /// The values of the record by name, and its names
        /// </summary>
        private static Func<string, object?> Lookup(object? record, out IList<string> keys)
        {
            switch (record)
            {
                case null:
                    keys = Array.Empty<string>();
                    return _ => null;
                case IDictionary<string, object?> dictionary:
                    keys = dictionary.Keys.ToArray();
                    return name => dictionary.TryGetValue(name, out var value) ? value : null;
                case IDictionary<string, string?> strings:
                    keys = strings.Keys.ToArray();
                    return name => strings.TryGetValue(name, out var value) ? value : null;
                case IReadOnlyDictionary<string, object?> readOnly:
                    keys = readOnly.Keys.ToArray();
                    return name => readOnly.TryGetValue(name, out var value) ? value : null;
                case IDictionary nonGeneric:
                    keys = nonGeneric.Keys.Cast<object>().Select(k => Convert.ToString(k, System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty).ToArray();
                    return name => nonGeneric.Contains(name) ? nonGeneric[name] : null;
                default:
                    var properties = record.GetType()
                        .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                        .Where(p => p.CanRead && p.GetIndexParameters().Length == 0)
                        .ToArray();
                    keys = properties.Select(p => p.Name).ToArray();
                    return name =>
                    {
                        var property = Array.Find(properties, p => p.Name == name);
                        return property?.GetValue(record);
                    };
            }
        }
    }
}
