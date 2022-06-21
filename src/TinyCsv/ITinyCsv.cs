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

using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace TinyCsv
{
    public interface ITinyCsv<T> where T : class, new()
    {
        CsvOptions<T> Options { get; }

        /// <summary>
        /// Reads a csv file and returns a list of objects.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        ICollection<T> LoadFromFile(string path);

        /// <summary>
        /// Reads a csv file and returns a list of objects.
        /// </summary>
        /// <param name="streamReader"></param>
        /// <returns></returns>
        IEnumerable<T> Load(StreamReader streamReader);

        /// <summary>
        /// Reads a csv file and returns a list of objects.
        /// </summary>
        /// <param name="streamReader"></param>
        /// <returns></returns>
        IEnumerable<T> LoadFromStream(StreamReader streamReader);

        /// <summary>
        /// Reads a csv file and returns a list of objects.
        /// </summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        IEnumerable<T> LoadFromStream(Stream stream);

#if NET452 || NET46 || NET47 || NET48 || NETSTANDARD2_0

        /// <summary>
        /// Reads a csv file and returns a list of objects asynchronously.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        Task<ICollection<T>> LoadFromFileAsync(string path, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Reads a csv file and returns a list of objects asynchronously.
        /// </summary>
        /// <param name="streamReader"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<ICollection<T>> LoadFromStreamAsync(StreamReader streamReader, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Reads a csv file and returns a list of objects asynchronously.
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<ICollection<T>> LoadFromStreamAsync(Stream stream, CancellationToken cancellationToken = default(CancellationToken));

#endif

#if NET5_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER

        /// <summary>
        /// Reads a csv file and returns a list of objects asynchronously.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        Task<ICollection<T>> LoadFromFileAsync(string path, CancellationToken cancellationToken = default(CancellationToken));


        /// <summary>
        /// Reads a csv file and returns a list of objects asynchronously.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        IAsyncEnumerable<T> LoadFromStreamAsync(StreamReader streamReader, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Reads a csv file and returns a list of objects asynchronously.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        IAsyncEnumerable<T> LoadFromStreamAsync(Stream stream, CancellationToken cancellationToken = default(CancellationToken));

#endif

        /// <summary>
        /// Writes a list of objects to a csv file.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="models"></param>
        void Save(string path, IEnumerable<T> models);

        /// <summary>
        /// Writes a list of objects to a csv file.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="models"></param>
        void Save(StreamWriter streamWriter, IEnumerable<T> models);

        /// <summary>
        /// Writes a list of objects to a csv file.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="models"></param>
        void Save(Stream stream, IEnumerable<T> models);

        /// <summary>
        /// Writes a list of objects to a csv file asynchronously.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="models"></param>
        Task SaveAsync(string path, IEnumerable<T> models, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Writes a list of objects to a csv file asynchronously.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="models"></param>
        Task SaveAsync(StreamWriter streamWriter, IEnumerable<T> models, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Writes a list of objects to a csv file asynchronously.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="models"></param>
        Task SaveAsync(Stream stream, IEnumerable<T> models, CancellationToken cancellationToken = default(CancellationToken));
    }
}