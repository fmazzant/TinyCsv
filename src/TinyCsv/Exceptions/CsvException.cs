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

namespace TinyCsv.Exceptions
{
    using System;
    using System.Runtime.Serialization;

    /// <summary>
    /// The base of the exceptions thrown while reading a csv, with the position of the error:
    /// the row, and the column and the value when the error is about a value.
    /// </summary>
    [Serializable]
    public class CsvException : Exception
    {
        /// <summary>
        /// The index of the row with the error, starting from 0 (the same index of the read events), if known
        /// </summary>
        public int? RowIndex { get; private set; }

        /// <summary>
        /// The row with the error, as it is in the csv, if known
        /// </summary>
        public string Row { get; private set; }

        /// <summary>
        /// The name of the column with the error, when the error is about a value
        /// </summary>
        public string ColumnName { get; protected set; }

        /// <summary>
        /// The value with the error, as it is in the csv, when the error is about a value
        /// </summary>
        public string Value { get; protected set; }

        /// <summary>
        /// The message, with the row and the column of the error
        /// </summary>
        public override string Message
        {
            get
            {
                var position = RowIndex.HasValue ? $"row {RowIndex}" : null;
                if (ColumnName != null)
                {
                    position = position == null ? $"column {ColumnName}" : $"{position}, column {ColumnName}";
                }
                return position == null ? base.Message : $"{base.Message} ({position})";
            }
        }

        /// <summary>
        /// Create a new instance of CsvException
        /// </summary>
        public CsvException()
            : base()
        {
        }

        /// <summary>
        /// Create a new instance of CsvException
        /// </summary>
        /// <param name="message"></param>
        public CsvException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Create a new instance of CsvException
        /// </summary>
        /// <param name="message"></param>
        /// <param name="innerException"></param>
        public CsvException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// Create a new instance of CsvException
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        protected CsvException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        /// <summary>
        /// Sets the row of the error
        /// </summary>
        /// <param name="rowIndex"></param>
        /// <param name="row"></param>
        internal void SetRow(int rowIndex, string row)
        {
            RowIndex = rowIndex;
            Row = row;
        }
    }
}
