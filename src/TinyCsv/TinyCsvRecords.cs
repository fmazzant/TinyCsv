
namespace TinyCsv
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Dynamic;
    using System.Globalization;
    using System.IO;
    using System.Text;
    using TinyCsv.Data;
    using TinyCsv.Exceptions;
    using TinyCsv.Streams;

    /// <summary>
    /// The options of <see cref="TinyCsvRecords"/>: <see cref="CsvOptions.HasHeaderRecord"/> is true by default,
    /// because the header names the values of the records.
    /// </summary>
    public sealed class CsvRecordOptions : CsvOptions
    {
        /// <summary>
        /// Creates the options, with the header
        /// </summary>
        public CsvRecordOptions()
        {
            HasHeaderRecord = true;
        }

        internal CsvRecordOptions WithDelimiter(string delimiter)
        {
            var options = (CsvRecordOptions)MemberwiseClone();
            options.Delimiter = delimiter;
            return options;
        }
    }

    /// <summary>
    /// Reads the records without a model: as dictionaries, as dynamic objects or into a DataTable.
    /// The values are strings, named by the header; without the header, or for a value beyond it,
    /// the name is Field1, Field2 and so on. A value missing from a short record is null.
    /// </summary>
    public sealed class TinyCsvRecords
    {
        /// <summary>
        /// The options
        /// </summary>
        public CsvRecordOptions Options { get; }

        /// <summary>
        /// Creates an instance with the default options: with the header and the semicolon as delimiter
        /// </summary>
        public TinyCsvRecords()
            : this(options => { })
        {
        }

        /// <summary>
        /// Creates an instance with the options
        /// </summary>
        /// <param name="options"></param>
        public TinyCsvRecords(Action<CsvRecordOptions> options)
        {
            Options = new CsvRecordOptions();
            options?.Invoke(Options);
        }

        /// <summary>
        /// Reads the records of a file as dictionaries, from the column name to the value
        /// </summary>
        public IEnumerable<IDictionary<string, string>> LoadFromFile(string path)
        {
            return LoadFromFile(path, ReadDictionaries);
        }

        /// <summary>
        /// Reads the records of a stream as dictionaries, from the column name to the value
        /// </summary>
        public IEnumerable<IDictionary<string, string>> LoadFromStream(Stream stream)
        {
            return ReadDictionaries(CreateReader(stream));
        }

        /// <summary>
        /// Reads the records of a text as dictionaries, from the column name to the value
        /// </summary>
        public IEnumerable<IDictionary<string, string>> LoadFromText(string text, Encoding encoding = null)
        {
            return ReadDictionaries(CreateReader(text, encoding));
        }

        /// <summary>
        /// Reads the records of a file as dynamic objects, whose properties are the columns
        /// </summary>
        public IEnumerable<dynamic> LoadDynamicFromFile(string path)
        {
            return LoadFromFile(path, ReadDynamics);
        }

        /// <summary>
        /// Reads the records of a stream as dynamic objects, whose properties are the columns
        /// </summary>
        public IEnumerable<dynamic> LoadDynamicFromStream(Stream stream)
        {
            return ReadDynamics(CreateReader(stream));
        }

        /// <summary>
        /// Reads the records of a text as dynamic objects, whose properties are the columns
        /// </summary>
        public IEnumerable<dynamic> LoadDynamicFromText(string text, Encoding encoding = null)
        {
            return ReadDynamics(CreateReader(text, encoding));
        }

        /// <summary>
        /// Reads the records of a file into a DataTable, with a string column for each column
        /// </summary>
        public DataTable LoadDataTableFromFile(string path)
        {
            using (var reader = new StreamReader(path, ReaderEncoding, true))
            {
                return ReadDataTable(reader);
            }
        }

        /// <summary>
        /// Reads the records of a stream into a DataTable, with a string column for each column
        /// </summary>
        public DataTable LoadDataTableFromStream(Stream stream)
        {
            return ReadDataTable(CreateReader(stream));
        }

        /// <summary>
        /// Reads the records of a text into a DataTable, with a string column for each column
        /// </summary>
        public DataTable LoadDataTableFromText(string text, Encoding encoding = null)
        {
            return ReadDataTable(CreateReader(text, encoding));
        }

        private Encoding ReaderEncoding => Options.TextEncoding ?? Encoding.UTF8;

        private StreamReader CreateReader(Stream stream) => new StreamReader(stream, ReaderEncoding, true);

        private TextReader CreateReader(string text, Encoding encoding) => TextMemoryStream.CreateReader(text, encoding ?? ReaderEncoding);

        private IEnumerable<TRecord> LoadFromFile<TRecord>(string path, Func<TextReader, IEnumerable<TRecord>> read)
        {
            using (var reader = new StreamReader(path, ReaderEncoding, true))
            {
                foreach (var record in read(reader))
                {
                    yield return record;
                }
            }
        }

        private IEnumerable<IDictionary<string, string>> ReadDictionaries(TextReader textReader)
        {
            var comparer = Options.HeaderNameComparer ?? StringComparer.OrdinalIgnoreCase;
            foreach (var record in ReadRecords(textReader, new List<string>()))
            {
                var dictionary = new Dictionary<string, string>(record.Names.Count, comparer);
                for (int i = 0; i < record.Names.Count; i++)
                {
                    dictionary[record.Names[i]] = record.Value(i);
                }
                yield return dictionary;
            }
        }

        private IEnumerable<dynamic> ReadDynamics(TextReader textReader)
        {
            foreach (var record in ReadRecords(textReader, new List<string>()))
            {
                IDictionary<string, object> expando = new ExpandoObject();
                for (int i = 0; i < record.Names.Count; i++)
                {
                    expando[record.Names[i]] = record.Value(i);
                }
                yield return expando;
            }
        }

        private DataTable ReadDataTable(TextReader textReader)
        {
            var table = new DataTable { Locale = Options.Culture ?? CultureInfo.InvariantCulture };
            var names = new List<string>();
            foreach (var record in ReadRecords(textReader, names))
            {
                for (int i = table.Columns.Count; i < record.Names.Count; i++)
                {
                    table.Columns.Add(record.Names[i], typeof(string));
                }
                var row = table.NewRow();
                for (int i = 0; i < record.Names.Count; i++)
                {
                    row[i] = (object)record.Value(i) ?? DBNull.Value;
                }
                table.Rows.Add(row);
            }
            if (table.Columns.Count == 0)
            {
                // a file with the header and without records
                foreach (var name in names)
                {
                    table.Columns.Add(name, typeof(string));
                }
            }
            return table;
        }

        /// <summary>
        /// The records: the fields and their names, that are added to names. The header, when there is, is not a record.
        /// </summary>
        private IEnumerable<CsvRecord> ReadRecords(TextReader textReader, List<string> names)
        {
            var source = CsvReadSource.Open(textReader, Options);
            var dataReader = new TinyCsvDataReader<CsvRecord>(Options.WithDelimiter(source.Delimiter), source.Reader);
            var headerCount = -1;
            var index = 0;
            foreach (var fields in dataReader.ReadLinesAndFields())
            {
                if (index++ == 0 && Options.HasHeaderRecord)
                {
                    names.AddRange(CsvRecord.UniqueNames(fields));
                    headerCount = names.Count;
                    continue;
                }

                if (Options.ValidateColumnCount && headerCount >= 0 && fields.Length != headerCount)
                {
                    var exception = new InvalidColumnCountException($"Invalid column count {fields.Length}, required {headerCount} columns.");
                    exception.SetRow(index - 1, string.Join(source.Delimiter, fields));
                    if (Options.BadDataFound == null)
                    {
                        throw exception;
                    }
                    Options.BadDataFound(exception);
                    continue;
                }

                for (int i = names.Count; i < fields.Length; i++)
                {
                    names.Add(CsvRecord.FieldName(i));
                }
                yield return new CsvRecord(names, fields);
            }
        }

        /// <summary>
        /// A record and the names of its fields
        /// </summary>
        private sealed class CsvRecord
        {
            private readonly string[] fields;

            public CsvRecord(List<string> names, string[] fields)
            {
                Names = names;
                this.fields = fields;
            }

            public List<string> Names { get; }

            public string Value(int index) => index < fields.Length ? fields[index] : null;

            public static string FieldName(int index) => "Field" + (index + 1).ToString(CultureInfo.InvariantCulture);

            /// <summary>
            /// The names of the header: an empty name is FieldN, a repeated name is followed by _N (i.e. Name_2)
            /// </summary>
            public static IEnumerable<string> UniqueNames(string[] header)
            {
                var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                for (int i = 0; i < header.Length; i++)
                {
                    var name = header[i]?.Trim();
                    if (string.IsNullOrEmpty(name))
                    {
                        name = FieldName(i);
                    }
                    var unique = name;
                    for (int n = 2; !names.Add(unique); n++)
                    {
                        unique = name + "_" + n.ToString(CultureInfo.InvariantCulture);
                    }
                    yield return unique;
                }
            }
        }
    }
}
