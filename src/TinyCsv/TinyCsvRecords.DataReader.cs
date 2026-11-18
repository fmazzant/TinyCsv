namespace Mafesoft.TinyCsv
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using Mafesoft.TinyCsv.Conversions;
    using Mafesoft.TinyCsv.Data;
    using Mafesoft.TinyCsv.Exceptions;
    using Mafesoft.TinyCsv.Factory;

    /// <summary>
    /// The data reader over the records, and the types of the columns (see <see cref="CsvRecordOptions.ColumnTypes"/>)
    /// </summary>
    public sealed partial class TinyCsvRecords
    {
        /// <summary>
        /// Creates a data reader over the records of a file, read one at a time (i.e. for <c>SqlBulkCopy.WriteToServer</c>):
        /// the columns are the names of the header (or Field1, Field2, ... of the first record), the values are strings
        /// or the types of <see cref="CsvRecordOptions.ColumnTypes"/>. Dispose it at the end.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public TinyCsvDbDataReader CreateDataReader(string path)
        {
            return CreateDataReader(new StreamReader(path, ReaderEncoding, true));
        }

        /// <summary>
        /// Creates a data reader over the records of a stream, read one at a time. Disposing the reader disposes the stream.
        /// </summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        public TinyCsvDbDataReader CreateDataReader(Stream stream)
        {
            return CreateDataReader(CreateReader(stream));
        }

        /// <summary>
        /// Creates a data reader over the records of a text reader, read one at a time. Disposing the data reader disposes the text reader.
        /// </summary>
        /// <param name="textReader"></param>
        /// <returns></returns>
        public TinyCsvDbDataReader CreateDataReader(TextReader textReader)
        {
            if (textReader == null)
            {
                throw new ArgumentNullException(nameof(textReader));
            }
            return new TinyCsvDbDataReader(new RecordSource(this, textReader), Options.HeaderNameComparer);
        }

        /// <summary>
        /// The types of the columns by name (see ColumnTypes): null when all the columns are strings
        /// </summary>
        private Dictionary<string, Type>? CreateColumnTypes()
        {
            var types = Options.ColumnTypes;
            if (types == null || types.Count == 0)
            {
                return null;
            }
            var result = new Dictionary<string, Type>(Options.HeaderNameComparer ?? StringComparer.OrdinalIgnoreCase);
            foreach (var pair in types)
            {
                result[pair.Key] = pair.Value ?? typeof(string);
            }
            return result;
        }

        /// <summary>
        /// The type of a column: the type of ColumnTypes without Nullable (a null value is DBNull), or string
        /// </summary>
        private static Type ColumnType(Dictionary<string, Type>? types, string name)
        {
            if (types != null && types.TryGetValue(name, out var type))
            {
                return Nullable.GetUnderlyingType(type) ?? type;
            }
            return typeof(string);
        }

        /// <summary>
        /// Converts the field of a column of the type: null for a null field, and for an empty field of a type that is not string
        /// </summary>
        /// <exception cref="InvalidColumnValueException">the field cannot be converted</exception>
        private object? ConvertValue(string name, Type type, IValueConverter? converter, string? value, int rowIndex)
        {
            if (value == null || type == typeof(string) || converter == null)
            {
                return value;
            }
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }
            var culture = Options.Culture ?? CultureInfo.InvariantCulture;
            Exception? inner = null;
            try
            {
                // the built-in converters return false for a value that cannot be parsed (instead of the default value)
                if (converter is DefaultValueConverter defaultConverter)
                {
                    if (defaultConverter.TryConvertBack(value, type, null, culture, out var result))
                    {
                        return result;
                    }
                }
                else
                {
                    return converter.ConvertBack(value, type, null, culture);
                }
            }
            catch (Exception exception) when (exception is FormatException || exception is InvalidCastException || exception is OverflowException || exception is ArgumentException)
            {
                inner = exception;
            }
            var invalid = new InvalidColumnValueException(name, value, $"The value '{value}' of the column '{name}' is not a valid {type.Name}.", inner);
            invalid.SetRowIndex(rowIndex);
            throw invalid;
        }

        /// <summary>
        /// The records of a text reader, for the data reader: the first record is read in advance,
        /// so the names of the columns are known before the first Read
        /// </summary>
        private sealed class RecordSource : CsvEnumeratorSource<CsvRecord>
        {
            private readonly TinyCsvRecords records;
            private readonly TextReader textReader;
            private readonly IValueConverter?[] converters;
            private int rowIndex;

            public RecordSource(TinyCsvRecords records, TextReader textReader)
                : this(records, textReader, new List<string>())
            {
            }

            private RecordSource(TinyCsvRecords records, TextReader textReader, List<string> names)
                : base(records.ReadRecords(textReader, names))
            {
                this.records = records;
                this.textReader = textReader;
                Names = names.ToArray();
                var types = records.CreateColumnTypes();
                Types = new Type[Names.Length];
                converters = new IValueConverter?[Names.Length];
                for (int i = 0; i < Names.Length; i++)
                {
                    Types[i] = ColumnType(types, Names[i]);
                    converters[i] = Types[i] == typeof(string) ? null : ValueConverterFactory.CreateValueConverterByType(Types[i]);
                }
                rowIndex = records.Options.HasHeaderRecord ? 0 : -1;
            }

            public override bool Read()
            {
                if (!base.Read())
                {
                    return false;
                }
                rowIndex++;
                return true;
            }

            public override object? GetValue(int ordinal)
            {
                return records.ConvertValue(Names[ordinal], Types[ordinal], converters[ordinal], Current.Value(ordinal), rowIndex);
            }

            public override void Dispose()
            {
                base.Dispose();
                textReader.Dispose();
            }
        }
    }
}
