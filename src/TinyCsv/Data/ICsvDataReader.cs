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
namespace TinyCsv.Data
{
    using System;
    using System.Collections.Generic;
    using System.Threading;

    /// <summary>
    /// ICsvDataReader
    /// </summary>
    public interface ICsvDataReader<T>
    {
        /// <summary>
        /// Get lines
        /// </summary>
        /// <returns></returns>
        IEnumerable<string> ReadLines();

        /// <summary>
        /// Get fields by line
        /// </summary>
        /// <param name="line"></param>
        /// <returns></returns>
        [Obsolete("Use: string[] GetFieldsByLine(string line, int columnsCount)", true)]
        string[] GetFieldsByLine(string line);

        /// <summary>
        /// Get fields by line
        /// </summary>
        /// <param name="line"></param>
        /// <param name="columnCount"></param>
        /// <returns></returns>
        string[] GetFieldsByLine(string line, int columnsCount, int potenrialColumnsCount = 1024);

        /// <summary>
        /// Get lines field
        /// </summary>
        /// <returns></returns>
        IEnumerable<string[]> ReadLinesAndFields();

#if NET5_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
        /// <summary>
        /// Read Lines 
        /// </summary>
        /// <returns></returns>
        IAsyncEnumerable<string> ReadLinesAsync(CancellationToken cancellationToken = default);
#endif
    }
}
