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

namespace TinyCsv.Data
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;

    /// <summary>
    /// CsvDataReader
    /// </summary>
    public class TinyCsvDataReader<T> : ICsvDataReader<T>
    {
        private ITinyCsvDataReaderOptions options;
        private readonly StreamReader reader;

        public TinyCsvDataReader(ITinyCsvDataReaderOptions options, StreamReader reader)
        {
            this.options = options;
            this.reader = reader;
        }

        public IEnumerable<string> GetLines()
        {
            var line = string.Empty;
            while ((line = reader.ReadLine()) != null)
            {
                yield return line;
            }
        }

        public string[] GetFields(string line)
        {
            var result = new List<string>();
            var sb = new StringBuilder(string.Empty);
            bool inQuotes = false;

            for (int i = 0; i < line.Length; i++)
            {
                var isLast = i == line.Length - 1;
                var isBackspace = line[i] == '\\';
                var isQuotes = line[i] == '\"';
                var isDelimiter = line[i] == options.Delimiter;

                if (isQuotes)
                {
                    if (!isLast && line[i + 1] == '\"')
                    {
                        sb.Append(line[i]);
                    }
                    else
                    {
                        inQuotes = !inQuotes;
                    }
                }
                else if (isBackspace)
                {
                    sb.Append(line[i + 1]);
                    i++;
                }
                else if (isDelimiter)
                {
                    if (i < line.Length - 1)
                    {
                        if (!inQuotes)
                        {
                            result.Add(sb.ToString());
                            sb.Clear();
                        }
                        else
                        {
                            sb.Append(line[i]);
                        }
                    }
                }
                else
                {
                    sb.Append(line[i]);
                }
            }
            result.Add(sb.ToString());

            return result.ToArray();
        }

        public IEnumerable<string[]> GetLinesField()
        {
            foreach (var line in GetLines())
            {
                var fields = GetFields(line);
                yield return fields;
            }
        }
    }
}