
namespace TinyCsv.Streams
{
    using System;
    using System.IO;
    using System.Threading.Tasks;

    /// <summary>
    /// A reader that returns the text already read from a reader (i.e. the sample used to detect the delimiter)
    /// and then the rest of the reader.
    /// </summary>
    internal sealed class PrefixedTextReader : TextReader
    {
        private readonly string prefix;
        private readonly TextReader reader;
        private int position;

        public PrefixedTextReader(string prefix, TextReader reader)
        {
            this.prefix = prefix ?? string.Empty;
            this.reader = reader;
        }

        private int Remaining => prefix.Length - position;

        public override int Peek()
        {
            return Remaining > 0 ? prefix[position] : reader.Peek();
        }

        public override int Read()
        {
            return Remaining > 0 ? prefix[position++] : reader.Read();
        }

        public override int Read(char[] buffer, int index, int count)
        {
            if (Remaining == 0)
            {
                return reader.Read(buffer, index, count);
            }
            var length = Math.Min(count, Remaining);
            prefix.CopyTo(position, buffer, index, length);
            position += length;
            return length;
        }

        public override Task<int> ReadAsync(char[] buffer, int index, int count)
        {
            return Remaining > 0
                ? Task.FromResult(Read(buffer, index, count))
                : reader.ReadAsync(buffer, index, count);
        }
    }
}
