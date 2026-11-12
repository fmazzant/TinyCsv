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

namespace Mafesoft.TinyCsv.AspNetCore
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;

    /// <summary>
    /// TinyCsv Factory. The instances are kept by the factory instance (there is no static state):
    /// each service provider has its own factory and its own registrations.
    /// </summary>
    public sealed class TinyCsvFactory : ITinyCsvFactory
    {
        private readonly ConcurrentDictionary<string, Lazy<object>> instances = new ConcurrentDictionary<string, Lazy<object>>(StringComparer.Ordinal);

        /// <summary>
        /// Creates a factory without registrations
        /// </summary>
        public TinyCsvFactory()
        {
        }

        /// <summary>
        /// Creates a factory with the registrations of a service collection
        /// </summary>
        /// <param name="registrations"></param>
        internal TinyCsvFactory(IEnumerable<TinyCsvRegistration> registrations)
        {
            foreach (var registration in registrations)
            {
                if (!instances.TryAdd(registration.Name, new Lazy<object>(registration.Create)))
                {
                    throw DuplicateName(registration.Name);
                }
            }
        }

        /// <inheritdoc/>
        public void Add<T>(string name, ITinyCsv<T> tinyCsv) where T : class, new()
        {
            if (name == null) throw new ArgumentNullException(nameof(name));
            if (tinyCsv == null) throw new ArgumentNullException(nameof(tinyCsv));

            if (!instances.TryAdd(name, new Lazy<object>(() => tinyCsv)))
            {
                throw DuplicateName(name);
            }
        }

        /// <inheritdoc/>
        public ITinyCsv<T> Create<T>(Action<CsvOptions<T>> options) where T : class, new()
        {
            return new TinyCsv<T>(options);
        }

        /// <inheritdoc/>
        public ITinyCsv<T> Get<T>(string name) where T : class, new()
        {
            if (name == null) throw new ArgumentNullException(nameof(name));

            if (!instances.TryGetValue(name, out var instance))
            {
                throw new KeyNotFoundException($"No TinyCsv is registered with the name '{name}'.");
            }
            if (instance.Value is ITinyCsv<T> tinyCsv)
            {
                return tinyCsv;
            }
            throw new InvalidOperationException($"The TinyCsv registered with the name '{name}' is {instance.Value.GetType()}, not ITinyCsv<{typeof(T).FullName}>.");
        }

        /// <inheritdoc/>
        public bool TryGet<T>(string name, [NotNullWhen(true)] out ITinyCsv<T>? tinyCsv) where T : class, new()
        {
            tinyCsv = null;
            if (name != null && instances.TryGetValue(name, out var instance))
            {
                tinyCsv = instance.Value as ITinyCsv<T>;
            }
            return tinyCsv != null;
        }

        internal static InvalidOperationException DuplicateName(string name)
        {
            return new InvalidOperationException($"A TinyCsv with the name '{name}' is already registered.");
        }
    }

    /// <summary>
    /// A TinyCsv registered by name in a service collection:
    /// the factory of the service provider creates it the first time it is requested
    /// </summary>
    internal sealed class TinyCsvRegistration
    {
        public TinyCsvRegistration(string name, Func<object> create)
        {
            Name = name;
            Create = create;
        }

        public string Name { get; }

        public Func<object> Create { get; }
    }
}
