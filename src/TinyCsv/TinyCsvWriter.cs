
namespace TinyCsv
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;
    using TinyCsv.Data;

    /// <summary>
    /// Writes the records one at a time, without keeping them in memory: for very large files.
    /// The header, when <see cref="CsvOptions.HasHeaderRecord"/> is true, is written before the first record.
    /// Create it with <see cref="TinyCsv{T}.CreateWriter(string)"/> and dispose it at the end.
    /// </summary>
    /// <typeparam name="T">the model</typeparam>
    public sealed class TinyCsvWriter<T> : IDisposable
#if NET5_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
        , IAsyncDisposable
#endif
        where T : class, new()
    {
        private readonly CsvOptions<T> options;
        private readonly TextWriter writer;
        private readonly bool leaveOpen;
        private readonly CsvLineBuilder<T> builder;
        private bool started;
        private bool completed;

        internal TinyCsvWriter(CsvOptions<T> options, TextWriter writer, bool leaveOpen)
        {
            this.options = options;
            this.writer = writer;
            this.leaveOpen = leaveOpen;
            builder = new CsvLineBuilder<T>(options);
            options.Handlers.OnStart();
        }

        /// <summary>
        /// The rows written: the header and the records
        /// </summary>
        public int Count { get; private set; }

        /// <summary>
        /// Writes a record
        /// </summary>
        /// <param name="model"></param>
        public void WriteRecord(T model)
        {
            WriteHeaderIfNotStarted();

            var handlers = options.Handlers.Write;
            handlers.OnRowWriting(Count, model);
            builder.Build(model);
            if (handlers.Enabled)
            {
                var line = builder.ToString();
                writer.WriteLine(line);
                handlers.OnRowWritten(Count, model, line);
            }
            else
            {
                // the line is written from the buffer, without creating the string
                builder.WriteTo(writer);
                writer.WriteLine();
            }
            Count++;
        }

        /// <summary>
        /// Writes the records
        /// </summary>
        /// <param name="models"></param>
        public void WriteRecords(IEnumerable<T> models)
        {
            foreach (var model in models)
            {
                WriteRecord(model);
            }
        }

        /// <summary>
        /// Writes a record
        /// </summary>
        /// <param name="model"></param>
        /// <param name="cancellationToken"></param>
        public async Task WriteRecordAsync(T model, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await WriteHeaderIfNotStartedAsync().ConfigureAwait(false);

            var handlers = options.Handlers.Write;
            handlers.OnRowWriting(Count, model);
            builder.Build(model);
            if (handlers.Enabled)
            {
                var line = builder.ToString();
                await writer.WriteLineAsync(line).ConfigureAwait(false);
                handlers.OnRowWritten(Count, model, line);
            }
            else
            {
                await builder.WriteToAsync(writer).ConfigureAwait(false);
                await writer.WriteLineAsync().ConfigureAwait(false);
            }
            Count++;
        }

        /// <summary>
        /// Writes the records
        /// </summary>
        /// <param name="models"></param>
        /// <param name="cancellationToken"></param>
        public async Task WriteRecordsAsync(IEnumerable<T> models, CancellationToken cancellationToken = default)
        {
            foreach (var model in models)
            {
                await WriteRecordAsync(model, cancellationToken).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Writes the buffered rows
        /// </summary>
        public void Flush()
        {
            writer.Flush();
        }

        /// <summary>
        /// Writes the buffered rows
        /// </summary>
        public Task FlushAsync()
        {
            return writer.FlushAsync();
        }

        private void WriteHeaderIfNotStarted()
        {
            var header = started ? null : Start();
            if (header != null)
            {
                writer.WriteLine(header);
            }
        }

        private async Task WriteHeaderIfNotStartedAsync()
        {
            var header = started ? null : Start();
            if (header != null)
            {
                await writer.WriteLineAsync(header).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// The header to write, if any, before the first record
        /// </summary>
        private string Start()
        {
            started = true;
            if (!options.HasHeaderRecord)
            {
                return null;
            }
            var header = options.AsColumnsHeaderLine();
            options.Handlers.Write.OnRowHeader(Count, header);
            Count++;
            return header;
        }

        /// <summary>
        /// Writes the header, if no record is written, raises Completed and flushes the rows
        /// </summary>
        internal void Complete()
        {
            if (completed)
            {
                return;
            }
            WriteHeaderIfNotStarted();
            completed = true;
            options.Handlers.OnCompleted(Count);
            writer.Flush();
        }

        /// <summary>
        /// Writes the header, if no record is written, raises Completed and flushes the rows
        /// </summary>
        internal async Task CompleteAsync()
        {
            if (completed)
            {
                return;
            }
            await WriteHeaderIfNotStartedAsync().ConfigureAwait(false);
            completed = true;
            options.Handlers.OnCompleted(Count);
            await writer.FlushAsync().ConfigureAwait(false);
        }

        /// <summary>
        /// Completes the file and closes the writer, when it is created by TinyCsv
        /// </summary>
        public void Dispose()
        {
            Complete();
            if (!leaveOpen)
            {
                writer.Dispose();
            }
        }

#if NET5_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
        /// <summary>
        /// Completes the file and closes the writer, when it is created by TinyCsv
        /// </summary>
        public async ValueTask DisposeAsync()
        {
            await CompleteAsync().ConfigureAwait(false);
            if (!leaveOpen)
            {
                await writer.DisposeAsync().ConfigureAwait(false);
            }
        }
#endif
    }
}
