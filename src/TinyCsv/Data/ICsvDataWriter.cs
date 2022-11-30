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
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    public interface ICsvDataWriter<T>
    {
        /// <summary>
        /// Write lines
        /// </summary>
        /// <returns></returns>
        int WriteLines(IEnumerable<string> lines);

        /// <summary>
        /// write line
        /// </summary>
        /// <param name="line"></param>
        /// <returns></returns>
        int WriteLine(string line);

        /// <summary>
        /// Write lines and fields
        /// </summary>
        /// <returns></returns>
        int WriteLinesAndFields(IEnumerable<string[]> lines);

        /// <summary>
        /// Write lines
        /// </summary>
        /// <returns></returns>
        Task<int> WriteLinesAsync(IEnumerable<string> lines, CancellationToken cancellationToken = default);

        /// <summary>
        /// write line
        /// </summary>
        /// <param name="line"></param>
        /// <returns></returns>
        Task<int> WriteLineAsync(string line, CancellationToken cancellationToken = default);

        /// <summary>
        /// Write lines and fields
        /// </summary>
        /// <returns></returns>
        Task<int> WriteLinesAndFieldsAsync(IEnumerable<string[]> lines, CancellationToken cancellationToken = default);
    }

}
