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
    using System.Threading;
    using System.Threading.Tasks;
    using TinyCsv.Extensions;

    /// <summary>
    /// TinyCsv is a simple csv reader/writer library.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public sealed class TinyCsv<T>
        where T : class, new()
    {
        /// <summary>
        /// Options
        /// </summary>
        public CsvOptions<T> Options { get; private set; }

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
        /// Reads a csv file and returns a list of objects.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public ICollection<T> Load(string path)
        {
            using (StreamReader streamReader = new StreamReader(path))
            {
                var models = Load(streamReader);
                return new List<T>(models);
            }
        }

        /// <summary>
        /// Reads a csv file and returns a list of objects.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public IEnumerable<T> Load(StreamReader streamReader)
        {
            var index = 0;

            while (!streamReader.EndOfStream)
            {
                if (index++ < Options.RowsToSkip)
                {
                    streamReader.ReadLine();
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
                    break;
                }
            }

            while (!streamReader.EndOfStream)
            {
                var line = streamReader.ReadLine();
                var skip = line.SkipRow(index++, this.Options);
                if (skip) continue;
                var model = this.GetModelFromLine(line);
                yield return model;
            }
        }

#if NET452 || NET46 || NET47 || NET48 || NETSTANDARD2_0
        /// <summary>
        /// Reads a csv file and returns a list of objects asynchronously.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public async Task<ICollection<T>> LoadAsync(string path, CancellationToken cancellationToken = default(CancellationToken))
        {
            using (StreamReader file = new StreamReader(path))
            {
                var models = await this.LoadAsync(file, cancellationToken);
                return new List<T>(models);
            }
        }

        /// <summary>
        /// Reads a csv file and returns a list of objects asynchronously.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public async Task<ICollection<T>> LoadAsync(StreamReader streamReader, CancellationToken cancellationToken = default(CancellationToken))
        {
            var models = new List<T>();
            try
            {
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
                    var model = this.GetModelFromLine(line);
                    models.Add(model);
                }
            }
            return models;
        }
#endif

#if NET5_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
        /// <summary>
        /// Reads a csv file and returns a list of objects asynchronously.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public async Task<ICollection<T>> LoadAsync(string path, CancellationToken cancellationToken = default(CancellationToken))
        {
            using (StreamReader streamReader = new StreamReader(path))
            {
                var models = await this.LoadAsync(streamReader).ToListAsync(cancellationToken);
                return models;
            }
        }

        /// <summary>
        /// Reads a csv file and returns a list of objects asynchronously.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public async IAsyncEnumerable<T> LoadAsync(StreamReader streamReader)
        {
            var index = 0;

            while (!streamReader.EndOfStream)
            {
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
                    var line = await streamReader.ReadLineAsync().ConfigureAwait(false);
                    var skip = line.SkipRow(index++, this.Options);
                    if (skip) continue;
                    break;
                }
            }

            while (!streamReader.EndOfStream)
            {
                var line = await streamReader.ReadLineAsync().ConfigureAwait(false);
                var skip = line.SkipRow(index++, this.Options);
                if (skip) continue;
                var model = this.GetModelFromLine(line);
                yield return model;
            }
        }
#endif

        /// <summary>
        /// Build T object from line
        /// </summary>
        /// <param name="line"></param>
        /// <returns></returns>
        private T GetModelFromLine(string line)
        {
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
            if (Options.HasHeaderRecord)
            {
                var headers = string.Join(Options.Delimiter, Options.Columns.Select(x => $"{x.ColumnName}"));
                streamWriter.WriteLine(headers);
            }

            foreach (var model in models)
            {
                var line = this.GetLineFromModel(model);
                streamWriter.WriteLine(line);
            }

            streamWriter.Flush();
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
                if (Options.HasHeaderRecord)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var headers = string.Join(Options.Delimiter, Options.Columns.Select(x => $"{x.ColumnName}"));
                    await file.WriteLineAsync(headers).ConfigureAwait(false);
                }

                foreach (var model in models)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var line = this.GetLineFromModel(model);
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
            try
            {
                if (Options.HasHeaderRecord)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var headers = string.Join(Options.Delimiter, Options.Columns.Select(x => $"{x.ColumnName}"));
                    await streamWriter.WriteLineAsync(headers).ConfigureAwait(false);
                }
                foreach (var model in models)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var line = this.GetLineFromModel(model);
                    await streamWriter.WriteLineAsync(line).ConfigureAwait(false);
                }
                await streamWriter.FlushAsync().ConfigureAwait(false);
            }
        }

        /// <summary>
        /// get line from model
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        private string GetLineFromModel(T model)
        {
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
            return line;
        }
    }
}
