#if NET5_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER

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
    using TinyCsv.Streams;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
    using System.Threading;
    using System.Runtime.CompilerServices;
    using TinyCsv.Data;
    using TinyCsv.Extensions;

    public sealed partial class TinyCsv<T>
    {
        /// <summary>
        /// Reads a csv file and returns a list of objects asynchronously.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public IAsyncEnumerable<T> LoadFromFileAsync(string path, CancellationToken cancellationToken = default)
        {
            var streamReader = new StreamReader(path);
            return LoadFromStreamAsync(streamReader, cancellationToken);
        }

        /// <summary>
        /// Reads a csv file and returns a list of objects asynchronously.
        /// </summary>
        /// <param name="streamReader"></param>
        /// <returns></returns>
        [Obsolete($"Use {nameof(LoadFromStreamAsync)} to load from file", false)]
        public IAsyncEnumerable<T> LoadAsync(StreamReader streamReader)
        {
            return this.LoadFromStreamAsync(streamReader);
        }

        /// <summary>
        /// Reads a csv file and returns a list of objects asynchronously.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public async IAsyncEnumerable<T> LoadFromStreamAsync(StreamReader streamReader, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var index = 0;
            var options = this.Options;
            var dataReader = new TinyCsvDataReader<T>(options, streamReader);
            var hasHeaderRecord = options.HasHeaderRecord;

            options.Handlers.OnStart();
            await foreach (var line in dataReader.ReadLinesAsync(cancellationToken))
            {
                cancellationToken.ThrowIfCancellationRequested();

                var currentIndex = index;
                index++;

                if (currentIndex == 0 && hasHeaderRecord)
                {
                    options.Handlers.Read.OnRowReading(currentIndex, line);
                    options.Handlers.Read.OnRowHeader(currentIndex, line);
                    continue;
                }
                options.Handlers.Read.OnRowReading(currentIndex, line);
                var fields = dataReader.GetFieldsByLine(line);
                var model = fields.GetModelFromStringArray<T>(options);
                options.Handlers.Read.OnRowRead(currentIndex, model, line);
                yield return model;
            }
            options.Handlers.OnCompleted(index);
        }

        /// <summary>
        /// Reads a csv file and returns a list of objects asynchronously.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public IAsyncEnumerable<T> LoadFromStreamAsync(Stream stream, CancellationToken cancellationToken = default)
        {
            var streamReader = new StreamReader(stream);
            return LoadFromStreamAsync(streamReader, cancellationToken);
        }

        /// <summary>
        /// Reads a csv from text and returns a list of objects asynchronously.
        /// </summary>
        /// <param name="text"></param>
        /// <param name="encoding"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public IAsyncEnumerable<T> LoadFromTextAsync(string text, Encoding encoding = null, CancellationToken cancellationToken = default)
        {
            var memoryStream = new TextMemoryStream(text, encoding ?? Options.TextEncoding);
            return LoadFromStreamAsync(memoryStream, cancellationToken);
        }
    }
}

#endif