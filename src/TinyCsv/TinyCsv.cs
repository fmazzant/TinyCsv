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
    using System.Text;
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
        public IEnumerable<T> Load(string path)
        {
            var models = new List<T>();
            using (StreamReader file = new StreamReader(path))
            {
                var index = 0;

                while (!file.EndOfStream)
                {
                    if (index++ < Options.RowsToSkip)
                    {
                        file.ReadLine();
                        continue;
                    }
                    break;
                }

                if (Options.HasHeaderRecord)
                {
                    while (!file.EndOfStream)
                    {
                        var line = file.ReadLine();
                        var skip = Options.SkipRow?.Invoke(line, index++) ?? false;
                        if (skip) continue;
                        break;
                    }
                }

                while (!file.EndOfStream)
                {
                    var line = file.ReadLine();
                    var skip = Options.SkipRow?.Invoke(line, index++) ?? false;
                    if (skip) continue;
                    var model = this.GetModelFromLine(line);
                    models.Add(model);
                }

                file.Close();
            }
            return models;
        }

        /// <summary>
        /// Reads a csv file and returns a list of objects.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public IEnumerable<T> Load(StreamReader streamReader)
        {
            var models = new List<T>();

            try
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
                        var skip = Options.SkipRow?.Invoke(line, index++) ?? false;
                        if (skip) continue;
                        break;
                    }
                }

                while (!streamReader.EndOfStream)
                {
                    var line = streamReader.ReadLine();
                    var skip = Options.SkipRow?.Invoke(line, index++) ?? false;
                    if (skip) continue;
                    var model = this.GetModelFromLine(line);
                    models.Add(model);
                }
            }
            finally
            {
                streamReader.Close();
            }
            return models;
        }

        /// <summary>
        /// Reads a csv file and returns a list of objects asynchronously.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public async Task<IEnumerable<T>> LoadAsync(string path, CancellationToken cancellationToken = new CancellationToken())
        {
            var models = new List<T>();
            using (StreamReader file = new StreamReader(path))
            {
                var index = 0;

                while (!file.EndOfStream)
                {
                    if (index++ < Options.RowsToSkip)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        await file.ReadLineAsync().ConfigureAwait(false);
                        continue;
                    }
                    break;
                }

                if (Options.HasHeaderRecord)
                {
                    while (!file.EndOfStream)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        var line = await file.ReadLineAsync().ConfigureAwait(false);
                        var skip = Options.SkipRow?.Invoke(line, index++) ?? false;
                        if (skip) continue;
                        break;
                    }
                }

                while (!file.EndOfStream)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var line = await file.ReadLineAsync().ConfigureAwait(false);
                    var skip = Options.SkipRow?.Invoke(line, index++) ?? false;
                    if (skip) continue;
                    var model = this.GetModelFromLine(line);
                    models.Add(model);
                }

                file.Close();
            }
            return models;
        }

        /// <summary>
        /// Reads a csv file and returns a list of objects asynchronously.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public async Task<IEnumerable<T>> LoadAsync(StreamReader streamReader, CancellationToken cancellationToken = new CancellationToken())
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
                        var skip = Options.SkipRow?.Invoke(line, index++) ?? false;
                        if (skip) continue;
                        break;
                    }
                }

                while (!streamReader.EndOfStream)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var line = await streamReader.ReadLineAsync().ConfigureAwait(false);
                    var skip = Options.SkipRow?.Invoke(line, index++) ?? false;
                    if (skip) continue;
                    var model = this.GetModelFromLine(line);
                    models.Add(model);
                }
            }
            finally
            {
                streamReader.Close();
            }
            return models;
        }

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
                var value = values[column.ColumnIndex].TrimData(Options.TrimData);
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
            using (StreamWriter file = new StreamWriter(path))
            {
                if (Options.HasHeaderRecord)
                {
                    var headers = string.Join(Options.Delimiter, Options.Columns.Select(x => $"{x.ColumnName}"));
                    file.WriteLine(headers);
                }

                foreach (var model in models)
                {
                    var line = this.GetLineFromModel(model);
                    file.WriteLine(line);
                }

                file.Flush();
                file.Close();
            }
        }

        /// <summary>
        /// Writes a list of objects to a csv file asynchronously.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="models"></param>
        public async Task SaveAsync(string path, IEnumerable<T> models, CancellationToken cancellationToken = new CancellationToken())
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
        public async Task SaveAsync(StreamWriter streamWriter, IEnumerable<T> models, CancellationToken cancellationToken = new CancellationToken())
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
            finally
            {
                streamWriter.Close();
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
                return stringValue?.EnclosedInDoubleQuotesIfNecessary(this.Options);
            });
            var line = string.Join(Options.Delimiter, values);
            return line;
        }
    }
}
