namespace Mafesoft.TinyCsv
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Reflection;
    using Mafesoft.TinyCsv.Data;

    /// <summary>
    /// The data reader over the models of a CSV file
    /// </summary>
    public sealed partial class TinyCsv<T>
    {
        /// <summary>
        /// Creates a data reader over the models of a file, read one at a time (i.e. for <c>SqlBulkCopy.WriteToServer</c>):
        /// the columns are the columns of the options, the values are the properties of the models, converted by the
        /// column converters (a null value is DBNull). Dispose it at the end.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public TinyCsvDbDataReader CreateDataReader(string path)
        {
            return CreateDataReader(CreateReader(path));
        }

        /// <summary>
        /// Creates a data reader over the models of a stream, read one at a time. Disposing the data reader disposes the stream.
        /// </summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        public TinyCsvDbDataReader CreateDataReader(Stream stream)
        {
            return CreateDataReader(CreateReader(stream));
        }

        /// <summary>
        /// Creates a data reader over the models of a text reader, read one at a time. Disposing the data reader disposes the text reader.
        /// </summary>
        /// <param name="textReader"></param>
        /// <returns></returns>
        public TinyCsvDbDataReader CreateDataReader(TextReader textReader)
        {
            if (textReader == null)
            {
                throw new ArgumentNullException(nameof(textReader));
            }
            return new TinyCsvDbDataReader(new ModelSource(this, textReader), Options.HeaderNameComparer);
        }

        /// <summary>
        /// The models of a text reader, for the data reader
        /// </summary>
        private sealed class ModelSource : CsvEnumeratorSource<T>
        {
            private readonly TextReader textReader;
            private readonly Func<T, object?>[] getters;

            public ModelSource(TinyCsv<T> csv, TextReader textReader)
                : base(csv.LoadFromReader(textReader))
            {
                this.textReader = textReader;
                var columns = csv.Options.Columns.ToArray();
                Names = columns.Select(c => c.ColumnName).ToArray();
                Types = columns.Select(c => Nullable.GetUnderlyingType(c.ColumnType) ?? c.ColumnType).ToArray();
                getters = columns.Select(c => Getter(csv.Options, c)).ToArray();
            }

            public override object? GetValue(int ordinal) => getters[ordinal](Current);

            public override void Dispose()
            {
                base.Dispose();
                textReader.Dispose();
            }

            /// <summary>
            /// The getter of the property of the column: the path of a nested property (null when an object of the path is null),
            /// or the property with the name of the column
            /// </summary>
            private static Func<T, object?> Getter(CsvOptions<T> options, CsvColumn column)
            {
                var path = column.PropertyPath;
                if (path == null)
                {
                    if (options.Properties == null || !options.Properties.TryGetValue(column.ColumnNameInternal ?? string.Empty, out var property))
                    {
                        return _ => null;
                    }
                    path = new[] { property };
                }

                // model => model.A == null ? null : model.A.B ... as object
                var model = Expression.Parameter(typeof(T), "model");
                Expression body = model;
                Expression? test = null;
                for (int i = 0; i < path.Length; i++)
                {
                    if (i > 0 && !body.Type.IsValueType)
                    {
                        var isNull = Expression.Equal(body, Expression.Constant(null, body.Type));
                        test = test == null ? isNull : Expression.OrElse(test, isNull);
                    }
                    body = Expression.Property(body, path[i]);
                }
                Expression value = Expression.Convert(body, typeof(object));
                if (test != null)
                {
                    value = Expression.Condition(test, Expression.Constant(null, typeof(object)), value);
                }
                return Expression.Lambda<Func<T, object?>>(value, model).Compile();
            }
        }
    }
}
