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
    using TinyCsv.Exceptions;
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
        [Obsolete($"Use {nameof(LoadFromStream)} to load from file", false)]
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
            var validateColumnCount = options.ValidateColumnCount;
            var columnsCount = options.Columns.Count;

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

                if (validateColumnCount && columnsCount != fields.Length)
                {
                    throw new InvalidColumnCountException($"Invalid column count {fields.Length} at {index} line, required {columnsCount} columns");
                }

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

            var options = this.Options;
            var writer = new CsvDataWriter<T>(options, streamWriter);

            options.Handlers.OnStart();
            if (options.HasHeaderRecord)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var headers = options.AsColumnsHeaderLine();
                options.Handlers.Write.OnRowHeader(index, headers);
                await writer.WriteLineAsync(headers).ConfigureAwait(false);
                index++;
            }

            foreach (var model in models)
            {
                cancellationToken.ThrowIfCancellationRequested();
                options.Handlers.Write.OnRowWriting(index, model);
                var line = model.GetLineFromGenericType(options);
                await writer.WriteLineAsync(line).ConfigureAwait(false);
                options.Handlers.Write.OnRowWrittin(index, model, line);
                index++;
            }
            options.Handlers.OnCompleted(index);
            await writer.FlushAsync().ConfigureAwait(false);
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
                lines[index] = Options.AsColumnsHeaderLine();
                index++;
            }

            foreach (var model in models)
            {
                lines[index] = model.GetLineFromGenericType(Options);
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
                lines[index] = Options.AsColumnsHeaderLine();
                index++;
            }

            foreach (var model in models)
            {
                cancellationToken.ThrowIfCancellationRequested();
                lines[index] = model.GetLineFromGenericType(Options);
                index++;
            }

            return Task.FromResult(lines);
        }

        /// <summary>
        /// Get all lines
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public IEnumerable<string> GetAllLinesFromFile(string path)
        {
            var streamReader = new StreamReader(path);
            return GetAllLinesFromStream(streamReader);
        }

        /// <summary>
        /// Get all lines
        /// </summary>
        /// <param name="streamReader"></param>
        /// <returns></returns>
        public IEnumerable<string> GetAllLinesFromStream(StreamReader streamReader)
        {
            var options = this.Options;
            var dataReader = new TinyCsvDataReader<T>(options, streamReader);
            return dataReader.ReadLines();
        }

        /// <summary>
        /// Get all lines
        /// </summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        public IEnumerable<string> GetAllLinesFromStream(Stream stream)
        {
            var streamReader = new StreamReader(stream);
            return GetAllLinesFromStream(streamReader);
        }

        /// <summary>
        /// Get all lines
        /// </summary>
        /// <param name="text"></param>
        /// <param name="encoding"></param>
        /// <returns></returns>
        public IEnumerable<string> GetAllLinesFromText(string text, Encoding encoding = null)
        {
            var options = this.Options;
            var memoryStream = new TextMemoryStream(text, encoding ?? options.TextEncoding);
            return GetAllLinesFromStream(memoryStream);
        }

        /// <summary>
        /// Get all lines and fields
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public IEnumerable<string[]> GetAllLinesAndFieldsFromFile(string path)
        {
            var streamReader = new StreamReader(path);
            return GetAllLinesAndFieldsFromStream(streamReader);
        }

        /// <summary>
        /// Get all lines and fields
        /// </summary>
        /// <param name="streamReader"></param>
        /// <returns></returns>
        public IEnumerable<string[]> GetAllLinesAndFieldsFromStream(StreamReader streamReader)
        {
            var options = this.Options;
            var dataReader = new TinyCsvDataReader<T>(options, streamReader);
            return dataReader.ReadLinesAndFields();
        }

        /// <summary>
        /// Get all lines and fields
        /// </summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        public IEnumerable<string[]> GetAllLinesAndFieldsFromStream(Stream stream)
        {
            var streamReader = new StreamReader(stream);
            return GetAllLinesAndFieldsFromStream(streamReader);
        }

        /// <summary>
        /// Get all lines and fields
        /// </summary>
        /// <param name="text"></param>
        /// <param name="encoding"></param>
        /// <returns></returns>
        public IEnumerable<string[]> GetAlGetAllLinesAndFieldslLinesFromText(string text, Encoding encoding = null)
        {
            var options = this.Options;
            var memoryStream = new TextMemoryStream(text, encoding ?? options.TextEncoding);
            return GetAllLinesAndFieldsFromStream(memoryStream);
        }
    }
}
