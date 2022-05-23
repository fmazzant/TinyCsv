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
    using TinyCsv.Args;
    using System;

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
        /// Allows skipping of initial rows without csv data
        /// </summary>
        public uint RowsToSkip { get; set; } = 0;

        /// <summary>
        /// Allows skipping of initial columns without csv data
        /// </summary>
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
        /// Event handler for start
        /// </summary>
        public event EventHandler<StartEventArgs> OnStart;


        /// <summary>
        /// Event handler for completed
        /// </summary>
        public event EventHandler<CompletedEventArgs> OnCompleted;

        /// <summary>
        /// Create a CsvOptions
        /// </summary>
        public CsvOptions()
        {
            Columns = new CsvOptionsColumns<T>();
        }
    }
}
