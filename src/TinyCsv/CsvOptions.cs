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

namespace TinyCsv
{
    using System;
    using System.Text;
    using TinyCsv.Args;

    /// <summary>
    /// Csv Options definition
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public sealed class CsvOptions<T>
    {
        /// <summary>
        /// Has header record
        /// </summary>
        public bool HasHeaderRecord { get; set; }

        /// <summary>
        /// Columns
        /// </summary>
        public CsvOptionsColumns<T> Columns { get; internal set; }

        /// <summary>
        /// Delimiter
        /// </summary>
        public string Delimiter { get; set; } = ";";

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
        [Obsolete($"Use: {nameof(SkipRow)} to skip the empty row.", true)]
        public bool SkipEmptyRows { get; set; } = false;

        /// <summary>
        /// Allows skipt row by condition
        /// </summary>
        public Func<string, int, bool> SkipRow { get; set; } = (row, index) => false;

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
        /// Respects new line (either \r\n or \n) characters inside field values enclosed in double quotes.
        /// </summary>
        public bool AllowRowEnclosedInDoubleQuotesValues { get; set; } = true;

        /// <summary>
        /// Allows the sequence "\"" to be a valid quoted value (in addition to the standard """")
        /// </summary>
        public bool AllowBackSlashToEscapeQuote { get; set; } = false;

        /// <summary>
        /// Allow the last char is the delimiter char
        /// </summary>
        public bool EndOfLineDelimiterChar { get; set; } = true;

        /// <summary>
        /// Event Handlers
        /// </summary>
        public EventHandlers Handlers { get; }

        /// <summary>
        /// Create a CsvOptions
        /// </summary>
        public CsvOptions()
        {
            Columns = new CsvOptionsColumns<T>();
            Handlers = new EventHandlers(this);
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
                Start?.Invoke(this, e);
            }

            /// <summary>
            /// Reise completed event
            /// </summary>
            /// <param name="e"></param>
            internal void OnCompleted(CompletedEventArgs e)
            {
                Completed?.Invoke(this, e);
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
            /// Reise row header
            /// </summary>
            /// <param name="e"></param>
            internal void OnRowHeader(RowHeaderReadingEventArgs e)
            {
                RowHeader?.Invoke(this, e);
            }

            /// <summary>
            /// Reise row header
            /// </summary>
            /// <param name="rowHeader"></param>
            internal void OnRowHeader(int index, string rowHeader)
            {
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
                RowReading?.Invoke(this, e);
            }

            /// <summary>
            /// Reise row reading event
            /// </summary>
            /// <param name="index"></param>
            /// <param name="row"></param>
            internal void OnRowReading(int index, string row)
            {
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
                RowRead?.Invoke(this, e);
            }

            /// <summary>
            /// Reise row read event
            /// </summary>
            /// <param name="index"></param>
            /// <param name="model"></param>
            internal void OnRowRead(int index, T model, string row)
            {
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
            public EventHandler<RowWrittinEventArgs<T>> RowWrittin { get; set; }

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
                RowHeader?.Invoke(this, e);
            }

            /// <summary>
            /// Reise row header is writing
            /// </summary>
            /// <param name="rowHeader"></param>
            internal void OnRowHeader(int index, string rowHeader)
            {
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
                RowWriting?.Invoke(this, e);
            }

            /// <summary>
            /// Reise row is writing event
            /// </summary>
            /// <param name="index"></param>
            /// <param name="model"></param>
            internal void OnRowWriting(int index, T model)
            {
                OnRowWriting(new RowWritingEventArgs<T>
                {
                    Index = index,
                    Model = model
                });
            }

            /// <summary>
            /// Reise row is writtin event
            /// </summary>
            /// <param name="e"></param>
            internal void OnRowWrittin(RowWrittinEventArgs<T> e)
            {
                RowWrittin?.Invoke(this, e);
            }

            /// <summary>
            /// Reise row is writtin event
            /// </summary>
            /// <param name="index"></param>
            /// <param name="row"></param>
            internal void OnRowWrittin(int index, T model, string row)
            {
                OnRowWrittin(new RowWrittinEventArgs<T>
                {
                    Index = index,
                    Model = model,
                    Row = row
                });
            }
        }
    }
}
