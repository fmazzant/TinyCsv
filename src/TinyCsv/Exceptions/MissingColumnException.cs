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
    /// Defines Missing Column Exception: a column is not in the header record
    /// </summary>
    [Serializable]
    public class MissingColumnException : Exception
    {
        /// <summary>
        /// The names of the columns that are not in the header record
        /// </summary>
        public string[] MissingColumns { get; } = new string[0];

        /// <summary>
        /// Create a new instance of MissingColumnException
        /// </summary>
        public MissingColumnException()
            : base()
        {
        }

        /// <summary>
        /// Create a new instance of MissingColumnException
        /// </summary>
        /// <param name="missingColumns">the names of the columns that are not in the header record</param>
        public MissingColumnException(string[] missingColumns)
            : base($"The header record does not contain the columns: {string.Join(", ", missingColumns)}.")
        {
            MissingColumns = missingColumns;
        }

        /// <summary>
        /// Create a new instance of MissingColumnException
        /// </summary>
        /// <param name="message"></param>
        public MissingColumnException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Create a new instance of MissingColumnException
        /// </summary>
        /// <param name="message"></param>
        /// <param name="innerException"></param>
        public MissingColumnException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// Create a new instance of MissingColumnException
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        protected MissingColumnException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
