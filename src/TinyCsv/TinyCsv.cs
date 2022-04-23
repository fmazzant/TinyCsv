using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using TinyCsv.Extentsions;

namespace TinyCsv
{
    public sealed class TinyCsv<T>
        where T : class, new()
    {
        public CsvOptions<T> Options { get; private set; }
        public TinyCsv(Action<CsvOptions<T>> options)
        {
            Options = new CsvOptions<T>();
            options(Options);
        }

        public ICollection<T> Load(string path)
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

        public void Save(string path, ICollection<T> models)
        {
            using (StreamWriter file = new StreamWriter(path))
            {
                var headers = string.Join(Options.Delimiter, Options.Columns.Select(x => $"\"{x.ColumnName}\""));
                file.WriteLine(headers);

                foreach (var model in models)
                {
                    var values = Options.Columns.Select(column =>
                    {
                        var columnExpression = column.ColumnExpression;
                        var propertyName = columnExpression.GetPropertyName();
                        var property = model.GetType().GetProperty(propertyName);
                        var value = property.GetValue(model);
                        var typedValue = string.Format(column.ColumnFormatProvider, "{0}", value);
                        var stringValue = $"\"{typedValue}\"";
                        return stringValue;
                    });
                    var line = string.Join(Options.Delimiter, values);
                    file.WriteLine(line);
                }
                file.Flush();
                file.Close();
            }
        }
    }
}
