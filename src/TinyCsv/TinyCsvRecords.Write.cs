namespace Mafesoft.TinyCsv
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Data;
    using System.Globalization;
    using System.IO;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Mafesoft.TinyCsv.Data;

    /// <summary>
    /// Writes the records without a model: a DataTable, an IDataReader (streamed), dictionaries or dynamic objects.
    /// The header has the names of the columns (see <see cref="CsvRecordOptions.ColumnsToWrite"/> to choose them and their order),
    /// the values are formatted with the <see cref="CsvOptions.Culture"/> and escaped as the typed writer does.
    /// </summary>
    public sealed partial class TinyCsvRecords
    {
        /// <summary>
        /// Writes the rows of a DataTable to a file
        /// </summary>
        public void SaveToFile(string path, DataTable table) => SaveToFile(path, Rows(table));

        /// <summary>
        /// Writes the rows of a data reader to a file, one at a time
        /// </summary>
        public void SaveToFile(string path, IDataReader reader) => SaveToFile(path, Rows(reader));

        /// <summary>
        /// Writes the records to a file: the columns are the keys of the first record
        /// </summary>
        public void SaveToFile(string path, IEnumerable<IDictionary<string, object?>> records) => SaveToFile(path, Rows(records));

        /// <summary>
        /// Writes the records to a file: dynamic objects (ExpandoObject), dictionaries (i.e. the records read by
        /// <see cref="LoadFromFile(string)"/>) or objects with public properties (i.e. anonymous types).
        /// The columns are the names of the first record.
        /// </summary>
        public void SaveToFile(string path, IEnumerable<dynamic> records) => SaveToFile(path, Rows(records));

        /// <summary>
        /// Writes the rows of a DataTable to a stream, that is left open
        /// </summary>
        public void SaveToStream(Stream stream, DataTable table) => SaveToStream(stream, Rows(table));

        /// <summary>
        /// Writes the rows of a data reader to a stream, one at a time. The stream is left open.
        /// </summary>
        public void SaveToStream(Stream stream, IDataReader reader) => SaveToStream(stream, Rows(reader));

        /// <summary>
        /// Writes the records to a stream, that is left open: the columns are the keys of the first record
        /// </summary>
        public void SaveToStream(Stream stream, IEnumerable<IDictionary<string, object?>> records) => SaveToStream(stream, Rows(records));

        /// <summary>
        /// Writes the records to a stream, that is left open: dynamic objects (ExpandoObject), dictionaries
        /// or objects with public properties. The columns are the names of the first record.
        /// </summary>
        public void SaveToStream(Stream stream, IEnumerable<dynamic> records) => SaveToStream(stream, Rows(records));

        /// <summary>
        /// Writes the rows of a DataTable to a file asynchronously
        /// </summary>
        public Task SaveToFileAsync(string path, DataTable table, CancellationToken cancellationToken = default) => SaveToFileAsync(path, Rows(table), cancellationToken);

        /// <summary>
        /// Writes the rows of a data reader to a file asynchronously, one at a time
        /// (with DbDataReader.ReadAsync when the reader is a DbDataReader)
        /// </summary>
        public Task SaveToFileAsync(string path, IDataReader reader, CancellationToken cancellationToken = default) => SaveToFileAsync(path, Rows(reader), cancellationToken);

        /// <summary>
        /// Writes the records to a file asynchronously: the columns are the keys of the first record
        /// </summary>
        public Task SaveToFileAsync(string path, IEnumerable<IDictionary<string, object?>> records, CancellationToken cancellationToken = default) => SaveToFileAsync(path, Rows(records), cancellationToken);

        /// <summary>
        /// Writes the records to a file asynchronously: dynamic objects (ExpandoObject), dictionaries
        /// or objects with public properties. The columns are the names of the first record.
        /// </summary>
        public Task SaveToFileAsync(string path, IEnumerable<dynamic> records, CancellationToken cancellationToken = default) => SaveToFileAsync(path, Rows(records), cancellationToken);

        /// <summary>
        /// Writes the rows of a DataTable to a stream asynchronously. The stream is left open.
        /// </summary>
        public Task SaveToStreamAsync(Stream stream, DataTable table, CancellationToken cancellationToken = default) => SaveToStreamAsync(stream, Rows(table), cancellationToken);

        /// <summary>
        /// Writes the rows of a data reader to a stream asynchronously, one at a time
        /// (with DbDataReader.ReadAsync when the reader is a DbDataReader). The stream is left open.
        /// </summary>
        public Task SaveToStreamAsync(Stream stream, IDataReader reader, CancellationToken cancellationToken = default) => SaveToStreamAsync(stream, Rows(reader), cancellationToken);

        /// <summary>
        /// Writes the records to a stream asynchronously, that is left open: the columns are the keys of the first record
        /// </summary>
        public Task SaveToStreamAsync(Stream stream, IEnumerable<IDictionary<string, object?>> records, CancellationToken cancellationToken = default) => SaveToStreamAsync(stream, Rows(records), cancellationToken);

        /// <summary>
        /// Writes the records to a stream asynchronously, that is left open: dynamic objects (ExpandoObject), dictionaries
        /// or objects with public properties. The columns are the names of the first record.
        /// </summary>
        public Task SaveToStreamAsync(Stream stream, IEnumerable<dynamic> records, CancellationToken cancellationToken = default) => SaveToStreamAsync(stream, Rows(records), cancellationToken);

        /// <summary>
        /// The CSV text of the rows of a DataTable, without the new line after the last row (as TinyCsv GetAllText)
        /// </summary>
        public string GetAllText(DataTable table) => GetAllText(Rows(table));

        /// <summary>
        /// The CSV text of the rows of a data reader, without the new line after the last row
        /// </summary>
        public string GetAllText(IDataReader reader) => GetAllText(Rows(reader));

        /// <summary>
        /// The CSV text of the records, without the new line after the last row: the columns are the keys of the first record
        /// </summary>
        public string GetAllText(IEnumerable<IDictionary<string, object?>> records) => GetAllText(Rows(records));

        /// <summary>
        /// The CSV text of the records, without the new line after the last row: dynamic objects (ExpandoObject),
        /// dictionaries or objects with public properties. The columns are the names of the first record.
        /// </summary>
        public string GetAllText(IEnumerable<dynamic> records) => GetAllText(Rows(records));

        private IList<string>? ColumnsToWrite => Options.ColumnsToWrite;

        private CsvRecordRows Rows(DataTable table) => new CsvDataTableRows(table, ColumnsToWrite);

        private CsvRecordRows Rows(IDataReader reader) => new CsvDataReaderRows(reader, ColumnsToWrite);

        private CsvRecordRows Rows(IEnumerable records) => new CsvKeyedRows(records, ColumnsToWrite);

        /// <summary>
        /// The encoding used to write. The default UTF-8 is written without the byte order mark, as StreamWriter does; WriteByteOrderMark, when set, decides it.
        /// </summary>
        private Encoding WriterEncoding => CsvOptions.WithByteOrderMark(
            Options.TextEncoding == null || ReferenceEquals(Options.TextEncoding, Encoding.UTF8) ? new UTF8Encoding(false) : Options.TextEncoding,
            Options.WriteByteOrderMark);

        private string NewLine => Options.NewLine ?? CsvOptions.DefaultNewLine;

        private void SaveToFile(string path, CsvRecordRows rows)
        {
            using (var writer = new StreamWriter(path, false, WriterEncoding))
            {
                Write(writer, rows);
            }
        }

        private void SaveToStream(Stream stream, CsvRecordRows rows)
        {
            using (var writer = new StreamWriter(stream, WriterEncoding, 1024, true))
            {
                Write(writer, rows);
            }
        }

        private async Task SaveToFileAsync(string path, CsvRecordRows rows, CancellationToken cancellationToken)
        {
            using (var writer = new StreamWriter(path, false, WriterEncoding))
            {
                await WriteAsync(writer, rows, cancellationToken).ConfigureAwait(false);
            }
        }

        private async Task SaveToStreamAsync(Stream stream, CsvRecordRows rows, CancellationToken cancellationToken)
        {
            using (var writer = new StreamWriter(stream, WriterEncoding, 1024, true))
            {
                await WriteAsync(writer, rows, cancellationToken).ConfigureAwait(false);
            }
        }

        private string GetAllText(CsvRecordRows rows)
        {
            using (var writer = new StringWriter(CultureInfo.InvariantCulture))
            {
                Write(writer, rows);
                var text = writer.ToString();
                var newLine = NewLine;
                if (newLine.Length > 0 && text.EndsWith(newLine, StringComparison.Ordinal))
                {
                    text = text.Substring(0, text.Length - newLine.Length);
                }
                // the byte order mark char (U+FEFF) when WriteByteOrderMark is true, as TinyCsv GetAllText
                return Options.WriteByteOrderMark == true ? "﻿" + text : text;
            }
        }

        private void Write(TextWriter writer, CsvRecordRows rows)
        {
            rows.Start();
            var line = new CsvFieldBuilder(Options);
            var newLine = NewLine;
            if (BuildStart(line, rows) is string start)
            {
                writer.Write(start);
            }
            var quoted = Quoted(line, rows);
            var index = Options.HasHeaderRecord ? 1 : 0;
            while (rows.Read())
            {
                BuildRow(line, rows, quoted, index++);
                line.WriteTo(writer);
                writer.Write(newLine);
            }
            writer.Flush();
        }

        private async Task WriteAsync(TextWriter writer, CsvRecordRows rows, CancellationToken cancellationToken)
        {
            await rows.StartAsync(cancellationToken).ConfigureAwait(false);
            var line = new CsvFieldBuilder(Options);
            var newLine = NewLine;
            if (BuildStart(line, rows) is string start)
            {
                await writer.WriteAsync(start).ConfigureAwait(false);
            }
            var quoted = Quoted(line, rows);
            var index = Options.HasHeaderRecord ? 1 : 0;
            while (await rows.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                BuildRow(line, rows, quoted, index++);
                await line.WriteToAsync(writer).ConfigureAwait(false);
                await writer.WriteAsync(newLine).ConfigureAwait(false);
            }
            await writer.FlushAsync().ConfigureAwait(false);
        }

        /// <summary>
        /// The lines before the first record, each followed by the new line: the separator line (see WriteSeparatorLine) and the header
        /// </summary>
        private string? BuildStart(CsvFieldBuilder line, CsvRecordRows rows)
        {
            var newLine = NewLine;
            var separatorLine = Options.WriteSeparatorLine ? Options.SeparatorLine + newLine : null;
            if (!Options.HasHeaderRecord || rows.Names.Length == 0)
            {
                return separatorLine;
            }
            line.Clear();
            var all = Options.QuoteMode == CsvQuoteMode.All || Options.QuoteMode == CsvQuoteMode.NonNumeric;
            for (int i = 0; i < rows.Names.Length; i++)
            {
                if (i > 0)
                {
                    line.AppendDelimiter();
                }
                if (all && Options.AllowRowEnclosedInDoubleQuotesValues)
                {
                    line.AppendHeaderName(rows.Names[i]);
                }
                else
                {
                    // the names come from the data (i.e. the keys of a dictionary): escaped when necessary
                    line.SetColumn(rows.Names[i], false);
                    line.AppendField(rows.Names[i]);
                }
            }
            return separatorLine + line.ToString() + newLine;
        }

        /// <summary>
        /// For each column whose type is known, true when its fields are always enclosed in quotes; null when the type of the value decides it
        /// </summary>
        private static bool?[] Quoted(CsvFieldBuilder line, CsvRecordRows rows)
        {
            var quoted = new bool?[rows.Names.Length];
            for (int i = 0; i < quoted.Length; i++)
            {
                var type = rows.Types[i];
                quoted[i] = type == null || type == typeof(object) ? (bool?)null : line.IsQuoted(type);
            }
            return quoted;
        }

        private void BuildRow(CsvFieldBuilder line, CsvRecordRows rows, bool?[] quoted, int rowIndex)
        {
            var culture = Options.Culture ?? CultureInfo.InvariantCulture;
            line.Clear();
            try
            {
                for (int i = 0; i < quoted.Length; i++)
                {
                    if (i > 0)
                    {
                        line.AppendDelimiter();
                    }
                    var value = rows.GetValue(i);
                    if (value == null || value is DBNull)
                    {
                        line.SetColumn(rows.Names[i], false);
                        line.TryAppendNullValueOnWrite();
                        continue;
                    }
                    line.SetColumn(rows.Names[i], quoted[i] ?? line.IsQuoted(value.GetType()));
                    switch (value)
                    {
                        case string text:
                            line.AppendText(text);
                            break;
                        case byte[] bytes:
                            line.AppendField(Convert.ToBase64String(bytes));
                            break;
                        default:
                            line.AppendField(string.Format(culture, "{0}", value));
                            break;
                    }
                }
            }
            catch (Exceptions.CsvException exception)
            {
                exception.SetRowIndex(rowIndex);
                throw;
            }
        }
    }
}
