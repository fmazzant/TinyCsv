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

#if NET8_0_OR_GREATER
namespace Mafesoft.TinyCsv.AspNetCore
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Mafesoft.TinyCsv.Exceptions;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc.Formatters;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Net.Http.Headers;

    /// <summary>
    /// The TinyCsv instance of a model in the MVC formatters: the ITinyCsv&lt;T&gt; registered with AddTinyCsv,
    /// otherwise a TinyCsv&lt;T&gt; with the attributes of the model, or its public properties as columns
    /// </summary>
    internal static class TinyCsvFormatterModels
    {
        private static readonly ConcurrentDictionary<Type, object> Defaults = new ConcurrentDictionary<Type, object>();

        /// <summary>
        /// The model of a collection type: IEnumerable&lt;T&gt; or IAsyncEnumerable&lt;T&gt; of a class with a parameterless constructor,
        /// or null. A string is not a collection of models.
        /// </summary>
        public static Type? GetModelType(Type? type, bool asyncEnumerable)
        {
            if (type == null || type == typeof(string))
            {
                return null;
            }
            var interfaces = type.IsInterface ? type.GetInterfaces().Append(type) : type.GetInterfaces();
            var definitions = asyncEnumerable
                ? new[] { typeof(IEnumerable<>), typeof(IAsyncEnumerable<>) }
                : new[] { typeof(IEnumerable<>) };
            foreach (var definition in definitions)
            {
                var model = interfaces
                    .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == definition)
                    .Select(i => i.GetGenericArguments()[0])
                    .FirstOrDefault();
                if (model != null)
                {
                    return IsModel(model) ? model : null;
                }
            }
            return null;
        }

        private static bool IsModel(Type type)
        {
            return type.IsClass && !type.IsAbstract && type != typeof(string) && type.GetConstructor(Type.EmptyTypes) != null;
        }

        public static ITinyCsv<T> Get<T>(HttpContext httpContext)
            where T : class, new()
        {
            return httpContext.RequestServices?.GetService<ITinyCsv<T>>()
                ?? (ITinyCsv<T>)Defaults.GetOrAdd(typeof(T), _ => new TinyCsv<T>());
        }

    }

    /// <summary>
    /// Writes the collections of models (IEnumerable&lt;T&gt;, IAsyncEnumerable&lt;T&gt;) as <c>text/csv</c>, one record at a time.
    /// Add it with <c>AddControllers().AddTinyCsvFormatters()</c>.
    /// </summary>
    public sealed class TinyCsvOutputFormatter : TextOutputFormatter
    {
        private static readonly MethodInfo WriteMethod = typeof(TinyCsvOutputFormatter).GetMethod(nameof(WriteAsync), BindingFlags.NonPublic | BindingFlags.Static)!;

        /// <summary>
        /// Creates the formatter for <c>text/csv</c> in UTF-8
        /// </summary>
        public TinyCsvOutputFormatter()
        {
            SupportedMediaTypes.Add(MediaTypeHeaderValue.Parse("text/csv"));
            SupportedEncodings.Add(new UTF8Encoding(false));
        }

        /// <summary>
        /// True for IEnumerable&lt;T&gt; and IAsyncEnumerable&lt;T&gt; of a class with a parameterless constructor
        /// </summary>
        protected override bool CanWriteType(Type? type)
        {
            return TinyCsvFormatterModels.GetModelType(type, true) != null;
        }

        /// <summary>
        /// Writes the models with the ITinyCsv&lt;T&gt; registered with AddTinyCsv, or with the columns of the model
        /// </summary>
        public override Task WriteResponseBodyAsync(OutputFormatterWriteContext context, Encoding selectedEncoding)
        {
            ArgumentNullException.ThrowIfNull(context);
            var model = TinyCsvFormatterModels.GetModelType(context.Object?.GetType() ?? context.ObjectType, true)
                ?? throw new InvalidOperationException($"{context.ObjectType} is not a collection of models.");
            return (Task)WriteMethod.MakeGenericMethod(model).Invoke(null, new[] { context.HttpContext, context.Object })!;
        }

        private static Task WriteAsync<T>(HttpContext httpContext, object? value)
            where T : class, new()
        {
            var csv = TinyCsvFormatterModels.Get<T>(httpContext);
            var cancellationToken = httpContext.RequestAborted;
            var body = httpContext.Response.Body;
            return value switch
            {
                IAsyncEnumerable<T> asyncRecords => csv.SaveAsync(body, asyncRecords, cancellationToken),
                IEnumerable<T> records => csv.SaveAsync(body, records, cancellationToken),
                _ => csv.SaveAsync(body, Array.Empty<T>(), cancellationToken),
            };
        }
    }

    /// <summary>
    /// Reads a <c>text/csv</c> request body into a collection of models (List&lt;T&gt;, T[], IEnumerable&lt;T&gt;, IReadOnlyList&lt;T&gt;, ...).
    /// A field with bad data adds a model error with the row and the column (a 400 ValidationProblemDetails with [ApiController]).
    /// Add it with <c>AddControllers().AddTinyCsvFormatters()</c>.
    /// </summary>
    public sealed class TinyCsvInputFormatter : TextInputFormatter
    {
        private static readonly MethodInfo ReadMethod = typeof(TinyCsvInputFormatter).GetMethod(nameof(ReadAsync), BindingFlags.NonPublic | BindingFlags.Static)!;

        /// <summary>
        /// Creates the formatter for <c>text/csv</c>
        /// </summary>
        public TinyCsvInputFormatter()
        {
            SupportedMediaTypes.Add(MediaTypeHeaderValue.Parse("text/csv"));
            SupportedEncodings.Add(UTF8EncodingWithoutBOM);
            SupportedEncodings.Add(UTF16EncodingLittleEndian);
        }

        /// <summary>
        /// True for the collections of models that a List&lt;T&gt; or a T[] can be assigned to
        /// </summary>
        protected override bool CanReadType(Type type)
        {
            var model = TinyCsvFormatterModels.GetModelType(type, false);
            return model != null
                && (type.IsAssignableFrom(typeof(List<>).MakeGenericType(model)) || type == model.MakeArrayType());
        }

        /// <summary>
        /// Reads the models with the ITinyCsv&lt;T&gt; registered with AddTinyCsv, or with the columns of the model
        /// </summary>
        public override async Task<InputFormatterResult> ReadRequestBodyAsync(InputFormatterContext context, Encoding encoding)
        {
            ArgumentNullException.ThrowIfNull(context);
            var model = TinyCsvFormatterModels.GetModelType(context.ModelType, false)!;
            try
            {
                var task = (Task<object>)ReadMethod.MakeGenericMethod(model).Invoke(null, new object[] { context.HttpContext, context.ModelType == model.MakeArrayType() })!;
                return await InputFormatterResult.SuccessAsync(await task.ConfigureAwait(false)).ConfigureAwait(false);
            }
            catch (CsvException exception)
            {
                // the message has the row and the column, when known
                context.ModelState.TryAddModelError(context.ModelName, exception.Message);
                return await InputFormatterResult.FailureAsync().ConfigureAwait(false);
            }
        }

        private static async Task<object> ReadAsync<T>(HttpContext httpContext, bool array)
            where T : class, new()
        {
            var csv = TinyCsvFormatterModels.Get<T>(httpContext);
            var records = new List<T>();
            await foreach (var record in csv.LoadFromStreamAsync(httpContext.Request.Body, httpContext.RequestAborted).ConfigureAwait(false))
            {
                records.Add(record);
            }
            return array ? records.ToArray() : records;
        }
    }
}
#endif
