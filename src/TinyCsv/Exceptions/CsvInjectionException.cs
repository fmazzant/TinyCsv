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
    /// A text value that starts with an injection character (see CsvOptions.InjectionProtection = Throw),
    /// thrown when writing, with the index of the row, the column and the value
    /// </summary>
    [Serializable]
    public class CsvInjectionException : CsvException
    {
        /// <summary>
        /// Create a new instance of CsvInjectionException
        /// </summary>
        public CsvInjectionException()
            : base()
        {
        }

        /// <summary>
        /// Create a new instance of CsvInjectionException
        /// </summary>
        /// <param name="message"></param>
        public CsvInjectionException(string? message)
            : base(message)
        {
        }

        /// <summary>
        /// Create a new instance of CsvInjectionException
        /// </summary>
        /// <param name="message"></param>
        /// <param name="innerException"></param>
        public CsvInjectionException(string? message, Exception? innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// Create a new instance of CsvInjectionException for a value of a column
        /// </summary>
        /// <param name="columnName">the name of the column</param>
        /// <param name="value">the value to write</param>
        public CsvInjectionException(string columnName, string value)
            : base($"The value '{value}' starts with an injection character.")
        {
            ColumnName = columnName;
            Value = value;
        }

        /// <summary>
        /// Create a new instance of CsvInjectionException
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
#if NET8_0_OR_GREATER
        // as the constructor of Exception: the formatter-based serialization is obsolete on .NET 8+
        [Obsolete("This API supports obsolete formatter-based serialization. It should not be called or extended by application code.", DiagnosticId = "SYSLIB0051", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
#endif
        protected CsvInjectionException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
