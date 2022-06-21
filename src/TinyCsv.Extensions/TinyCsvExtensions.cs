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
    using Microsoft.Extensions.DependencyInjection;
    using System;

    /// <summary>
    /// TinyCsv Extensions
    /// </summary>
    public static class TinyCsvExtensions
    {
        /// <summary>
        /// Add TinyCsv to service
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="services"></param>
        /// <param name="serviceName"></param>
        /// <param name="options"></param>
        public static void AddTinyCsv<T>(this IServiceCollection services, string serviceName, Action<CsvOptions<T>> options)
            where T : class, new()
        {
            var tinyCsvFactory = services.GetTinyCsvFactory();
            tinyCsvFactory.Add(serviceName, new TinyCsv<T>(options));
        }

        /// <summary>
        /// Get TinyCsvFactory
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        static ITinyCsvFactory GetTinyCsvFactory(this IServiceCollection services)
        {
            var tinyCsvFactory = services.BuildServiceProvider().GetService<ITinyCsvFactory>();
            if (tinyCsvFactory == null)
            {
                services.AddSingleton<ITinyCsvFactory, TinyCsvFactory>();
                tinyCsvFactory = services.BuildServiceProvider().GetService<ITinyCsvFactory>();
            }
            return tinyCsvFactory;
        }
    }
}