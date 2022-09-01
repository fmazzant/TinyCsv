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
    using System.Linq;
    using System.Runtime.CompilerServices;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using TinyCsv.Extensions;

    /// <summary>
    /// TinyCsv is a simple csv reader/writer library.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public sealed class TinyCsv<T> : ITinyCsv<T> where T : class, new()
    {
        /// <summary>
        /// Options
        /// </summary>
        internal CsvOptions<T> Options { get; private set; }

        /// <summary>
        /// Create a new instance of TinyCsv
        /// </summary>
        /// <param name="options"></param>
        public TinyCsv(Action<CsvOptions<T>> options)
        {
            Options = new CsvOptions<T>();
            options(Options);
        }

        /// <summary>
        /// Create a new instance of TinyCsv
        /// </summary>
        /// <param name="options"></param>
        public TinyCsv(CsvOptions<T> options)
        {
            Options = options;
        }

        /// <summary>
        /// Create a new instance of TinyCsv taking the options from attributes
        /// </summary>
        public TinyCsv()
        {
            Options = new CsvOptions<T>();
        }

        /// <summary>
        /// Reads a csv file and returns a list of objects.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        [Obsolete($"Use {nameof(LoadFromFile)} to load from file", false)]
        public ICollection<T> Load(string path)
        {
            return LoadFromFile(path);
        }

        /// <summary>
        /// Reads a csv file and returns a list of objects.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public ICollection<T> LoadFromFile(string path)
        {
            using var streamReader = new StreamReader(path);
            var models = Load(streamReader);
            return new List<T>(models);
        }

        /// <summary>
        /// Reads a csv file and returns a list of objects.
        /// </summary>
        /// <param name="streamReader"></param>
        /// <returns></returns>
        public IEnumerable<T> Load(StreamReader streamReader)
        {
            return LoadFromStream(streamReader);
        }

        /// <summary>
        /// Reads a csv file and returns a list of objects.
        /// </summary>
        /// <param name="streamReader"></param>
        /// <returns></returns>
        public IEnumerable<T> LoadFromStream(StreamReader streamReader)
        {
            var index = 0;

            while (!streamReader.EndOfStream)
            {
                if (index < Options.RowsToSkip)
                {
                    streamReader.ReadLine();
                    index++;
                    continue;
                }
                break;
            }

            if (Options.HasHeaderRecord)
            {
                while (!streamReader.EndOfStream)
                {
                    var line = streamReader.ReadLine();
                    var skip = line.SkipRow(index++, this.Options);
                    if (skip) continue;
                    GetHeaderFromLine(index - 1, line);
                    break;
                }
            }

            while (!streamReader.EndOfStream)
            {
                var line = streamReader.ReadLine();
                var skip = line.SkipRow(index++, this.Options);
                if (skip) continue;
                var model = this.GetModelFromLine(index - 1, line);
                yield return model;
            }
        }

        /// <summary>
        /// Reads a csv file and returns a list of objects.
        /// </summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        public IEnumerable<T> LoadFromStream(Stream stream)
        {
            return LoadFromStream(new StreamReader(stream));

        }

        /// <summary>
        /// Reads a csv from text and returns a list of objects.
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public IEnumerable<T> LoadFromText(string text, Encoding encoding = null)
        {
            var textEncoding = encoding ?? Options.TextEncoding;
            var bytes = textEncoding.GetBytes(text);
            var memoryStream = new MemoryStream(bytes);
            return LoadFromStream(memoryStream);
        }

#if NET452 || NET46 || NET47 || NET48 || NETSTANDARD2_0

        /// <summary>
        /// Reads a csv file and returns a list of objects asynchronously.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        [Obsolete($"Use {nameof(LoadFromFileAsync)} to load from file", false)]
        public Task<ICollection<T>> LoadAsync(string path, CancellationToken cancellationToken = default(CancellationToken))
        {
            return LoadFromFileAsync(path, cancellationToken);
        }

        /// <summary>
        /// Reads a csv file and returns a list of objects asynchronously.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public async Task<ICollection<T>> LoadFromFileAsync(string path, CancellationToken cancellationToken = default(CancellationToken))
        {
            using (StreamReader file = new StreamReader(path))
            {
                var models = await this.LoadFromStreamAsync(file, cancellationToken);
                return new List<T>(models);
            }
        }

        /// <summary>
        /// Reads a csv file and returns a list of objects asynchronously.
        /// </summary>
        /// <param name="streamReader"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        [Obsolete($"Use {nameof(LoadFromFileAsync)} to load from file", false)]
        public Task<ICollection<T>> LoadAsync(StreamReader streamReader, CancellationToken cancellationToken = default(CancellationToken))
        {
            return LoadFromStreamAsync(streamReader, cancellationToken);
        }

        /// <summary>
        /// Reads a csv file and returns a list of objects asynchronously.
        /// </summary>
        /// <param name="streamReader"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<ICollection<T>> LoadFromStreamAsync(StreamReader streamReader, CancellationToken cancellationToken = default(CancellationToken))
        {
            var models = new List<T>();
            var index = 0;

            while (!streamReader.EndOfStream)
            {
                if (index++ < Options.RowsToSkip)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    await streamReader.ReadLineAsync().ConfigureAwait(false);
                    continue;
                }
                break;
            }

            if (Options.HasHeaderRecord)
            {
                while (!streamReader.EndOfStream)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var headerLine = await streamReader.ReadLineAsync().ConfigureAwait(false);
                    var skip = headerLine.SkipRow(index++, this.Options);
                    if (skip) continue;
                    break;
                }
            }

            while (!streamReader.EndOfStream)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var line = await streamReader.ReadLineAsync().ConfigureAwait(false);
                var skip = line.SkipRow(index++, this.Options);
                if (skip) continue;
                var model = this.GetModelFromLine(index - 1, line);
                models.Add(model);
            }

            return models;
        }

        /// <summary>
        /// Reads a csv file and returns a list of objects asynchronously.
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public Task<ICollection<T>> LoadFromStreamAsync(Stream stream, CancellationToken cancellationToken = default(CancellationToken))
        {
            using var streamReader = new StreamReader(stream);
            return LoadFromStreamAsync(streamReader, cancellationToken);
        }

        /// <summary>
        /// Reads a csv from text and returns a list of objects asynchronously.
        /// </summary>
        /// <param name="text"></param>
        /// <param name="encoding">The default is UTF8</param>
        /// <returns></returns>
        public Task<ICollection<T>> LoadFromTextAsync(string text, Encoding encoding = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            var textEncoding = encoding ?? Options.TextEncoding;
            var bytes = textEncoding.GetBytes(text);
            var memoryStream = new MemoryStream(bytes);
            return LoadFromStreamAsync(memoryStream, cancellationToken);
        }
#endif

#if NET5_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER

        /// <summary>
        /// Reads a csv file and returns a list of objects asynchronously.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public async Task<ICollection<T>> LoadFromFileAsync(string path, CancellationToken cancellationToken = default(CancellationToken))
        {
            using (StreamReader streamReader = new StreamReader(path))
            {
                var models = await this.LoadFromStreamAsync(streamReader).ToListAsync(cancellationToken);
                return models;
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
            var index = 0;

            while (!streamReader.EndOfStream)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (index++ < Options.RowsToSkip)
                {
                    await streamReader.ReadLineAsync().ConfigureAwait(false);
                    continue;
                }
                break;
            }

            if (Options.HasHeaderRecord)
            {
                while (!streamReader.EndOfStream)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var line = await streamReader.ReadLineAsync().ConfigureAwait(false);
                    var skip = line.SkipRow(index++, this.Options);
                    if (skip) continue;
                    break;
                }
            }

            while (!streamReader.EndOfStream)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var line = await streamReader.ReadLineAsync().ConfigureAwait(false);
                var skip = line.SkipRow(index++, this.Options);
                if (skip) continue;
                var model = this.GetModelFromLine(index - 1, line);
                yield return model;
            }
        }

        /// <summary>
        /// Reads a csv file and returns a list of objects asynchronously.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public IAsyncEnumerable<T> LoadFromStreamAsync(Stream stream, CancellationToken cancellationToken = default(CancellationToken))
        {
            using var streamReader = new StreamReader(stream);
            return LoadFromStreamAsync(streamReader, cancellationToken);
        }

        /// <summary>
        /// Reads a csv from text and returns a list of objects asynchronously.
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public IAsyncEnumerable<T> LoadFromTextAsync(string text, Encoding encoding = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            var textEncoding = encoding ?? Options.TextEncoding;
            var bytes = textEncoding.GetBytes(text);
            var memoryStream = new MemoryStream(bytes);
            return LoadFromStreamAsync(memoryStream, cancellationToken);
        }
#endif

        /// <summary>
        /// Get header from line
        /// </summary>
        /// <param name="index"></param>
        /// <param name="line"></param>
        /// <returns></returns>
        private void GetHeaderFromLine(int index, string line)
        {
            Options.Handlers.Read.OnRowHeader(index, line);
        }

        /// <summary>
        /// Build T object from line
        /// </summary>
        /// <param name="line"></param>
        /// <returns></returns>
        private T GetModelFromLine(int index, string line)
        {
            Options.Handlers.Read.OnRowReading(index, line);
            var values = line.SplitLine(this.Options);

            var model = new T();
            foreach (var column in Options.Columns)
            {
                var value = values[column.ColumnIndex].TrimData(this.Options);
                var columnExpression = column.ColumnExpression;
                var propertyName = columnExpression.GetPropertyName();
                var property = typeof(T).GetProperty(propertyName);
                var typedValue = column.Converter.ConvertBack(value, column.ColumnType, null, column.ColumnFormatProvider);
                property.SetValue(model, typedValue);
            }
            Options.Handlers.Read.OnRowRead(index, model, line);
            return model;
        }

        /// <summary>
        /// Writes a list of objects to a csv file.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="models"></param>
        public void Save(string path, IEnumerable<T> models)
        {
            using (StreamWriter streamWriter = new StreamWriter(path))
            {
                Save(streamWriter, models);
            }
        }

        /// <summary>
        /// Writes a list of objects to a csv file.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="models"></param>
        public void Save(StreamWriter streamWriter, IEnumerable<T> models)
        {
            var index = 0;
            if (Options.HasHeaderRecord)
            {
                var headers = GetHeaderFromOptions(index++);
                streamWriter.WriteLine(headers);
            }

            foreach (var model in models)
            {
                var line = this.GetLineFromModel(index++, model);
                streamWriter.WriteLine(line);
            }

            streamWriter.Flush();
        }

        /// <summary>
        /// Writes a list of objects to a csv file.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="models"></param>
        public void Save(Stream stream, IEnumerable<T> models)
        {
            Save(new StreamWriter(stream), models);
        }

        /// <summary>
        /// Writes a list of objects to a csv file asynchronously.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="models"></param>
        public async Task SaveAsync(string path, IEnumerable<T> models, CancellationToken cancellationToken = default(CancellationToken))
        {
            using (StreamWriter file = new StreamWriter(path))
            {
                var index = 0;

                if (Options.HasHeaderRecord)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var headers = GetHeaderFromOptions(index++);
                    await file.WriteLineAsync(headers).ConfigureAwait(false);
                }

                foreach (var model in models)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var line = this.GetLineFromModel(index++, model);
                    await file.WriteLineAsync(line).ConfigureAwait(false);
                }

                await file.FlushAsync().ConfigureAwait(false);
                file.Close();
            }
        }

        /// <summary>
        /// Writes a list of objects to a csv file asynchronously.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="models"></param>
        public async Task SaveAsync(StreamWriter streamWriter, IEnumerable<T> models, CancellationToken cancellationToken = default(CancellationToken))
        {
            var index = 0;

            if (Options.HasHeaderRecord)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var headers = GetHeaderFromOptions(index++);
                await streamWriter.WriteLineAsync(headers).ConfigureAwait(false);
            }

            foreach (var model in models)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var line = this.GetLineFromModel(index++, model);
                await streamWriter.WriteLineAsync(line).ConfigureAwait(false);
            }
            await streamWriter.FlushAsync().ConfigureAwait(false);
        }

        /// <summary>
        /// Writes a list of objects to a csv file asynchronously.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="models"></param>
        public Task SaveAsync(Stream stream, IEnumerable<T> models, CancellationToken cancellationToken = default(CancellationToken))
        {
            using (StreamWriter streamWriter = new StreamWriter(stream))
            {
                return SaveAsync(streamWriter, models);
            }
        }

        /// <summary>
        /// get header from options
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        private string GetHeaderFromOptions(int index)
        {
            var headers = string.Join(Options.Delimiter, Options.Columns.Select(x => $"{x.ColumnName}"));
            Options.Handlers.Write.OnRowHeader(index, headers);
            return headers;
        }

        /// <summary>
        /// get line from model
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        private string GetLineFromModel(int index, T model)
        {
            Options.Handlers.Write.OnRowWriting(index, model);
            var values = Options.Columns.Select(column =>
            {
                var columnExpression = column.ColumnExpression;
                var propertyName = columnExpression.GetPropertyName();
                var property = model.GetType().GetProperty(propertyName);
                var value = property.GetValue(model);
                var stringValue = column.Converter.Convert(value, null, column.ColumnFormatProvider);
                return stringValue?.EnclosedInQuotesIfNecessary(this.Options);
            });
            var line = string.Join(Options.Delimiter, values);
            Options.Handlers.Write.OnRowWrittin(index, model, line);
            return line;
        }

        /// <summary>
        /// Get all csv text
        /// </summary>
        /// <param name="models"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public string GetAllText(IEnumerable<T> models)
        {
            var lines = GetAllLines(models);
            return string.Join(Options.NewLine, lines);
        }

        /// <summary>
        /// Get all csv lines
        /// </summary>
        /// <param name="models"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public string[] GetAllLines(IEnumerable<T> models)
        {
            var index = 0;

            var count = models.Count() + (Options.HasHeaderRecord ? 1 : 0);
            var lines = new string[count];

            if (Options.HasHeaderRecord)
            {
                lines[index] = GetHeaderFromOptions(index);
                index++;
            }

            foreach (var model in models)
            {
                lines[index] = this.GetLineFromModel(index, model);
                index++;
            }

            return lines;
        }

        /// <summary>
        /// Get all csv text
        /// </summary>
        /// <param name="models"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public async Task<string> GetAllTextAsync(IEnumerable<T> models, CancellationToken cancellationToken = default)
        {
            var lines = await GetAllLinesAsync(models, cancellationToken);
            return string.Join(Options.NewLine, lines);
        }

        /// <summary>
        /// Get all csv lines
        /// </summary>
        /// <param name="models"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public Task<string[]> GetAllLinesAsync(IEnumerable<T> models, CancellationToken cancellationToken = default)
        {
            var index = 0;

            var count = models.Count() + (Options.HasHeaderRecord ? 1 : 0);
            var lines = new string[count];

            if (Options.HasHeaderRecord)
            {
                cancellationToken.ThrowIfCancellationRequested();
                lines[index] = GetHeaderFromOptions(index);
                index++;
            }

            foreach (var model in models)
            {
                cancellationToken.ThrowIfCancellationRequested();
                lines[index] = this.GetLineFromModel(index, model);
                index++;
            }

            return Task.FromResult(lines);
        }
    }
}
