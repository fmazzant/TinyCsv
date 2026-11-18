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
namespace Mafesoft.TinyCsv.AspNetCore.Extensions
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using System.Threading.Tasks;
    using Mafesoft.TinyCsv.Exceptions;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.DependencyInjection;

    /// <summary>
    /// TinyCsv in the minimal APIs: CSV results, uploaded files and request bodies, ProblemDetails for bad data
    /// </summary>
    public static class TinyCsvHttpExtensions
    {
        /// <summary>
        /// A result that writes the records as CSV, one at a time (i.e. <c>db.Orders.AsAsyncEnumerable()</c>):
        /// <c>text/csv; charset=utf-8</c>, and a download when the file name is set
        /// </summary>
        public static TinyCsvResult<T> Csv<T>(this IResultExtensions resultExtensions, ITinyCsv<T> csv, IAsyncEnumerable<T> records, string? fileDownloadName = null)
            where T : class, new()
        {
            ArgumentNullException.ThrowIfNull(resultExtensions);
            ArgumentNullException.ThrowIfNull(records);
            return new TinyCsvResult<T>(csv, null, records, fileDownloadName);
        }

        /// <summary>
        /// A result that writes the records as CSV, one at a time: <c>text/csv; charset=utf-8</c>, and a download when the file name is set
        /// </summary>
        public static TinyCsvResult<T> Csv<T>(this IResultExtensions resultExtensions, ITinyCsv<T> csv, IEnumerable<T> records, string? fileDownloadName = null)
            where T : class, new()
        {
            ArgumentNullException.ThrowIfNull(resultExtensions);
            ArgumentNullException.ThrowIfNull(records);
            return new TinyCsvResult<T>(csv, records, null, fileDownloadName);
        }

        /// <summary>
        /// The 400 ProblemDetails of a CSV with bad data, with the row and the column (see <see cref="TinyCsvProblemDetails"/>)
        /// </summary>
        public static IResult CsvProblem(this IResultExtensions resultExtensions, CsvException exception)
        {
            ArgumentNullException.ThrowIfNull(resultExtensions);
            return TinyCsvProblemDetails.ToResult(exception);
        }

        /// <summary>
        /// Reads the records of an uploaded file asynchronously, one at a time
        /// </summary>
        public static async IAsyncEnumerable<T> LoadFromFormFileAsync<T>(this ITinyCsv<T> csv, IFormFile file, [EnumeratorCancellation] CancellationToken cancellationToken = default)
            where T : class, new()
        {
            ArgumentNullException.ThrowIfNull(csv);
            ArgumentNullException.ThrowIfNull(file);
            await using (var stream = file.OpenReadStream())
            {
                await foreach (var record in csv.LoadFromStreamAsync(stream, cancellationToken).ConfigureAwait(false))
                {
                    yield return record;
                }
            }
        }

        /// <summary>
        /// Reads the records of the request body asynchronously, one at a time (i.e. a <c>text/csv</c> POST);
        /// the request is cancelled with <c>RequestAborted</c>
        /// </summary>
        public static IAsyncEnumerable<T> LoadFromRequestAsync<T>(this ITinyCsv<T> csv, HttpRequest request, CancellationToken cancellationToken = default)
            where T : class, new()
        {
            ArgumentNullException.ThrowIfNull(csv);
            ArgumentNullException.ThrowIfNull(request);
            var token = cancellationToken.CanBeCanceled ? cancellationToken : request.HttpContext.RequestAborted;
            return csv.LoadFromStreamAsync(request.Body, token);
        }

        /// <summary>
        /// Registers <see cref="TinyCsvExceptionHandler"/> and the ProblemDetails service: a CsvException not handled by
        /// the endpoint becomes a 400 ProblemDetails with the row and the column. Use it with <c>app.UseExceptionHandler()</c>.
        /// </summary>
        public static IServiceCollection AddTinyCsvProblemDetails(this IServiceCollection services)
        {
            ArgumentNullException.ThrowIfNull(services);
            services.AddProblemDetails();
            services.AddExceptionHandler<TinyCsvExceptionHandler>();
            return services;
        }
    }
}
#endif
