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
    using TinyCsv.Extentsions;

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
                if (Options.HasHeaderRecord)
                {
                    file.ReadLine();
                }

                while (!file.EndOfStream)
                {
                    var line = file.ReadLine();
                    var values = line.Split(new string[] { Options.Delimiter }, StringSplitOptions.None);
                    var model = new T();
                    foreach (var column in Options.Columns)
                    {
                        var value = values[column.ColumnIndex].Trim('"', '\'');
                        var columnExpression = column.ColumnExpression;
                        var propertyName = columnExpression.GetPropertyName();
                        var property = typeof(T).GetProperty(propertyName);
                        var typedValue = Convert.ChangeType(value, column.ColumnType, column.ColumnFormatProvider);
                        property.SetValue(model, typedValue);
                    }
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

            if (Options.HasHeaderRecord)
            {
                streamReader.ReadLine();
            }

            while (!streamReader.EndOfStream)
            {
                var line = streamReader.ReadLine();
                var values = line.Split(new string[] { Options.Delimiter }, StringSplitOptions.None);
                var model = new T();
                foreach (var column in Options.Columns)
                {
                    var value = values[column.ColumnIndex].Trim('"', '\'');
                    var columnExpression = column.ColumnExpression;
                    var propertyName = columnExpression.GetPropertyName();
                    var property = typeof(T).GetProperty(propertyName);
                    var typedValue = Convert.ChangeType(value, column.ColumnType, column.ColumnFormatProvider);
                    property.SetValue(model, typedValue);
                }
                models.Add(model);
            }

            streamReader.Close();

            return models;
        }

        /// <summary>
        /// Reads a csv file and returns a list of objects.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public async Task<ICollection<T>> LoadAsync(string path, CancellationToken cancellationToken = new CancellationToken())
        {
            var models = new List<T>();
            using (StreamReader file = new StreamReader(path))
            {
                if (Options.HasHeaderRecord)
                {
                    await file.ReadLineAsync().ConfigureAwait(false);
                }

                while (!file.EndOfStream)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        return null;
                    }

                    var line = await file.ReadLineAsync().ConfigureAwait(false);
                    var values = line.Split(new string[] { Options.Delimiter }, StringSplitOptions.None);
                    var model = new T();
                    foreach (var column in Options.Columns)
                    {
                        var value = values[column.ColumnIndex].Trim('"', '\'');
                        var columnExpression = column.ColumnExpression;
                        var propertyName = columnExpression.GetPropertyName();
                        var property = typeof(T).GetProperty(propertyName);
                        var typedValue = Convert.ChangeType(value, column.ColumnType, column.ColumnFormatProvider);
                        property.SetValue(model, typedValue);
                    }
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
        public async Task<IEnumerable<T>> LoadAsync(StreamReader streamReader, CancellationToken cancellationToken = new CancellationToken())
        {
            var models = new List<T>();

            if (Options.HasHeaderRecord)
            {
                await streamReader.ReadLineAsync().ConfigureAwait(false);
            }

            while (!streamReader.EndOfStream)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    return null;
                }

                var line = await streamReader.ReadLineAsync().ConfigureAwait(false);
                var values = line.Split(new string[] { Options.Delimiter }, StringSplitOptions.None);
                var model = new T();
                foreach (var column in Options.Columns)
                {
                    var value = values[column.ColumnIndex].Trim('"', '\'');
                    var columnExpression = column.ColumnExpression;
                    var propertyName = columnExpression.GetPropertyName();
                    var property = typeof(T).GetProperty(propertyName);
                    var typedValue = Convert.ChangeType(value, column.ColumnType, column.ColumnFormatProvider);
                    property.SetValue(model, typedValue);
                }
                models.Add(model);
            }

            streamReader.Close();

            return models;
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
                    var values = Options.Columns.Select(column =>
                    {
                        var columnExpression = column.ColumnExpression;
                        var propertyName = columnExpression.GetPropertyName();
                        var property = model.GetType().GetProperty(propertyName);
                        var value = property.GetValue(model);
                        var stringValue = string.Format(column.ColumnFormatProvider, "{0}", value);
                        return stringValue;
                    });
                    var line = string.Join(Options.Delimiter, values);
                    file.WriteLine(line);
                }

                file.Flush();
                file.Close();
            }
        }

        /// <summary>
        /// Writes a list of objects to a csv file.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="models"></param>
        public async Task SaveAsync(string path, IEnumerable<T> models, CancellationToken cancellationToken = new CancellationToken())
        {
            using (StreamWriter file = new StreamWriter(path))
            {
                if (Options.HasHeaderRecord)
                {
                    var headers = string.Join(Options.Delimiter, Options.Columns.Select(x => $"{x.ColumnName}"));
                    await file.WriteLineAsync(headers).ConfigureAwait(false);
                }

                foreach (var model in models)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        return;
                    }

                    var values = Options.Columns.Select(column =>
                    {
                        var columnExpression = column.ColumnExpression;
                        var propertyName = columnExpression.GetPropertyName();
                        var property = model.GetType().GetProperty(propertyName);
                        var value = property.GetValue(model);
                        var stringValue = string.Format(column.ColumnFormatProvider, "{0}", value);
                        return stringValue;
                    });
                    var line = string.Join(Options.Delimiter, values);
                    await file.WriteLineAsync(line).ConfigureAwait(false);
                }

                await file.FlushAsync().ConfigureAwait(false);
                file.Close();
            }
        }

        /// <summary>
        /// Writes a list of objects to a csv file.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="models"></param>
        public async Task SaveAsync(StreamWriter streamWriter, IEnumerable<T> models, CancellationToken cancellationToken = new CancellationToken())
        {
            if (Options.HasHeaderRecord)
            {
                var headers = string.Join(Options.Delimiter, Options.Columns.Select(x => $"{x.ColumnName}"));
                await streamWriter.WriteLineAsync(headers).ConfigureAwait(false);
            }

            foreach (var model in models)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    return;
                }

                var values = Options.Columns.Select(column =>
                {
                    var columnExpression = column.ColumnExpression;
                    var propertyName = columnExpression.GetPropertyName();
                    var property = model.GetType().GetProperty(propertyName);
                    var value = property.GetValue(model);
                    var stringValue = string.Format(column.ColumnFormatProvider, "{0}", value);
                    return stringValue;
                });
                var line = string.Join(Options.Delimiter, values);
                await streamWriter.WriteLineAsync(line).ConfigureAwait(false);
            }

            await streamWriter.FlushAsync().ConfigureAwait(false);
            streamWriter.Close();
        }
    }
}
