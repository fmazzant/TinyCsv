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

namespace TinyCsv.Extensions
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// TinyCsv Factory
    /// </summary>
    [Obsolete("Use: TinyCsv.AspNetCore nuget package!", true)]
    public sealed class TinyCsvFactory : ITinyCsvFactory
    {
        private static Dictionary<string, object> _tinyCsv = new Dictionary<string, object>();

        /// <summary>
        /// TinyCsv Factory
        /// </summary>
        public TinyCsvFactory()
        {

        }

        /// <summary>
        /// Add TinyCsv with service's name
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="serviceName"></param>
        /// <param name="tinyCsv"></param>
        /// <exception cref="Exception"></exception>
        public void Add<T>(string serviceName, ITinyCsv<T> tinyCsv) where T : class, new()
        {
            if (_tinyCsv.ContainsKey(serviceName))
            {
                throw new Exception($"TinyCsv with name {serviceName} already exists");
            }
            _tinyCsv.Add(serviceName, tinyCsv);
        }

        /// <summary>
        /// Create new TinyCsv
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="options"></param>
        /// <returns></returns>
        public ITinyCsv<T> Create<T>(Action<CsvOptions<T>> options) where T : class, new()
        {
            ITinyCsv<T> tinyCsv = new TinyCsv<T>(options);
            return tinyCsv;
        }

        /// <summary>
        /// Get TinyCsv by service's name
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="serviceName"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public ITinyCsv<T> Get<T>(string serviceName) where T : class, new()
        {
            if (!_tinyCsv.ContainsKey(serviceName))
            {
                throw new Exception($"TinyCsv with name {serviceName} does not exist");
            }
            return (ITinyCsv<T>)_tinyCsv[serviceName];
        }
    }
}