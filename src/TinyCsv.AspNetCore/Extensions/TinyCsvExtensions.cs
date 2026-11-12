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

namespace Mafesoft.TinyCsv.AspNetCore.Extensions
{
    using System;
    using System.Linq;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.DependencyInjection.Extensions;

    /// <summary>
    /// Registers TinyCsv in a service collection.
    /// A TinyCsv&lt;T&gt; instance is thread-safe after its construction, as long as its options are not changed,
    /// so it is registered as singleton.
    /// </summary>
    public static class TinyCsvExtensions
    {
        /// <summary>
        /// Registers <see cref="ITinyCsvFactory"/> as singleton
        /// </summary>
        /// <param name="services"></param>
        /// <returns>the service collection, for the fluent registration</returns>
        public static IServiceCollection AddTinyCsv(this IServiceCollection services)
        {
            if (services == null) throw new ArgumentNullException(nameof(services));

            services.TryAddSingleton<ITinyCsvFactory>(provider => new TinyCsvFactory(provider.GetServices<TinyCsvRegistration>()));
            return services;
        }

        /// <summary>
        /// Registers <see cref="ITinyCsv{T}"/> as singleton, with the options: inject ITinyCsv&lt;T&gt;
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="services"></param>
        /// <param name="options"></param>
        /// <returns>the service collection, for the fluent registration</returns>
        public static IServiceCollection AddTinyCsv<T>(this IServiceCollection services, Action<CsvOptions<T>> options)
            where T : class, new()
        {
            services.AddTinyCsv();
            services.AddSingleton<ITinyCsv<T>>(_ => new TinyCsv<T>(options));
            return services;
        }

        /// <summary>
        /// Registers a TinyCsv with a name, unique in the service collection.
        /// It is returned by <see cref="ITinyCsvFactory.Get{T}(string)"/> and, on .NET 8 or later,
        /// it is also a keyed service: <c>[FromKeyedServices("name")] ITinyCsv&lt;T&gt;</c>.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="services"></param>
        /// <param name="name"></param>
        /// <param name="options"></param>
        /// <returns>the service collection, for the fluent registration</returns>
        /// <exception cref="ArgumentException">the name is null or empty</exception>
        /// <exception cref="InvalidOperationException">a TinyCsv with the same name is already registered</exception>
        public static IServiceCollection AddTinyCsv<T>(this IServiceCollection services, string name, Action<CsvOptions<T>> options)
            where T : class, new()
        {
            if (services == null) throw new ArgumentNullException(nameof(services));
            if (string.IsNullOrEmpty(name)) throw new ArgumentException("The name of the TinyCsv is required.", nameof(name));

            var registered = services
                .Where(service => service.ServiceType == typeof(TinyCsvRegistration))
                .Select(service => service.ImplementationInstance)
                .OfType<TinyCsvRegistration>()
                .Any(registration => registration.Name == name);
            if (registered)
            {
                throw TinyCsvFactory.DuplicateName(name);
            }

            services.AddTinyCsv();
            services.AddSingleton(new TinyCsvRegistration(name, () => new TinyCsv<T>(options)));
#if NET8_0_OR_GREATER
            // the keyed service is the instance of the factory
            services.AddKeyedSingleton<ITinyCsv<T>>(name, (provider, key) => provider.GetRequiredService<ITinyCsvFactory>().Get<T>(name));
#endif
            return services;
        }
    }
}
