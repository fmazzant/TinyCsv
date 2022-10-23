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
    using TinyCsv.Data;
    using TinyCsv.Extensions;
    using TinyCsv.Streams;

    /// <summary>
    /// TinyCsv is a simple csv reader/writer library.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public sealed partial class TinyCsv<T> : ITinyCsv<T> where T : class, new()
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
            Options = new CsvOptions<T>(typeof(T));
        }

        /// <summary>
        /// Reads a csv file and returns a list of objects.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        [Obsolete($"Use {nameof(LoadFromFile)} to load from file", false)]
        public IEnumerable<T> Load(string path)
        {
            return LoadFromFile(path);
        }

        /// <summary>
        /// Reads a csv file and returns a list of objects.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public IEnumerable<T> LoadFromFile(string path)
        {
            var streamReader = new StreamReader(path);
            return LoadFromStream(streamReader);

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
        /// Reads a csv file and returns a list of generic objects T.
        /// </summary>
        /// <param name="streamReader"></param>
        /// <returns></returns>
        public IEnumerable<T> LoadFromStream(StreamReader streamReader)
        {
            var index = 0;
            var options = this.Options;
            var dataReader = new TinyCsvDataReader<T>(options, streamReader);
            var hasHeaderRecord = options.HasHeaderRecord;

            options.Handlers.OnStart();
            var lines = dataReader.ReadLines();
            foreach (var line in lines)
            {
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
                yield return model;
                options.Handlers.Read.OnRowRead(currentIndex, model, line);
            }
            options.Handlers.OnCompleted(index);
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
            var memoryStream = new TextMemoryStream(text, encoding ?? Options.TextEncoding);
            return LoadFromStream(memoryStream);
        }

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
                var propertyName = column.ColumnName ?? columnExpression?.GetPropertyName();
                var property = Options.Properties[propertyName];
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
            var streamWriter = new StreamWriter(path);
            Save(streamWriter, models);
        }

        /// <summary>
        /// Writes a list of objects to a csv file.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="models"></param>
        public void Save(StreamWriter streamWriter, IEnumerable<T> models)
        {
            var index = 0;
            var options = this.Options;
            var writer = new CsvDataWriter<T>(options, streamWriter);

            options.Handlers.OnStart();
            if (options.HasHeaderRecord)
            {
                var headers = options.AsColumnsHeaderLine();
                options.Handlers.Write.OnRowHeader(index, headers);
                writer.WriteLine(headers);
                index++;
            }

            foreach (var model in models)
            {
                options.Handlers.Write.OnRowWriting(index, model);
                var line = model.GetLineFromGenericType(options);
                writer.WriteLine(line);
                options.Handlers.Write.OnRowWrittin(index, model, line);
                index++;
            }
            options.Handlers.OnCompleted(index);
            writer.Flush();
        }

        /// <summary>
        /// Writes a list of objects to a csv file.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="models"></param>
        public void Save(Stream stream, IEnumerable<T> models)
        {
            var streamWriter = new StreamWriter(stream);
            Save(streamWriter, models);
        }

        /// <summary>
        /// Writes a list of objects to a csv file asynchronously.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="models"></param>
        public async Task SaveAsync(string path, IEnumerable<T> models, CancellationToken cancellationToken = default)
        {
            var file = new StreamWriter(path);
            await SaveAsync(file, models, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Writes a list of objects to a csv file asynchronously.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="models"></param>
        public async Task SaveAsync(StreamWriter streamWriter, IEnumerable<T> models, CancellationToken cancellationToken = default)
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
        public Task SaveAsync(Stream stream, IEnumerable<T> models, CancellationToken cancellationToken = default)
        {
            var streamWriter = new StreamWriter(stream);
            return SaveAsync(streamWriter, models);
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
                var propertyName = column.ColumnName ?? columnExpression?.GetPropertyName();
                var property = Options.Properties[propertyName];
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

        /// <summary>
        /// Returns the index of line jumped
        /// </summary>
        /// <param name="index"></param>
        /// <param name="streamReader"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        async Task<int> GetIndexFromStreamReaderBySkipRowsAsync(int index, StreamReader streamReader, CancellationToken cancellationToken = default)
        {
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
                    var skipHeaderLine = headerLine.SkipRow(index++, this.Options);
                    if (skipHeaderLine) continue;
                    break;
                }
            }
            return index;
        }

        async Task<T> GetModelAndIndexFromStreamReaderAsync(int index, StreamReader streamReader, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var line = await streamReader.ReadLineAsync().ConfigureAwait(false);
            var skip = line.SkipRow(index++, this.Options);
            if (skip) return null;
            var model = this.GetModelFromLine(index - 1, line);
            return model;
        }
    }
}
