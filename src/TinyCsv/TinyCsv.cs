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
    using TinyCsv.Extentsions;

    /// <summary>
    /// TinyCsv is a simple csv reader/writer library.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public sealed class TinyCsv<T>
        where T : class, new()
    {
        public CsvOptions<T> Options { get; private set; }
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

        /// <summary>
        /// Writes a list of objects to a csv file.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="models"></param>
        public void Save(string path, ICollection<T> models)
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
    }
}
