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
    using System.Globalization;

    /// <summary>
    /// A data reader over a CSV file: the records are read one at a time, so the memory does not grow with the
    /// number of rows (i.e. <c>SqlBulkCopy.WriteToServer(reader)</c> or <c>DataTable.Load(reader)</c>).
    /// Created by <see cref="TinyCsvRecords.CreateDataReader(string)"/> (the values are strings, or the types of
    /// <see cref="CsvRecordOptions.ColumnTypes"/>) and by <see cref="TinyCsv{T}.CreateDataReader(string)"/>
    /// (the columns and the converted values of the model). Dispose it at the end.
    /// </summary>
    public sealed class TinyCsvDbDataReader : DbDataReader
    {
        private readonly CsvDataReaderSource source;
        private readonly StringComparer comparer;
        private bool closed;
        private bool hasRow;

        internal TinyCsvDbDataReader(CsvDataReaderSource source, StringComparer? comparer)
        {
            this.source = source;
            this.comparer = comparer ?? StringComparer.OrdinalIgnoreCase;
        }

        /// <inheritdoc />
        public override int Depth => 0;

        /// <inheritdoc />
        public override int FieldCount => source.Names.Length;

        /// <inheritdoc />
        public override bool HasRows => source.HasRows;

        /// <inheritdoc />
        public override bool IsClosed => closed;

        /// <inheritdoc />
        public override int RecordsAffected => -1;

        /// <inheritdoc />
        public override object this[int ordinal] => GetValue(ordinal);

        /// <inheritdoc />
        public override object this[string name] => GetValue(GetOrdinal(name));

        /// <inheritdoc />
        public override bool Read()
        {
            if (closed)
            {
                throw new InvalidOperationException("The data reader is closed.");
            }
            hasRow = source.Read();
            return hasRow;
        }

        /// <inheritdoc />
        public override bool NextResult() => false;

        /// <inheritdoc />
        public override string GetName(int ordinal) => source.Names[ordinal];

        /// <inheritdoc />
        public override int GetOrdinal(string name)
        {
            for (int i = 0; i < source.Names.Length; i++)
            {
                if (comparer.Equals(source.Names[i], name))
                {
                    return i;
                }
            }
            throw new IndexOutOfRangeException($"The column '{name}' does not exist.");
        }

        /// <inheritdoc />
        public override Type GetFieldType(int ordinal) => source.Types[ordinal];

        /// <inheritdoc />
        public override string GetDataTypeName(int ordinal) => GetFieldType(ordinal).Name;

        /// <inheritdoc />
        public override object GetValue(int ordinal)
        {
            if (!hasRow)
            {
                throw new InvalidOperationException("There is no current row: call Read first.");
            }
            if (ordinal < 0 || ordinal >= source.Names.Length)
            {
                throw new IndexOutOfRangeException($"The ordinal {ordinal} is out of range.");
            }
            return source.GetValue(ordinal) ?? DBNull.Value;
        }

        /// <inheritdoc />
        public override int GetValues(object[] values)
        {
            var count = Math.Min(values.Length, FieldCount);
            for (int i = 0; i < count; i++)
            {
                values[i] = GetValue(i);
            }
            return count;
        }

        /// <inheritdoc />
        public override bool IsDBNull(int ordinal) => GetValue(ordinal) is DBNull;

        /// <inheritdoc />
        public override bool GetBoolean(int ordinal) => (bool)GetValue(ordinal);

        /// <inheritdoc />
        public override byte GetByte(int ordinal) => (byte)GetValue(ordinal);

        /// <inheritdoc />
        public override char GetChar(int ordinal) => (char)GetValue(ordinal);

        /// <inheritdoc />
        public override DateTime GetDateTime(int ordinal) => (DateTime)GetValue(ordinal);

        /// <inheritdoc />
        public override decimal GetDecimal(int ordinal) => (decimal)GetValue(ordinal);

        /// <inheritdoc />
        public override double GetDouble(int ordinal) => (double)GetValue(ordinal);

        /// <inheritdoc />
        public override float GetFloat(int ordinal) => (float)GetValue(ordinal);

        /// <inheritdoc />
        public override Guid GetGuid(int ordinal) => (Guid)GetValue(ordinal);

        /// <inheritdoc />
        public override short GetInt16(int ordinal) => (short)GetValue(ordinal);

        /// <inheritdoc />
        public override int GetInt32(int ordinal) => (int)GetValue(ordinal);

        /// <inheritdoc />
        public override long GetInt64(int ordinal) => (long)GetValue(ordinal);

        /// <inheritdoc />
        public override string GetString(int ordinal) => (string)GetValue(ordinal);

        /// <inheritdoc />
        public override long GetBytes(int ordinal, long dataOffset, byte[]? buffer, int bufferOffset, int length)
        {
            return Copy((byte[])GetValue(ordinal), dataOffset, buffer, bufferOffset, length);
        }

        /// <inheritdoc />
        public override long GetChars(int ordinal, long dataOffset, char[]? buffer, int bufferOffset, int length)
        {
            var value = GetValue(ordinal);
            return Copy(value as char[] ?? ((string)value).ToCharArray(), dataOffset, buffer, bufferOffset, length);
        }

        private static long Copy<TItem>(TItem[] data, long dataOffset, TItem[]? buffer, int bufferOffset, int length)
        {
            if (buffer == null)
            {
                return data.Length;
            }
            var count = (int)Math.Max(0, Math.Min(length, data.Length - dataOffset));
            Array.Copy(data, dataOffset, buffer, bufferOffset, count);
            return count;
        }

        /// <inheritdoc />
        public override IEnumerator GetEnumerator() => new DbEnumerator(this, false);

        /// <summary>
        /// The schema of the columns: ColumnName, ColumnOrdinal, DataType, AllowDBNull and the other standard columns
        /// </summary>
        public override DataTable GetSchemaTable()
        {
            var schema = new DataTable("SchemaTable") { Locale = CultureInfo.InvariantCulture };
            schema.Columns.Add(SchemaTableColumn.ColumnName, typeof(string));
            schema.Columns.Add(SchemaTableColumn.ColumnOrdinal, typeof(int));
            schema.Columns.Add(SchemaTableColumn.ColumnSize, typeof(int));
            schema.Columns.Add(SchemaTableColumn.NumericPrecision, typeof(short));
            schema.Columns.Add(SchemaTableColumn.NumericScale, typeof(short));
            schema.Columns.Add(SchemaTableColumn.DataType, typeof(Type));
            schema.Columns.Add(SchemaTableColumn.ProviderType, typeof(int));
            schema.Columns.Add(SchemaTableColumn.IsLong, typeof(bool));
            schema.Columns.Add(SchemaTableColumn.AllowDBNull, typeof(bool));
            schema.Columns.Add(SchemaTableOptionalColumn.IsReadOnly, typeof(bool));
            schema.Columns.Add(SchemaTableColumn.IsUnique, typeof(bool));
            schema.Columns.Add(SchemaTableColumn.IsKey, typeof(bool));
            schema.Columns.Add(SchemaTableColumn.BaseColumnName, typeof(string));
            for (int i = 0; i < FieldCount; i++)
            {
                schema.Rows.Add(GetName(i), i, -1, DBNull.Value, DBNull.Value, GetFieldType(i), DBNull.Value, false, true, true, false, false, GetName(i));
            }
            return schema;
        }

        /// <inheritdoc />
        public override void Close()
        {
            if (!closed)
            {
                closed = true;
                hasRow = false;
                source.Dispose();
            }
        }

        /// <inheritdoc />
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                Close();
            }
            base.Dispose(disposing);
        }
    }

    /// <summary>
    /// The rows of a TinyCsvDbDataReader: the columns are known before the first Read
    /// </summary>
    internal abstract class CsvDataReaderSource : IDisposable
    {
        public string[] Names { get; protected set; } = Array.Empty<string>();

        public Type[] Types { get; protected set; } = Array.Empty<Type>();

        public abstract bool HasRows { get; }

        public abstract bool Read();

        /// <summary>
        /// The value of the column in the current row, null for a null value
        /// </summary>
        public abstract object? GetValue(int ordinal);

        public abstract void Dispose();
    }

    /// <summary>
    /// The rows of an enumeration that is read one item at a time: the first item is read in advance,
    /// to know the columns (i.e. the header) before the first Read
    /// </summary>
    internal abstract class CsvEnumeratorSource<TItem> : CsvDataReaderSource
    {
        private readonly IEnumerator<TItem> enumerator;
        private bool first = true;
        private readonly bool hasFirst;

        protected CsvEnumeratorSource(IEnumerable<TItem> items)
        {
            enumerator = items.GetEnumerator();
            hasFirst = enumerator.MoveNext();
        }

        protected TItem Current => enumerator.Current;

        protected bool HasFirst => hasFirst;

        public override bool HasRows => hasFirst;

        public override bool Read()
        {
            if (first)
            {
                first = false;
                return hasFirst;
            }
            return enumerator.MoveNext();
        }

        public override void Dispose() => enumerator.Dispose();
    }
}
