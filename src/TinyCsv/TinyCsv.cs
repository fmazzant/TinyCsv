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
            AddColumnsFromPropertiesIfEmpty();
        }

        /// <summary>
        /// Create a new instance of TinyCsv
        /// </summary>
        /// <param name="options"></param>
        public TinyCsv(CsvOptions<T> options)
        {
            Options = options;
            AddColumnsFromPropertiesIfEmpty();
        }

        /// <summary>
        /// Create a new instance of TinyCsv taking the options from attributes
        /// </summary>
        public TinyCsv()
        {
            Options = new CsvOptions<T>(typeof(T));
            AddColumnsFromPropertiesIfEmpty();
        }

        /// <summary>
        /// When no column is defined, the columns are the public properties of T (see CsvOptionsColumns.AddColumnsFromProperties)
        /// </summary>
        private void AddColumnsFromPropertiesIfEmpty()
        {
            if (Options.Columns.Count == 0)
            {
                Options.Columns.AddColumnsFromProperties(typeof(T));
            }
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
            var streamReader = CreateReader(path);
            return DisposeAfterEnumeration(streamReader, LoadFromStream(streamReader));
        }

        /// <summary>
        /// Enumerates the items and then disposes the owner (i.e. the reader of a file)
        /// </summary>
        /// <typeparam name="TItem"></typeparam>
        /// <param name="owner"></param>
        /// <param name="items"></param>
        /// <returns></returns>
        private static IEnumerable<TItem> DisposeAfterEnumeration<TItem>(IDisposable owner, IEnumerable<TItem> items)
        {
            using (owner)
            {
                foreach (var item in items)
                {
                    yield return item;
                }
            }
        }

        /// <summary>
        /// The encoding used to read. A byte order mark, if present, takes precedence.
        /// </summary>
        private Encoding ReaderEncoding => Options.TextEncoding ?? Encoding.UTF8;

        /// <summary>
        /// The encoding used to write. The default UTF-8 is written without the byte order mark, as StreamWriter does.
        /// </summary>
        private Encoding WriterEncoding => Options.TextEncoding == null || ReferenceEquals(Options.TextEncoding, Encoding.UTF8)
            ? new UTF8Encoding(false)
            : Options.TextEncoding;

        private StreamReader CreateReader(string path) => new StreamReader(path, ReaderEncoding, true);

        private StreamReader CreateReader(Stream stream) => new StreamReader(stream, ReaderEncoding, true);

        /// <summary>
        /// The reader of a text. With UTF-8 the text is read as it is, because encoding and decoding it
        /// returns the same text, apart from a leading byte order mark that is removed.
        /// </summary>
        /// <param name="text"></param>
        /// <param name="encoding"></param>
        /// <returns></returns>
        private TextReader CreateReader(string text, Encoding encoding)
        {
            var textEncoding = encoding ?? ReaderEncoding;
            if (textEncoding is UTF8Encoding && text != null)
            {
                return new StringReader(text.Length > 0 && text[0] == '﻿' ? text.Substring(1) : text);
            }
            return new StreamReader(new TextMemoryStream(text, textEncoding), textEncoding, true);
        }

        private StreamWriter CreateWriter(string path) => new StreamWriter(path, false, WriterEncoding);

        private StreamWriter CreateWriter(Stream stream) => new StreamWriter(stream, WriterEncoding);

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
            return LoadFromReader(streamReader);
        }

        /// <summary>
        /// Reads the models from a reader
        /// </summary>
        /// <param name="textReader"></param>
        /// <returns></returns>
        private IEnumerable<T> LoadFromReader(TextReader textReader)
        {
            var reader = new CsvModelReader<T>(Options, textReader);
            while (reader.Read())
            {
                if (reader.TryReadModel(out var model))
                {
                    yield return model;
                    reader.OnModelRead(model);
                }
            }
            reader.OnCompleted();
        }

        /// <summary>
        /// Reads a csv file and returns a list of objects.
        /// </summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        public IEnumerable<T> LoadFromStream(Stream stream)
        {
            return LoadFromStream(CreateReader(stream));
        }

        /// <summary>
        /// Reads a csv from text and returns a list of objects.
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public IEnumerable<T> LoadFromText(string text, Encoding encoding = null)
        {
            return LoadFromReader(CreateReader(text, encoding));
        }

        /// <summary>
        /// Writes a list of objects to a csv file.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="models"></param>
        public void Save(string path, IEnumerable<T> models)
        {
            using (var streamWriter = CreateWriter(path))
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
            var builder = new CsvLineBuilder<T>(options);
            var handlers = options.Handlers.Write;
            foreach (var model in models)
            {
                handlers.OnRowWriting(index, model);
                builder.Build(model);
                if (handlers.Enabled)
                {
                    var line = builder.ToString();
                    writer.WriteLine(line);
                    handlers.OnRowWritten(index, model, line);
                }
                else
                {
                    // the line is written from the buffer, without creating the string
                    builder.WriteTo(streamWriter);
                    streamWriter.WriteLine();
                }
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
            var streamWriter = CreateWriter(stream);
            Save(streamWriter, models);
        }

        /// <summary>
        /// Writes a list of objects to a csv file asynchronously.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="models"></param>
        public async Task SaveAsync(string path, IEnumerable<T> models, CancellationToken cancellationToken = default)
        {
            using (var file = CreateWriter(path))
            {
                await SaveAsync(file, models, cancellationToken).ConfigureAwait(false);
            }
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

            var builder = new CsvLineBuilder<T>(options);
            var handlers = options.Handlers.Write;
            foreach (var model in models)
            {
                cancellationToken.ThrowIfCancellationRequested();
                handlers.OnRowWriting(index, model);
                builder.Build(model);
                if (handlers.Enabled)
                {
                    var line = builder.ToString();
                    await writer.WriteLineAsync(line).ConfigureAwait(false);
                    handlers.OnRowWritten(index, model, line);
                }
                else
                {
                    // the line is written from the buffer, without creating the string
                    await builder.WriteToAsync(streamWriter).ConfigureAwait(false);
                    await streamWriter.WriteLineAsync().ConfigureAwait(false);
                }
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
            var streamWriter = CreateWriter(stream);
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

            // enumerated once: a lazy sequence (i.e. LoadFromText) cannot be enumerated twice
            models = models as ICollection<T> ?? models.ToList();
            var count = models.Count() + (Options.HasHeaderRecord ? 1 : 0);
            var lines = new string[count];

            if (Options.HasHeaderRecord)
            {
                lines[index] = Options.AsColumnsHeaderLine();
                index++;
            }

            var builder = new CsvLineBuilder<T>(Options);
            foreach (var model in models)
            {
                builder.Build(model);
                lines[index] = builder.ToString();
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

            // enumerated once: a lazy sequence (i.e. LoadFromText) cannot be enumerated twice
            models = models as ICollection<T> ?? models.ToList();
            var count = models.Count() + (Options.HasHeaderRecord ? 1 : 0);
            var lines = new string[count];

            if (Options.HasHeaderRecord)
            {
                cancellationToken.ThrowIfCancellationRequested();
                lines[index] = Options.AsColumnsHeaderLine();
                index++;
            }

            var builder = new CsvLineBuilder<T>(Options);
            foreach (var model in models)
            {
                cancellationToken.ThrowIfCancellationRequested();
                builder.Build(model);
                lines[index] = builder.ToString();
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
            var streamReader = CreateReader(path);
            return DisposeAfterEnumeration(streamReader, GetAllLinesFromStream(streamReader));
        }

        /// <summary>
        /// Get all lines
        /// </summary>
        /// <param name="streamReader"></param>
        /// <returns></returns>
        public IEnumerable<string> GetAllLinesFromStream(StreamReader streamReader)
        {
            return GetAllLinesFromReader(streamReader);
        }

        private IEnumerable<string> GetAllLinesFromReader(TextReader textReader)
        {
            var dataReader = new TinyCsvDataReader<T>(Options, textReader);
            return dataReader.ReadLines();
        }

        /// <summary>
        /// Get all lines
        /// </summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        public IEnumerable<string> GetAllLinesFromStream(Stream stream)
        {
            var streamReader = CreateReader(stream);
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
            return GetAllLinesFromReader(CreateReader(text, encoding));
        }

        /// <summary>
        /// Get all lines and fields
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public IEnumerable<string[]> GetAllLinesAndFieldsFromFile(string path)
        {
            var streamReader = CreateReader(path);
            return DisposeAfterEnumeration(streamReader, GetAllLinesAndFieldsFromStream(streamReader));
        }

        /// <summary>
        /// Get all lines and fields
        /// </summary>
        /// <param name="streamReader"></param>
        /// <returns></returns>
        public IEnumerable<string[]> GetAllLinesAndFieldsFromStream(StreamReader streamReader)
        {
            return GetAllLinesAndFieldsFromReader(streamReader);
        }

        private IEnumerable<string[]> GetAllLinesAndFieldsFromReader(TextReader textReader)
        {
            var dataReader = new TinyCsvDataReader<T>(Options, textReader);
            return dataReader.ReadLinesAndFields();
        }

        /// <summary>
        /// Get all lines and fields
        /// </summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        public IEnumerable<string[]> GetAllLinesAndFieldsFromStream(Stream stream)
        {
            var streamReader = CreateReader(stream);
            return GetAllLinesAndFieldsFromStream(streamReader);
        }

        /// <summary>
        /// Get all lines and fields
        /// </summary>
        /// <param name="text"></param>
        /// <param name="encoding"></param>
        /// <returns></returns>
        public IEnumerable<string[]> GetAllLinesAndFieldsFromText(string text, Encoding encoding = null)
        {
            return GetAllLinesAndFieldsFromReader(CreateReader(text, encoding));
        }

        /// <summary>
        /// Get all lines and fields
        /// </summary>
        /// <param name="text"></param>
        /// <param name="encoding"></param>
        /// <returns></returns>
        [Obsolete("Use GetAllLinesAndFieldsFromText instead. GetAlGetAllLinesAndFieldslLinesFromText will be removed in 4.0.")]
        public IEnumerable<string[]> GetAlGetAllLinesAndFieldslLinesFromText(string text, Encoding encoding = null)
        {
            return GetAllLinesAndFieldsFromText(text, encoding);
        }
    }
}
