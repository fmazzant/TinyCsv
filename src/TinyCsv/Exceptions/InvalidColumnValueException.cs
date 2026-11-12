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

namespace Mafesoft.TinyCsv.Exceptions
{
    using System;
    using System.Runtime.Serialization;

    /// <summary>
    /// Defines Invalid Column Value Exception
    /// </summary>
    [Serializable]
    public class InvalidColumnValueException : CsvException
    {
        /// <summary>
        /// Create a new instance of InvalidColumnValueException
        /// </summary>
        public InvalidColumnValueException()
            : base()
        {

        }

        /// <summary>
        /// Create a new instance of InvalidColumnValueException
        /// </summary>
        /// <param name="message"></param>
        public InvalidColumnValueException(string? message)
            : base(message)
        {
        }

        /// <summary>
        /// Create a new instance of InvalidColumnValueException
        /// </summary>
        /// <param name="message"></param>
        /// <param name="innerException"></param>
        public InvalidColumnValueException(string? message, Exception? innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// Create a new instance of InvalidColumnValueException for a value of a column
        /// </summary>
        /// <param name="columnName">the name of the column</param>
        /// <param name="value">the value, as it is in the csv</param>
        /// <param name="message"></param>
        /// <param name="innerException">the exception of the converter, if any</param>
        public InvalidColumnValueException(string columnName, string value, string message, Exception? innerException = null)
            : base(message, innerException)
        {
            ColumnName = columnName;
            Value = value;
        }

        /// <summary>
        /// Create a new instance of InvalidColumnValueException
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        protected InvalidColumnValueException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {

        }
    }
}