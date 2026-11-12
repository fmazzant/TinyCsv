
namespace Mafesoft.TinyCsv.Data
{
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Mafesoft.TinyCsv.Streams;

    /// <summary>
    /// The reader of a csv and its delimiter: the delimiter of the options or,
    /// with <see cref="CsvOptions.DetectDelimiter"/>, the delimiter detected from the first records.
    /// </summary>
    internal sealed class CsvReadSource
    {
        private const int SampleRecords = 10;
        private const int MaxSampleLength = 64 * 1024;
        private const int ChunkLength = 4096;

        private CsvReadSource(TextReader reader, string delimiter)
        {
            Reader = reader;
            Delimiter = delimiter;
        }

        public TextReader Reader { get; }

        public string Delimiter { get; }

        public static CsvReadSource Open(TextReader reader, CsvOptions options)
        {
            if (!options.DetectDelimiter)
            {
                return new CsvReadSource(reader, options.Delimiter);
            }

            var sample = new Sample();
            while (!sample.IsFull)
            {
                sample.Append(reader.Read(sample.Chunk, 0, ChunkLength));
            }
            return sample.ToSource(reader, options);
        }

        public static async Task<CsvReadSource> OpenAsync(TextReader reader, CsvOptions options, CancellationToken cancellationToken)
        {
            if (!options.DetectDelimiter)
            {
                return new CsvReadSource(reader, options.Delimiter);
            }

            var sample = new Sample();
            while (!sample.IsFull)
            {
                cancellationToken.ThrowIfCancellationRequested();
                sample.Append(await reader.ReadAsync(sample.Chunk, 0, ChunkLength).ConfigureAwait(false));
            }
            return sample.ToSource(reader, options);
        }

        /// <summary>
        /// The first chars of the reader: at least the first records, or the whole text when it is short
        /// </summary>
        private sealed class Sample
        {
            private readonly StringBuilder text = new StringBuilder();
            private bool complete;
            private int newLines;

            public char[] Chunk { get; } = new char[ChunkLength];

            public bool IsFull => complete || text.Length >= MaxSampleLength || newLines > SampleRecords + 1;

            public void Append(int read)
            {
                complete = read == 0;
                for (int i = 0; i < read; i++)
                {
                    if (Chunk[i] == '\n')
                    {
                        newLines++;
                    }
                }
                text.Append(Chunk, 0, read);
            }

            public CsvReadSource ToSource(TextReader reader, CsvOptions options)
            {
                var sample = text.ToString();
                var delimiter = Detect(sample, complete, options) ?? options.Delimiter;
                return new CsvReadSource(new PrefixedTextReader(sample, reader), delimiter);
            }
        }

        /// <summary>
        /// The candidate found the same number of times in every record wins; between them, the one found more times.
        /// When no candidate is consistent, the candidate found more times in the first record.
        /// </summary>
        internal static string? Detect(string sample, bool complete, CsvOptions options)
        {
            var records = SplitRecords(sample, complete, options);
            if (records.Count == 0 || options.DelimiterCandidates == null)
            {
                return null;
            }

            string? best = null;
            var bestConsistent = false;
            var bestCount = 0;
            foreach (var candidate in options.DelimiterCandidates)
            {
                if (string.IsNullOrEmpty(candidate))
                {
                    continue;
                }

                var count = Count(records[0], candidate, options);
                if (count == 0)
                {
                    continue;
                }

                var consistent = true;
                for (int i = 1; i < records.Count && consistent; i++)
                {
                    consistent = Count(records[i], candidate, options) == count;
                }

                if ((consistent && !bestConsistent) || (consistent == bestConsistent && count > bestCount))
                {
                    best = candidate;
                    bestConsistent = consistent;
                    bestCount = count;
                }
            }
            return best;
        }

        /// <summary>
        /// The records of the sample, without the rows to skip, the comments and the empty rows.
        /// A new line in a quoted value does not end the record.
        /// </summary>
        private static List<string> SplitRecords(string sample, bool complete, CsvOptions options)
        {
            var records = new List<string>();
            var quoting = options.AllowRowEnclosedInDoubleQuotesValues;
            var inQuotes = false;
            var start = 0;
            var index = 0;
            for (int i = 0; i <= sample.Length && records.Count < SampleRecords; i++)
            {
                var end = i == sample.Length;
                if (!end && quoting && sample[i] == options.DoubleQuotes)
                {
                    inQuotes = !inQuotes;
                }
                if (end || (!inQuotes && sample[i] == '\n'))
                {
                    // the last record of an incomplete sample may be truncated
                    if (end && !complete && records.Count > 0)
                    {
                        break;
                    }
                    var record = sample.Substring(start, i - start).TrimEnd('\r');
                    if (index++ >= options.RowsToSkip && !IsToSkip(record, options))
                    {
                        records.Add(record);
                    }
                    start = i + 1;
                }
            }
            return records;
        }

        private static bool IsToSkip(string record, CsvOptions options)
        {
            return record.Trim().Length == 0 || (options.AllowComment && record[0] == options.Comment);
        }

        private static int Count(string record, string candidate, CsvOptions options)
        {
            var count = 0;
            var inQuotes = false;
            var quoting = options.AllowRowEnclosedInDoubleQuotesValues;
            for (int i = 0; i < record.Length; i++)
            {
                if (quoting && record[i] == options.DoubleQuotes)
                {
                    inQuotes = !inQuotes;
                }
                else if (!inQuotes && string.CompareOrdinal(record, i, candidate, 0, candidate.Length) == 0)
                {
                    count++;
                    i += candidate.Length - 1;
                }
            }
            return count;
        }
    }
}
