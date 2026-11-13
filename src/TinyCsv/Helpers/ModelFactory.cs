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


namespace Mafesoft.TinyCsv.Helpers
{
    using System;
    using System.Linq.Expressions;

    /// <summary>
    /// Creates the models. In generic code shared by the reference types, new T() calls Activator.CreateInstance,
    /// that is slow: where the code can be compiled, the constructor is called by a compiled delegate.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal static class ModelFactory<T> where T : class, new()
    {
        /// <summary>
        /// Creates a model
        /// </summary>
        public static readonly Func<T> Create = CreateFactory();

        private static Func<T> CreateFactory()
        {
#if NETCOREAPP3_0_OR_GREATER || NETSTANDARD2_1
            if (!System.Runtime.CompilerServices.RuntimeFeature.IsDynamicCodeCompiled)
            {
                // i.e. Native AOT: an interpreted expression would be slower than Activator
                return () => new T();
            }
#endif
            try
            {
                return Expression.Lambda<Func<T>>(Expression.New(typeof(T))).Compile();
            }
            catch (Exception ex) when (ex is NotSupportedException || ex is MemberAccessException || ex is System.Security.SecurityException)
            {
                // the expression cannot be compiled on this platform (PlatformNotSupportedException is a NotSupportedException)
                // or the constructor is not accessible to the compiled code (i.e. partial trust): new T() works anyway
                return () => new T();
            }
        }
    }
}
