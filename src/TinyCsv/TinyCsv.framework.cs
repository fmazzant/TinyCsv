#if NET452 || NET462 || NET472 || NET48 || NETSTANDARD2_0

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
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using TinyCsv.Data;
    using TinyCsv.Streams;

    public sealed partial class TinyCsv<T>
    {
        /// <summary>
        /// Reads a csv file and returns a list of objects asynchronously.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        [Obsolete($"Use {nameof(LoadFromFileAsync)} to load from file", false)]
        public Task<IEnumerable<T>> LoadAsync(string path, CancellationToken cancellationToken = default)
        {
            return LoadFromFileAsync(path, cancellationToken);
        }

        /// <summary>
        /// Reads a csv file and returns a list of objects asynchronously.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public async Task<IEnumerable<T>> LoadFromFileAsync(string path, CancellationToken cancellationToken = default)
        {
            using (var streamReader = CreateReader(path))
            {
                return await LoadFromStreamAsync(streamReader, cancellationToken: cancellationToken).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Reads a csv file and returns a list of objects asynchronously.
        /// </summary>
        /// <param name="streamReader"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        [Obsolete($"Use {nameof(LoadFromStreamAsync)} to load from file", false)]
        public Task<IEnumerable<T>> LoadAsync(StreamReader streamReader, CancellationToken cancellationToken = default)
        {
            return LoadFromStreamAsync(streamReader, cancellationToken);
        }

        /// <summary>
        /// Reads a csv file and returns a list of objects asynchronously.
        /// </summary>
        /// <param name="streamReader"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<IEnumerable<T>> LoadFromStreamAsync(StreamReader streamReader, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var models = new List<T>();
            foreach (var model in LoadFromStream(streamReader))
            {
                cancellationToken.ThrowIfCancellationRequested();
                models.Add(model);
            }
            return await Task.FromResult(models);
        }

        /// <summary>
        /// Reads a csv file and returns a list of objects asynchronously.
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public Task<IEnumerable<T>> LoadFromStreamAsync(Stream stream, CancellationToken cancellationToken = default)
        {
            var streamReader = CreateReader(stream);
            return LoadFromStreamAsync(streamReader, cancellationToken);
        }

        /// <summary>
        /// Reads a csv from text and returns a list of objects asynchronously.
        /// </summary>
        /// <param name="text"></param>
        /// <param name="encoding">The default is UTF8</param>
        /// <returns></returns>
        public Task<IEnumerable<T>> LoadFromTextAsync(string text, Encoding encoding = null, CancellationToken cancellationToken = default)
        {
            return LoadFromStreamAsync(CreateReader(text, encoding), cancellationToken);
        }
    }
}
#endif

