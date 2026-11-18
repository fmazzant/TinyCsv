namespace Mafesoft.TinyCsv
{
    using System;
    using System.IO;
    using Mafesoft.TinyCsv.Data;

    /// <summary>
    /// CreateDataReader for <see cref="ITinyCsv{T}"/> (i.e. the instance registered by AddTinyCsv): an extension,
    /// so the interface does not change in a minor version
    /// </summary>
    public static class TinyCsvDataReaderExtensions
    {
        /// <summary>
        /// Creates a data reader over the models of a file, read one at a time: see <see cref="TinyCsv{T}.CreateDataReader(string)"/>
        /// </summary>
        /// <exception cref="NotSupportedException">the instance is not a TinyCsv&lt;T&gt;</exception>
        public static TinyCsvDbDataReader CreateDataReader<T>(this ITinyCsv<T> csv, string path)
            where T : class, new()
        {
            return Get(csv).CreateDataReader(path);
        }

        /// <summary>
        /// Creates a data reader over the models of a stream, read one at a time: see <see cref="TinyCsv{T}.CreateDataReader(Stream)"/>
        /// </summary>
        /// <exception cref="NotSupportedException">the instance is not a TinyCsv&lt;T&gt;</exception>
        public static TinyCsvDbDataReader CreateDataReader<T>(this ITinyCsv<T> csv, Stream stream)
            where T : class, new()
        {
            return Get(csv).CreateDataReader(stream);
        }

        private static TinyCsv<T> Get<T>(ITinyCsv<T> csv)
            where T : class, new()
        {
            if (csv == null)
            {
                throw new ArgumentNullException(nameof(csv));
            }
            return csv as TinyCsv<T> ?? throw new NotSupportedException($"CreateDataReader requires a {nameof(TinyCsv<T>)} instance.");
        }
    }
}
