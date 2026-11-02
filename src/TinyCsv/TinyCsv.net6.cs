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
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Runtime.CompilerServices;
    using System.Text;
    using System.Threading;
    using TinyCsv.Data;
    using TinyCsv.Streams;

    public sealed partial class TinyCsv<T>
    {
        /// <summary>
        /// Reads a csv file and returns a list of objects asynchronously.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public IAsyncEnumerable<T> LoadFromFileAsync(string path, CancellationToken cancellationToken = default)
        {
            var streamReader = CreateReader(path);
            return DisposeAfterEnumerationAsync(streamReader, LoadFromStreamAsync(streamReader, cancellationToken));
        }

        /// <summary>
        /// Enumerates the items and then disposes the owner (i.e. the reader of a file)
        /// </summary>
        /// <typeparam name="TItem"></typeparam>
        /// <param name="owner"></param>
        /// <param name="items"></param>
        /// <returns></returns>
        private static async IAsyncEnumerable<TItem> DisposeAfterEnumerationAsync<TItem>(IDisposable owner, IAsyncEnumerable<TItem> items)
        {
            using (owner)
            {
                await foreach (var item in items)
                {
                    yield return item;
                }
            }
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
            var reader = new CsvModelReader<T>(Options, streamReader);
            await foreach (var line in reader.DataReader.ReadLinesAsync(cancellationToken))
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (reader.TryReadModel(line, out var model))
                {
                    yield return model;
                    reader.OnModelRead(model, line);
                }
            }
            reader.OnCompleted();
        }

        /// <summary>
        /// Reads a csv file and returns a list of objects asynchronously.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public IAsyncEnumerable<T> LoadFromStreamAsync(Stream stream, CancellationToken cancellationToken = default)
        {
            var streamReader = CreateReader(stream);
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
            return LoadFromStreamAsync(CreateReader(text, encoding), cancellationToken);
        }
    }
}

#endif