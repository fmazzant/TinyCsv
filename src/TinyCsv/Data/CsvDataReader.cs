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
    using System.Collections.Generic;
    using System.IO;

    /// <summary>
    /// CsvDataReader
    /// </summary>
    public class TinyCsvDataReader<T> : ICsvDataReader<T>
    {
        private CsvOptions<T> options;
        private readonly StreamReader reader;

        public TinyCsvDataReader(CsvOptions<T> options, StreamReader reader)
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


        public IEnumerable<string[]> GetFields(string line)
        {
            var fields = line.Split(options.Delimiter[0]);



            yield return fields;
        }
    }
}