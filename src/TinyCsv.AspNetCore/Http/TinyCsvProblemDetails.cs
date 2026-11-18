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
    using System.Threading;
    using System.Threading.Tasks;
    using Mafesoft.TinyCsv.Exceptions;
    using Microsoft.AspNetCore.Diagnostics;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;

    /// <summary>
    /// The ProblemDetails (RFC 9457) of a CSV with bad data: status 400, the message of the exception as detail
    /// and the extensions <c>row</c> (the index of the row, the header included), <c>column</c> and <c>value</c>, when known
    /// </summary>
    public static class TinyCsvProblemDetails
    {
        /// <summary>
        /// The title of the problem
        /// </summary>
        public const string Title = "The CSV contains bad data.";

        /// <summary>
        /// Creates the ProblemDetails of the exception
        /// </summary>
        /// <param name="exception"></param>
        /// <param name="httpContext">the request, for the instance (the path), if any</param>
        /// <returns></returns>
        public static ProblemDetails Create(CsvException exception, HttpContext? httpContext = null)
        {
            ArgumentNullException.ThrowIfNull(exception);
            var problem = new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = Title,
                Detail = exception.Message,
                Instance = httpContext?.Request.Path.Value,
            };
            if (exception.RowIndex != null)
            {
                problem.Extensions["row"] = exception.RowIndex.Value;
            }
            if (!string.IsNullOrEmpty(exception.ColumnName))
            {
                problem.Extensions["column"] = exception.ColumnName;
            }
            if (exception.Value != null)
            {
                problem.Extensions["value"] = exception.Value;
            }
            return problem;
        }

        /// <summary>
        /// The 400 result of the exception, for the minimal APIs
        /// </summary>
        /// <param name="exception"></param>
        /// <returns></returns>
        public static IResult ToResult(CsvException exception)
        {
            return Results.Problem(Create(exception));
        }
    }

    /// <summary>
    /// Turns a <see cref="CsvException"/> not handled by the endpoint (i.e. a bad upload) into a 400 ProblemDetails,
    /// with the row and the column. Register it with <c>AddTinyCsvProblemDetails()</c> and use <c>app.UseExceptionHandler()</c>.
    /// </summary>
    public sealed class TinyCsvExceptionHandler : IExceptionHandler
    {
        /// <summary>
        /// Writes the ProblemDetails of a CsvException; the other exceptions are left to the next handler
        /// </summary>
        public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
        {
            var csvException = exception as CsvException ?? exception.InnerException as CsvException;
            if (csvException == null || httpContext.Response.HasStarted)
            {
                return false;
            }
            httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
            await Results.Problem(TinyCsvProblemDetails.Create(csvException, httpContext)).ExecuteAsync(httpContext).ConfigureAwait(false);
            return true;
        }
    }
}
#endif
