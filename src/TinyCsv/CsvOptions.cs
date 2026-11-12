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
        /// Represents a character encoding
        /// </summary>
        Encoding TextEncoding { get; set; }

        /// <summary>
        /// Allows skipping of initial rows without csv data
        /// </summary>
        uint RowsToSkip { get; set; }

        /// <summary>
        /// Allows skipt row by condition
        /// </summary>
        Func<string, int, bool> SkipRow { get; set; }

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
        /// Delimiter
        /// </summary>
        public string Delimiter { get; set; } = ";";

        /// <summary>
        /// Detects the delimiter from the first records when reading, among <see cref="DelimiterCandidates"/>.
        /// <see cref="Delimiter"/> is used when no candidate is found and when writing.
        /// </summary>
        public bool DetectDelimiter { get; set; } = false;

        /// <summary>
        /// The delimiters that <see cref="DetectDelimiter"/> looks for: comma, semicolon, tab and pipe
        /// </summary>
        public string[] DelimiterCandidates { get; set; } = { ",", ";", "	", "|" };

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
        /// Gets or Set the newline string defined for this environment
        /// </summary>
        public string NewLine { get; set; } = Environment.NewLine;

        /// <summary>
        /// Represents a character encoding
        /// </summary>
        public Encoding TextEncoding { get; set; } = Encoding.UTF8;

        /// <summary>
        /// Allows skipping of initial rows without csv data
        /// </summary>
        public uint RowsToSkip { get; set; } = 0;

        /// <summary>
        /// Allows skipping of initial columns without csv data
        /// </summary>
        [Obsolete($"Use: {nameof(SkipRow)} to skip the empty row.", false)]
        public bool SkipEmptyRows { get; set; } = false;

        /// <summary>
        /// Allows skipt row by condition
        /// </summary>
        public Func<string, int, bool> SkipRow { get; set; } = NotSkipRow;

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
        /// The default ignores the case.
        /// </summary>
        public StringComparer HeaderNameComparer { get; set; } = StringComparer.OrdinalIgnoreCase;

        /// <summary>
        /// The culture used to read and write the values: numbers, dates and the columns with a format.
        /// The default is the invariant culture, so the files do not depend on the culture of the machine.
        /// A column with a ColumnFormatProvider uses its provider.
        /// </summary>
        public CultureInfo Culture { get; set; } = CultureInfo.InvariantCulture;

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
        public Action<CsvException> BadDataFound { get; set; }

        /// <summary>
        /// Enable handlers if necessary
        /// </summary>
        public bool EnableHandlers { get; set; } = false;
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
            this.SetOptionPropertiesFromType(typeof(T));
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
            this.SetOptionPropertiesFromType(type);
        }

        /// <summary>
        /// Set Option Properties From Type
        /// </summary>
        /// <param name="type"></param>
        private void SetOptionPropertiesFromType(Type type)
        {
            FasterProperties = PropertyHelper<T>.GetPropertiesExpressionTree(type);
            Properties = new ConcurrentDictionary<string, PropertyInfo>();
            var properties = type.GetProperties(BindingFlags.Instance | BindingFlags.Public);
            foreach (var property in properties)
            {
                Properties.TryAdd(property.Name, property);
            }
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
            var columnIndex = 0;
            foreach (var column in Columns)
            {
                if (indexByName.TryGetValue(column.ColumnName ?? string.Empty, out var fieldIndex))
                {
                    fieldIndexes[columnIndex] = fieldIndex;
                }
                else
                {
                    missing.Add(column.ColumnName);
                }
                columnIndex++;
            }

            if (missing.Count > 0)
            {
                throw new MissingColumnException(missing.ToArray());
            }
            return fieldIndexes;
        }

        /// <summary>
        /// Defines Event Handlers
        /// </summary>
        public sealed class EventHandlers
        {
            /// <summary>
            /// Occors when TinyCsv is staring
            /// </summary>
            public event EventHandler<StartEventArgs> Start;

            /// <summary>
            /// Occors when TinyCsv is completed
            /// </summary>
            public event EventHandler<CompletedEventArgs> Completed;

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
            public EventHandler<RowHeaderReadingEventArgs> RowHeader { get; set; }

            /// <summary>
            /// Occors when row is reading
            /// </summary>
            public EventHandler<RowReadingEventArgs> RowReading { get; set; }

            /// <summary>
            /// Occors when row is read
            /// </summary>
            public EventHandler<RowReadEventArgs<T>> RowRead { get; set; }

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
            public EventHandler<RowHeaderWritingEventArgs> RowHeader { get; set; }

            /// <summary>
            /// Occors when row is writting
            /// </summary>
            public EventHandler<RowWritingEventArgs<T>> RowWriting { get; set; }

            /// <summary>
            /// Occors when row is written
            /// </summary>
            public EventHandler<RowWrittenEventArgs<T>> RowWritten { get; set; }

            /// <summary>
            /// Occors when row is written
            /// </summary>
#pragma warning disable CS0618 // RowWrittinEventArgs<T> is obsolete
            [Obsolete("Use RowWritten instead. RowWrittin will be removed in 4.0.")]
            public EventHandler<RowWrittinEventArgs<T>> RowWrittin { get; set; }
#pragma warning restore CS0618

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
#pragma warning disable CS0618 // RowWrittin is kept until 4.0
                var rowWrittin = RowWrittin;
                if (RowWritten == null && rowWrittin == null) return;

                // RowWrittinEventArgs<T> derives from RowWrittenEventArgs<T>, so the same
                // instance is passed to the handlers of both RowWritten and RowWrittin.
                RowWrittenEventArgs<T> e = rowWrittin != null
                    ? new RowWrittinEventArgs<T>()
                    : new RowWrittenEventArgs<T>();
                e.Index = index;
                e.Model = model;
                e.Row = row;

                RowWritten?.Invoke(this, e);
                rowWrittin?.Invoke(this, (RowWrittinEventArgs<T>)e);
#pragma warning restore CS0618
            }
        }
    }
}
