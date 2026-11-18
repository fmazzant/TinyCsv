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
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Net.Http.Headers;

    /// <summary>
    /// A minimal API result that writes the records as CSV (<c>text/csv; charset=utf-8</c>), streamed one at a time:
    /// the whole file is never in memory. With a file name, the response is a download (<c>Content-Disposition: attachment</c>).
    /// Create it with <c>Results.Extensions.Csv(...)</c>.
    /// </summary>
    /// <typeparam name="T">the model</typeparam>
    public sealed class TinyCsvResult<T> : IResult, IStatusCodeHttpResult, IContentTypeHttpResult
        where T : class, new()
    {
        /// <summary>
        /// The content type of the CSV responses
        /// </summary>
        public const string CsvContentType = "text/csv; charset=utf-8";

        private readonly ITinyCsv<T> csv;
        private readonly IEnumerable<T>? records;
        private readonly IAsyncEnumerable<T>? asyncRecords;

        internal TinyCsvResult(ITinyCsv<T> csv, IEnumerable<T>? records, IAsyncEnumerable<T>? asyncRecords, string? fileDownloadName)
        {
            this.csv = csv ?? throw new ArgumentNullException(nameof(csv));
            this.records = records;
            this.asyncRecords = asyncRecords;
            FileDownloadName = fileDownloadName;
        }

        /// <summary>
        /// The name of the downloaded file, or null when the response is not a download
        /// </summary>
        public string? FileDownloadName { get; }

        /// <summary>
        /// 200
        /// </summary>
        public int? StatusCode => StatusCodes.Status200OK;

        /// <summary>
        /// <c>text/csv; charset=utf-8</c>
        /// </summary>
        public string? ContentType => CsvContentType;

        /// <summary>
        /// Writes the records to the response body
        /// </summary>
        /// <param name="httpContext"></param>
        /// <returns></returns>
        public async Task ExecuteAsync(HttpContext httpContext)
        {
            ArgumentNullException.ThrowIfNull(httpContext);
            var response = httpContext.Response;
            response.StatusCode = StatusCodes.Status200OK;
            response.ContentType = CsvContentType;
            if (!string.IsNullOrEmpty(FileDownloadName))
            {
                var disposition = new ContentDispositionHeaderValue("attachment");
                disposition.SetHttpFileName(FileDownloadName);
                response.Headers.ContentDisposition = disposition.ToString();
            }

            // SaveAsync writes and flushes asynchronously (Kestrel does not allow synchronous writes)
            var cancellationToken = httpContext.RequestAborted;
            if (asyncRecords != null)
            {
                await csv.SaveAsync(response.Body, asyncRecords, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                await csv.SaveAsync(response.Body, records ?? Array.Empty<T>(), cancellationToken).ConfigureAwait(false);
            }
        }
    }
}
#endif
