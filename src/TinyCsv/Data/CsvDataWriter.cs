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
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// TinyCsvDataWriter
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class CsvDataWriter<T> : ICsvDataWriter<T>
    {
        private readonly ICsvOptions options;
        private readonly StreamWriter writer;

        /// <summary>
        /// Create a TinyCsvDataReader instance
        /// </summary>
        /// <param name="options"></param>
        /// <param name="reader"></param>
        public CsvDataWriter(ICsvOptions options, StreamWriter writer)
        {
            this.options = options;
            this.writer = writer;
        }

        /// <summary>
        /// Write lines
        /// </summary>
        /// <returns></returns>
        public int WriteLines(IEnumerable<string> lines)
        {
            var index = 0;
            foreach (var line in lines)
            {
                index += this.WriteLine(line);
            }
            return index;
        }

        /// <summary>
        /// write line
        /// </summary>
        /// <param name="line"></param>
        /// <returns></returns>
        public int WriteLine(string line)
        {
            writer.WriteLine(line);
            return 1;
        }

        /// <summary>
        /// Write lines and fields
        /// </summary>
        /// <returns></returns>
        public int WriteLinesAndFields(IEnumerable<string[]> lines)
        {
            var index = 0;
            foreach (var fields in lines)
            {
                var line = string.Join(options.Delimiter, fields);
                index += this.WriteLine(line);
            }
            return index;
        }

        /// <summary>
        /// Write lines
        /// </summary>
        /// <returns></returns>
        public async Task<int> WriteLinesAsync(IEnumerable<string> lines, CancellationToken cancellationToken = default)
        {
            var index = 0;
            foreach (var line in lines)
            {
                index += await this.WriteLineAsync(line, cancellationToken);
            }
            return index;
        }

        /// <summary>
        /// write line
        /// </summary>
        /// <param name="line"></param>
        /// <returns></returns>
        public async Task<int> WriteLineAsync(string line, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await writer.WriteLineAsync(line);
            return 1;
        }

        /// <summary>
        /// Write lines and fields
        /// </summary>
        /// <returns></returns>
        public async Task<int> WriteLinesAndFieldsAsync(IEnumerable<string[]> lines, CancellationToken cancellationToken = default)
        {
            var index = 0;
            foreach (var fields in lines)
            {
                var line = string.Join(options.Delimiter, fields);
                index += await this.WriteLineAsync(line, cancellationToken);
            }
            return index;
        }

        /// <summary>
        /// Clears all buffers for the current writer and causes any buffered data to be written to the underlying stream.
        /// </summary>
        public void Flush()
        {
            writer.Flush();
        }

        /// <summary>
        /// Clears all buffers for the current writer and causes any buffered data to be written to the underlying stream.
        /// </summary>
        public Task FlushAsync()
        {
            return writer.FlushAsync();
        }
    }
}