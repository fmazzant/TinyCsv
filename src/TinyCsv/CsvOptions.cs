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

namespace Mafesoft.TinyCsv
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using Mafesoft.TinyCsv.Args;
    using Mafesoft.TinyCsv.Attributes;
    using Mafesoft.TinyCsv.Exceptions;
    using Mafesoft.TinyCsv.Helpers;

    /// <summary>
    ///  Csv Options abstract definition
    /// </summary>
    public interface ICsvOptions
    {
        /// <summary>
        /// Has header record
        /// </summary>
        bool HasHeaderRecord { get; set; }

        /// <summary>
        /// Delimiter
        /// </summary>
        string Delimiter { get; set; }

        /// <summary>
        /// Respects new line (either \r\n or \n) characters inside field values enclosed in double quotes.
        /// </summary>
        bool AllowRowEnclosedInDoubleQuotesValues { get; set; }

        /// <summary>
        /// Allows the sequence "\"" to be a valid quoted value (in addition to the standard """")
        /// </summary>
        bool AllowBackSlashToEscapeQuote { get; set; }

        /// <summary>
        /// Double quotes
        /// </summary>
        char DoubleQuotes { get; set; }

        /// <summary>
        /// Comment
        /// </summary>
        char Comment { get; set; }

        /// <summary>
        /// Gets or Set the newline string defined for this environment
        /// </summary>
        string NewLine { get; set; }

        /// <summary>
        /// Represents a character encoding. Null is UTF-8.
        /// </summary>
        Encoding? TextEncoding { get; set; }

        /// <summary>
        /// Allows skipping of initial rows without csv data
        /// </summary>
        uint RowsToSkip { get; set; }

        /// <summary>
        /// Allows skipt row by condition. Null does not skip any row.
        /// </summary>
        Func<string, int, bool>? SkipRow { get; set; }

        /// <summary>
        /// Can be used to trim each cell
        /// </summary>
        bool TrimData { get; set; }

        /// <summary>
        /// Checks each row immediately for column count
        /// </summary>
        bool ValidateColumnCount { get; set; }

        /// <summary>
        /// Allows comment inside the content
        /// </summary>
        bool AllowComment { get; set; }

        /// <summary>
        /// Allow the last char is the delimiter char
        /// </summary>
        bool EndOfLineDelimiterChar { get; set; }
    }

    /// <summary>
    ///  Csv Options abstract definition
    /// </summary>
    public abstract class CsvOptions : ICsvOptions
    {
        /// <summary>
        /// Has header record
        /// </summary>
        public bool HasHeaderRecord { get; set; }

        /// <summary>
        /// The default <see cref="Delimiter"/>: the semicolon, used by Excel with the Italian and most European regional settings
        /// </summary>
        public const string DefaultDelimiter = ";";

        /// <summary>
        /// The default <see cref="NewLine"/>: CRLF, as RFC 4180. It is the same on every operating system.
        /// </summary>
        public const string DefaultNewLine = "\r\n";

        /// <summary>
        /// Delimiter. The default is the semicolon (<see cref="DefaultDelimiter"/>): set "," for RFC 4180 files,
        /// or <see cref="DetectDelimiter"/> to detect it when reading.
        /// </summary>
        public string Delimiter { get; set; } = DefaultDelimiter;

        /// <summary>
        /// Detects the delimiter from the first records when reading, among <see cref="DelimiterCandidates"/>.
        /// <see cref="Delimiter"/> is used when no candidate is found and when writing.
        /// </summary>
        public bool DetectDelimiter { get; set; } = false;

        /// <summary>
        /// The delimiters that <see cref="DetectDelimiter"/> looks for: comma, semicolon, tab and pipe.
        /// When null, the delimiter is not detected and <see cref="Delimiter"/> is used.
        /// </summary>
        public string[]? DelimiterCandidates { get; set; } = { ",", ";", "	", "|" };

        /// <summary>
        /// Respects new line (either \r\n or \n) characters inside field values enclosed in double quotes.
        /// </summary>
        public bool AllowRowEnclosedInDoubleQuotesValues { get; set; } = true;

        /// <summary>
        /// Allows the sequence "\"" to be a valid quoted value (in addition to the standard """")
        /// </summary>
        public bool AllowBackSlashToEscapeQuote { get; set; } = false;

        /// <summary>
        /// Double quotes
        /// </summary>
        public char DoubleQuotes { get; set; } = '"';

        /// <summary>
        /// Comment
        /// </summary>
        public char Comment { get; set; } = '#';

        /// <summary>
        /// The line terminator written after each row. The default is CRLF (<see cref="DefaultNewLine"/>), as RFC 4180,
        /// on every operating system. When reading, CRLF, LF and CR are all accepted, whatever the value.
        /// </summary>
        public string NewLine { get; set; } = DefaultNewLine;

        /// <summary>
        /// The encoding to read and write. The default is UTF-8, written without the byte order mark (BOM);
        /// set <see cref="WriteByteOrderMark"/> (or <c>new UTF8Encoding(true)</c>) to write the BOM (i.e. for Excel). When reading, a BOM takes precedence.
        /// Null is the same as the default.
        /// </summary>
        public Encoding? TextEncoding { get; set; } = Encoding.UTF8;

        /// <summary>
        /// Allows skipping of initial rows without csv data
        /// </summary>
        public uint RowsToSkip { get; set; } = 0;

        /// <summary>
        /// Allows skipt row by condition. Null does not skip any row.
        /// </summary>
        public Func<string, int, bool>? SkipRow { get; set; } = NotSkipRow;

        /// <summary>
        /// The default SkipRow, that does not skip any row: the readers do not create the string of the record to call it
        /// </summary>
        internal static readonly Func<string, int, bool> NotSkipRow = (row, index) => false;

        /// <summary>
        /// Can be used to trim each cell
        /// </summary>
        public bool TrimData { get; set; } = false;

        /// <summary>
        /// Checks each row immediately for column count
        /// </summary>
        public bool ValidateColumnCount { get; set; } = false;

        /// <summary>
        /// Allows comment inside the content
        /// </summary>
        public bool AllowComment { get; set; } = true;

        /// <summary>
        /// Allow the last char is the delimiter char
        /// </summary>
        public bool EndOfLineDelimiterChar { get; set; } = true;

        /// <summary>
        /// Maps the columns to the fields by the names in the header record instead of by position,
        /// so the order of the columns in the file does not matter and the columns that are not mapped are ignored.
        /// It requires HasHeaderRecord, and a column that is not in the header throws a MissingColumnException.
        /// </summary>
        public bool MapColumnsByHeader { get; set; } = false;

        /// <summary>
        /// Compares the names in the header record with the column names when MapColumnsByHeader is true.
        /// The default ignores the case. Null is the same as the default.
        /// </summary>
        public StringComparer? HeaderNameComparer { get; set; } = StringComparer.OrdinalIgnoreCase;

        /// <summary>
        /// The culture used to read and write the values: numbers, dates and the columns with a format.
        /// The default is the invariant culture, so the files do not depend on the culture of the machine.
        /// A column with a ColumnFormatProvider uses its provider. Null is the invariant culture.
        /// </summary>
        public CultureInfo? Culture { get; set; } = CultureInfo.InvariantCulture;

        /// <summary>
        /// A value that is not empty and that cannot be converted to the type of its column (i.e. "abc" for an int)
        /// is bad data, an InvalidColumnValueException. When false (default), it is converted to the default value.
        /// </summary>
        public bool ValidateValues { get; set; } = false;

        /// <summary>
        /// Called for each row with bad data (a value that cannot be converted, a wrong column count, ...):
        /// the row is skipped and the read goes on. The exception has the row, the column and the value of the error.
        /// When null (default), the exception is thrown.
        /// </summary>
        public Action<CsvException>? BadDataFound { get; set; }

        /// <summary>
        /// Enable handlers if necessary
        /// </summary>
        public bool EnableHandlers { get; set; } = false;

        /// <summary>
        /// The default <see cref="InjectionCharacters"/>: the characters that start a formula in Excel and LibreOffice
        /// (<c>=</c>, <c>+</c>, <c>-</c>, <c>@</c>, tab and carriage return), as OWASP recommends
        /// </summary>
        public static char[] DefaultInjectionCharacters => new[] { '=', '+', '-', '@', '\t', '\r' };

        /// <summary>
        /// Protects the text values from CSV injection (formula injection) when writing: a value that starts with one of
        /// <see cref="InjectionCharacters"/> is escaped, stripped or rejected. Only the columns of type string are checked,
        /// so the numbers (i.e. <c>-5</c>) are written as they are. The default is <see cref="CsvInjectionProtection.None"/>.
        /// </summary>
        public CsvInjectionProtection InjectionProtection { get; set; } = CsvInjectionProtection.None;

        /// <summary>
        /// The characters that start a formula, see <see cref="InjectionProtection"/>.
        /// The default is <see cref="DefaultInjectionCharacters"/>; null is the same as the default.
        /// </summary>
        public char[]? InjectionCharacters { get; set; } = DefaultInjectionCharacters;

        /// <summary>
        /// When the writer encloses the fields in quotes. The default is <see cref="CsvQuoteMode.Minimal"/>:
        /// only the fields that contain the delimiter, a quote or a new line. The fields are enclosed in quotes
        /// only when <see cref="AllowRowEnclosedInDoubleQuotesValues"/> is true.
        /// </summary>
        public CsvQuoteMode QuoteMode { get; set; } = CsvQuoteMode.Minimal;

        /// <summary>
        /// The values that are read as null (i.e. "NULL" or "\N"), compared with the whole field, case sensitive,
        /// before the converter is called: the property is null, or the default value of a type that cannot be null.
        /// A column can have its own values (see CsvMemberMap.NullValues and NullValuesAttribute), that replace these.
        /// Null (default) reads every value with the converter.
        /// </summary>
        public string[]? NullValues { get; set; }

        /// <summary>
        /// The text written for a null value (i.e. "NULL" or "\N"). The default is an empty field.
        /// When it is empty, a null value is written by the converter of the column, as before.
        /// </summary>
        public string? NullValueOnWrite { get; set; } = string.Empty;

        /// <summary>
        /// Writes the byte order mark (BOM) of the encoding, independently from the <see cref="TextEncoding"/> instance:
        /// true writes it (Excel needs it to show the accented characters of a UTF-8 file), false does not write it
        /// (many Linux tools and importers fail with it). Null (default) follows the encoding: the default UTF-8 is written
        /// without the BOM, <c>new UTF8Encoding(true)</c> with the BOM.
        /// <c>GetAllText</c> starts with the BOM char (U+FEFF) when it is true.
        /// </summary>
        public bool? WriteByteOrderMark { get; set; }

        /// <summary>
        /// Writes the line <c>sep=</c> followed by the <see cref="Delimiter"/> (i.e. <c>sep=;</c>) before the header:
        /// Excel reads it to choose the delimiter, regardless of the regional settings. The default is false.
        /// When reading, a first line <c>sep=X</c> is always recognized: it sets the delimiter and it is skipped.
        /// </summary>
        public bool WriteSeparatorLine { get; set; } = false;

        /// <summary>
        /// The prefix of the separator line, see <see cref="WriteSeparatorLine"/>
        /// </summary>
        internal const string SeparatorLinePrefix = "sep=";

        /// <summary>
        /// The separator line written with <see cref="WriteSeparatorLine"/>, i.e. "sep=;"
        /// </summary>
        internal string SeparatorLine => SeparatorLinePrefix + Delimiter;

        /// <summary>
        /// The encoding with or without the byte order mark (see WriteByteOrderMark). The encodings without a byte order mark
        /// (i.e. ASCII) are returned as they are.
        /// </summary>
        /// <param name="encoding"></param>
        /// <param name="writeByteOrderMark"></param>
        /// <returns></returns>
        internal static Encoding WithByteOrderMark(Encoding encoding, bool? writeByteOrderMark)
        {
            if (writeByteOrderMark == null)
            {
                return encoding;
            }
            var bom = writeByteOrderMark.Value;
            if ((encoding.GetPreamble().Length > 0) == bom)
            {
                return encoding;
            }
            var throwOnInvalid = encoding.DecoderFallback is DecoderExceptionFallback;
            switch (encoding.CodePage)
            {
                case 65001:
                    return new UTF8Encoding(bom, throwOnInvalid);
                case 1200:
                    return new UnicodeEncoding(false, bom, throwOnInvalid);
                case 1201:
                    return new UnicodeEncoding(true, bom, throwOnInvalid);
                case 12000:
                    return new UTF32Encoding(false, bom, throwOnInvalid);
                case 12001:
                    return new UTF32Encoding(true, bom, throwOnInvalid);
                default:
                    return encoding;
            }
        }
    }

    /// <summary>
    /// Csv Options definition
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public sealed class CsvOptions<T> : CsvOptions
    {
        /// <summary>
        /// Columns
        /// </summary>
        public CsvOptionsColumns<T> Columns { get; internal set; }

        /// <summary>
        /// Event Handlers
        /// </summary>
        public EventHandlers Handlers { get; }

        /// <summary>
        /// Object's properties
        /// </summary>
        internal ConcurrentDictionary<string, PropertyInfo> Properties { get; set; }

        /// <summary>
        /// Object's faster properties
        /// </summary>
        internal ConcurrentDictionary<string, PropertyHelper<T>> FasterProperties { get; set; }

        /// <summary>
        /// Create a CsvOptions
        /// </summary>
        public CsvOptions()
        {
            Columns = new CsvOptionsColumns<T>();
            Handlers = new EventHandlers(this);
            FasterProperties = PropertyHelper<T>.GetPropertiesExpressionTree(typeof(T));
            Properties = GetProperties(typeof(T));
        }

        /// <summary>
        /// Create options from Type
        /// </summary>
        /// <param name="type"></param>
        public CsvOptions(Type type)
        {
            Handlers = new EventHandlers(this);
            Columns = new CsvOptionsColumns<T>(type);
            this.SetOptionValueFromType(type);
            FasterProperties = PropertyHelper<T>.GetPropertiesExpressionTree(type);
            Properties = GetProperties(type);
        }

        /// <summary>
        /// The public instance properties of the type, by name
        /// </summary>
        /// <param name="type"></param>
        private static ConcurrentDictionary<string, PropertyInfo> GetProperties(Type type)
        {
            var result = new ConcurrentDictionary<string, PropertyInfo>();
            var properties = type.GetProperties(BindingFlags.Instance | BindingFlags.Public);
            foreach (var property in properties)
            {
                result.TryAdd(property.Name, property);
            }
            return result;
        }

        /// <summary>
        /// Set Option Value From Type
        /// </summary>
        /// <param name="attribute"></param>
        private void SetOptionValueFromType(Type type)
        {
            Attribute[] attrs = Attribute.GetCustomAttributes(type);
            foreach (var attribute in attrs)
            {
                if (attribute is DelimiterAttribute delimeter)
                {
                    this.Delimiter = delimeter.Delimiter;
                }
                if (attribute is AllowBackSlashToEscapeQuoteAttribute allowBackSlashToEscapeQuote)
                {
                    this.AllowBackSlashToEscapeQuote = allowBackSlashToEscapeQuote.AllowBackSlashToEscapeQuote;
                }
                if (attribute is AllowCommentAttribute allowComment)
                {
                    this.AllowComment = allowComment.AllowComment;
                }
                if (attribute is AllowRowEnclosedInDoubleQuotesValuesAttribute allowRowEnclosedInDoubleQuotesValues)
                {
                    this.AllowRowEnclosedInDoubleQuotesValues = allowRowEnclosedInDoubleQuotesValues.AllowRowEnclosedInDoubleQuotesValues;
                }
                if (attribute is CommentAttribute comment)
                {
                    this.Comment = comment.Comment;
                }
                if (attribute is DoubleQuotesAttribute doubleQuotes)
                {
                    this.DoubleQuotes = doubleQuotes.DoubleQuotes;
                }
                if (attribute is EndOfLineDelimiterCharAttribute endOfLineDelimiterChar)
                {
                    this.EndOfLineDelimiterChar = endOfLineDelimiterChar.EndOfLineDelimiterChar;
                }
                if (attribute is HasHeaderRecordAttribute hasHeaderRecord)
                {
                    this.HasHeaderRecord = hasHeaderRecord.HasHeaderRecord;
                }
                if (attribute is NewLineAttribute newLine)
                {
                    this.NewLine = newLine.NewLine;
                }
                if (attribute is RowsToSkipAttribute rowsToSkip)
                {
                    this.RowsToSkip = rowsToSkip.RowsToSkip;
                }
                if (attribute is SkipRowAttribute skipRow)
                {
                    this.SkipRow = skipRow.SkipRow;
                }
                if (attribute is TextEncodingAttribute textEncoding)
                {
                    this.TextEncoding = textEncoding.TextEncoding;
                }
                if (attribute is TrimDataAttribute trimData)
                {
                    this.TrimData = trimData.TrimData;
                }
                if (attribute is ValidateColumnCountAttribute validateColumnCount)
                {
                    this.ValidateColumnCount = validateColumnCount.ValidateColumnCount;
                }
                if (attribute is MapColumnsByHeaderAttribute mapColumnsByHeader)
                {
                    this.MapColumnsByHeader = mapColumnsByHeader.MapColumnsByHeader;
                }
                if (attribute is DetectDelimiterAttribute detectDelimiter)
                {
                    this.DetectDelimiter = detectDelimiter.DetectDelimiter;
                }
                if (attribute is CultureAttribute culture)
                {
                    this.Culture = culture.Culture;
                }
                if (attribute is ValidateValuesAttribute validateValues)
                {
                    this.ValidateValues = validateValues.ValidateValues;
                }
                if (attribute is InjectionProtectionAttribute injectionProtection)
                {
                    this.InjectionProtection = injectionProtection.InjectionProtection;
                    this.InjectionCharacters = injectionProtection.InjectionCharacters ?? this.InjectionCharacters;
                }
                if (attribute is QuoteModeAttribute quoteMode)
                {
                    this.QuoteMode = quoteMode.QuoteMode;
                }
                if (attribute is NullValuesAttribute nullValues)
                {
                    this.NullValues = nullValues.NullValues;
                }
                if (attribute is NullValueOnWriteAttribute nullValueOnWrite)
                {
                    this.NullValueOnWrite = nullValueOnWrite.NullValueOnWrite;
                }
                if (attribute is WriteByteOrderMarkAttribute writeByteOrderMark)
                {
                    this.WriteByteOrderMark = writeByteOrderMark.WriteByteOrderMark;
                }
                if (attribute is WriteSeparatorLineAttribute writeSeparatorLine)
                {
                    this.WriteSeparatorLine = writeSeparatorLine.WriteSeparatorLine;
                }
            }
        }

        /// <summary>
        /// Uses the columns and the options of a class map, defined outside the model
        /// </summary>
        /// <param name="classMap"></param>
        /// <returns></returns>
        public CsvOptions<T> UseClassMap(CsvClassMap<T> classMap)
        {
            if (classMap == null)
            {
                throw new ArgumentNullException(nameof(classMap));
            }
            classMap.Apply(this);
            return this;
        }

        /// <summary>
        /// Uses the columns and the options of a class map, defined outside the model
        /// </summary>
        /// <typeparam name="TClassMap"></typeparam>
        /// <returns></returns>
        public CsvOptions<T> UseClassMap<TClassMap>() where TClassMap : CsvClassMap<T>, new()
        {
            return UseClassMap(new TClassMap());
        }

        /// <summary>
        /// A copy of the options with another delimiter (i.e. the detected one): the columns and the handlers are shared
        /// </summary>
        internal CsvOptions<T> WithDelimiter(string delimiter)
        {
            if (delimiter == Delimiter)
            {
                return this;
            }
            var options = (CsvOptions<T>)MemberwiseClone();
            options.Delimiter = delimiter;
            return options;
        }

        /// <summary>
        /// get header as string line 
        /// </summary>
        /// <returns></returns>
        public string AsColumnsHeaderLine()
        {
            var headers = string.Join(this.Delimiter, this.Columns.Select(x => $"{x.ColumnName}"));
            return headers;
        }

        /// <summary>
        /// Throws if MapColumnsByHeader is used without the header record
        /// </summary>
        /// <exception cref="InvalidOperationException"></exception>
        internal void ThrowsIfMapColumnsByHeaderWithoutHeader()
        {
            if (MapColumnsByHeader && !HasHeaderRecord)
            {
                throw new InvalidOperationException($"{nameof(MapColumnsByHeader)} requires {nameof(HasHeaderRecord)}.");
            }
        }

        /// <summary>
        /// Returns, for each column in the order of Columns, the index of its field in the record,
        /// looking for the column name in the header record.
        /// </summary>
        /// <param name="headerFields">the fields of the header record</param>
        /// <returns></returns>
        /// <exception cref="MissingColumnException">a column is not in the header</exception>
        internal int[] GetFieldIndexesByHeader(string[] headerFields)
        {
            var comparer = HeaderNameComparer ?? StringComparer.OrdinalIgnoreCase;
            var indexByName = new Dictionary<string, int>(comparer);
            for (int i = 0; i < headerFields.Length; i++)
            {
                var name = headerFields[i]?.Trim() ?? string.Empty;
                if (!indexByName.ContainsKey(name))
                {
                    indexByName.Add(name, i);
                }
            }

            var fieldIndexes = new int[Columns.Count];
            var missing = new List<string>();
            var missingNames = new List<string>();
            var columnIndex = 0;
            foreach (var column in Columns)
            {
                if (TryGetFieldIndex(indexByName, column, out var fieldIndex))
                {
                    fieldIndexes[columnIndex] = fieldIndex;
                }
                else if (column.IsOptional)
                {
                    // beyond every record: the field is missing (see CsvColumn.IsOptional)
                    fieldIndexes[columnIndex] = int.MaxValue;
                }
                else
                {
                    missing.Add(column.ColumnName ?? string.Empty);
                    var alternativeNames = column.AlternativeNames;
                    missingNames.Add(alternativeNames.Length == 0
                        ? column.ColumnName ?? string.Empty
                        : $"{column.ColumnName} (or {string.Join(", ", alternativeNames)})");
                }
                columnIndex++;
            }

            if (missing.Count > 0)
            {
                throw new MissingColumnException(missing.ToArray(),
                    $"The header record does not contain the columns: {string.Join(", ", missingNames)}.");
            }
            return fieldIndexes;
        }

        /// <summary>
        /// The index of the field of the column: the first name of the column (see CsvColumn.ColumnNames) found in the header
        /// </summary>
        private static bool TryGetFieldIndex(Dictionary<string, int> indexByName, CsvColumn column, out int fieldIndex)
        {
            if (indexByName.TryGetValue(column.ColumnName ?? string.Empty, out fieldIndex))
            {
                return true;
            }
            foreach (var name in column.AlternativeNames)
            {
                if (indexByName.TryGetValue(name ?? string.Empty, out fieldIndex))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Defines Event Handlers
        /// </summary>
        public sealed class EventHandlers
        {
            /// <summary>
            /// Occors when TinyCsv is staring
            /// </summary>
            public event EventHandler<StartEventArgs>? Start;

            /// <summary>
            /// Occors when TinyCsv is completed
            /// </summary>
            public event EventHandler<CompletedEventArgs>? Completed;

            /// <summary>
            /// Options
            /// </summary>
            internal CsvOptions<T> Options { get; private set; }

            /// <summary>
            /// Read event handlers
            /// </summary>
            public ReadEventHandlers Read { get; private set; }

            /// <summary>
            /// Write event handlers
            /// </summary>
            public WriteEventHandlers Write { get; private set; }

            /// <summary>
            /// Handlers is enabled
            /// </summary>
            public bool Enabled => Options?.EnableHandlers ?? false;

            /// <summary>
            /// Create an instance of EventHandlers
            /// </summary>
            /// <param name="options"></param>
            internal EventHandlers(CsvOptions<T> options)
            {
                Options = options;
                Read = new ReadEventHandlers(options);
                Write = new WriteEventHandlers(options);
            }

            /// <summary>
            /// Reise start event
            /// </summary>
            /// <param name="e"></param>
            internal void OnStart(StartEventArgs e)
            {
                if (!Enabled) return;
                Start?.Invoke(this, e);
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="e"></param>
            internal void OnStart()
            {
                if (!Enabled) return;
                OnStart(new StartEventArgs() { });
            }

            /// <summary>
            /// Reise completed event
            /// </summary>
            /// <param name="e"></param>
            internal void OnCompleted(CompletedEventArgs e)
            {
                if (!Enabled) return;
                Completed?.Invoke(this, e);
            }

            /// <summary>
            /// Reise completed event
            /// </summary>
            /// <param name="e"></param>
            internal void OnCompleted(int elements)
            {
                if (!Enabled) return;
                OnCompleted(new CompletedEventArgs
                {
                    Elements = elements
                });
            }
        }

        /// <summary>
        /// Defines Read Event Handlers
        /// </summary>
        public sealed class ReadEventHandlers
        {
            /// <summary>
            /// Options
            /// </summary>
            internal CsvOptions<T> Options { get; private set; }

            /// <summary>
            /// Occors when row header is reading
            /// </summary>
            public EventHandler<RowHeaderReadingEventArgs>? RowHeader { get; set; }

            /// <summary>
            /// Occors when row is reading
            /// </summary>
            public EventHandler<RowReadingEventArgs>? RowReading { get; set; }

            /// <summary>
            /// Occors when row is read
            /// </summary>
            public EventHandler<RowReadEventArgs<T>>? RowRead { get; set; }

            /// <summary>
            /// Create an instance of Read event handlers
            /// </summary>
            /// <param name="options"></param>
            internal ReadEventHandlers(CsvOptions<T> options)
            {
                Options = options;
            }

            /// <summary>
            /// Handlers is enabled
            /// </summary>
            public bool Enabled => Options?.EnableHandlers ?? false;

            /// <summary>
            /// Reise row header
            /// </summary>
            /// <param name="e"></param>
            internal void OnRowHeader(RowHeaderReadingEventArgs e)
            {
                if (!Enabled) return;
                RowHeader?.Invoke(this, e);
            }

            /// <summary>
            /// Reise row header
            /// </summary>
            /// <param name="rowHeader"></param>
            internal void OnRowHeader(int index, string rowHeader)
            {
                if (!Enabled) return;
                OnRowHeader(new RowHeaderReadingEventArgs
                {
                    Index = index,
                    RowHeader = rowHeader
                });
            }

            /// <summary>
            /// Reise row reading event
            /// </summary>
            /// <param name="e"></param>
            internal void OnRowReading(RowReadingEventArgs e)
            {
                if (!Enabled) return;
                RowReading?.Invoke(this, e);
            }

            /// <summary>
            /// Reise row reading event
            /// </summary>
            /// <param name="index"></param>
            /// <param name="row"></param>
            internal void OnRowReading(int index, string row)
            {
                if (!Enabled) return;
                OnRowReading(new RowReadingEventArgs
                {
                    Index = index,
                    Row = row
                });
            }

            /// <summary>
            /// Reise row read event
            /// </summary>
            /// <param name="e"></param>
            internal void OnRowRead(RowReadEventArgs<T> e)
            {
                if (!Enabled) return;
                RowRead?.Invoke(this, e);
            }

            /// <summary>
            /// Reise row read event
            /// </summary>
            /// <param name="index"></param>
            /// <param name="model"></param>
            internal void OnRowRead(int index, T model, string row)
            {
                if (!Enabled) return;
                OnRowRead(new RowReadEventArgs<T>
                {
                    Index = index,
                    Model = model,
                    Row = row
                });
            }
        }

        /// <summary>
        /// Defines Write Event Handlers
        /// </summary>
        public sealed class WriteEventHandlers
        {
            /// <summary>
            /// Options
            /// </summary>
            internal CsvOptions<T> Options { get; private set; }

            /// <summary>
            /// Occors when row header is writing
            /// </summary>
            public EventHandler<RowHeaderWritingEventArgs>? RowHeader { get; set; }

            /// <summary>
            /// Occors when row is writting
            /// </summary>
            public EventHandler<RowWritingEventArgs<T>>? RowWriting { get; set; }

            /// <summary>
            /// Occors when row is written
            /// </summary>
            public EventHandler<RowWrittenEventArgs<T>>? RowWritten { get; set; }

            /// <summary>
            /// Handlers is enabled
            /// </summary>
            public bool Enabled => Options?.EnableHandlers ?? false;

            /// <summary>
            /// Create an instance of Write event handlers
            /// </summary>
            /// <param name="options"></param>
            internal WriteEventHandlers(CsvOptions<T> options)
            {
                Options = options;
            }

            /// <summary>
            /// Reise row header is writing
            /// </summary>
            /// <param name="e"></param>
            internal void OnRowHeader(RowHeaderWritingEventArgs e)
            {
                if (!Enabled) return;
                RowHeader?.Invoke(this, e);
            }

            /// <summary>
            /// Reise row header is writing
            /// </summary>
            /// <param name="rowHeader"></param>
            internal void OnRowHeader(int index, string rowHeader)
            {
                if (!Enabled) return;
                OnRowHeader(new RowHeaderWritingEventArgs
                {
                    Index = index,
                    RowHeader = rowHeader
                });
            }

            /// <summary>
            /// Reise row is writing event
            /// </summary>
            /// <param name="e"></param>
            internal void OnRowWriting(RowWritingEventArgs<T> e)
            {
                if (!Enabled) return;
                RowWriting?.Invoke(this, e);
            }

            /// <summary>
            /// Reise row is writing event
            /// </summary>
            /// <param name="index"></param>
            /// <param name="model"></param>
            internal void OnRowWriting(int index, T model)
            {
                if (!Enabled) return;
                OnRowWriting(new RowWritingEventArgs<T>
                {
                    Index = index,
                    Model = model
                });
            }

            /// <summary>
            /// Reise row is written event
            /// </summary>
            /// <param name="index"></param>
            /// <param name="model"></param>
            /// <param name="row"></param>
            internal void OnRowWritten(int index, T model, string row)
            {
                if (!Enabled) return;
                var rowWritten = RowWritten;
                if (rowWritten == null) return;

                rowWritten.Invoke(this, new RowWrittenEventArgs<T>
                {
                    Index = index,
                    Model = model,
                    Row = row
                });
            }
        }
    }
}
