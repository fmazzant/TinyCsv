namespace Mafesoft.TinyCsv.AspNetCore.Tests
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Mafesoft.TinyCsv.AspNetCore.Extensions;
    using Mafesoft.TinyCsv.Exceptions;
#if !NET10_0_OR_GREATER
    using Mafesoft.TinyCsv.Extensions;
#endif
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.DependencyInjection;
    using Xunit;

    /// <summary>
    /// The CSV result, the request helpers and the ProblemDetails on a DefaultHttpContext (.NET 8 and .NET 10)
    /// </summary>
    public class CsvHttpUnitTests
    {
        public class Row
        {
            public int Id { get; set; }
            public string Name { get; set; }
        }

        private static readonly TinyCsv<Row> Csv = new TinyCsv<Row>(o => o.HasHeaderRecord = true);

        private static DefaultHttpContext Context()
        {
            var context = new DefaultHttpContext();
            context.Response.Body = new MemoryStream();
            return context;
        }

        private static string Body(HttpContext context) => Encoding.UTF8.GetString(((MemoryStream)context.Response.Body).ToArray());

        [Fact]
        public async Task CsvResult_WritesTheRecordsAndTheHeaders()
        {
            var context = Context();
            var result = Results.Extensions.Csv(Csv, new[] { new Row { Id = 1, Name = "a;b" } }, "rows.csv");

            await result.ExecuteAsync(context);

            Assert.Equal(200, context.Response.StatusCode);
            Assert.Equal("text/csv; charset=utf-8", context.Response.ContentType);
            Assert.Contains("attachment", context.Response.Headers.ContentDisposition.ToString());
            Assert.Contains("rows.csv", context.Response.Headers.ContentDisposition.ToString());
            Assert.Equal("Id;Name\r\n1;\"a;b\"\r\n", Body(context));
        }

        [Fact]
        public async Task CsvResult_WithoutFileName_IsNotADownload()
        {
            var context = Context();

            await Results.Extensions.Csv(Csv, AsyncRows(), null).ExecuteAsync(context);

            Assert.Equal(0, context.Response.Headers.ContentDisposition.Count);
            Assert.Equal("Id;Name\r\n1;x\r\n", Body(context));
        }

        private static async System.Collections.Generic.IAsyncEnumerable<Row> AsyncRows()
        {
            await Task.Yield();
            yield return new Row { Id = 1, Name = "x" };
        }

        [Fact]
        public async Task LoadFromRequestAsync_ReadsTheBody()
        {
            var context = new DefaultHttpContext();
            context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes("Id;Name\r\n1;a\r\n2;b\r\n"));

            var rows = await Csv.LoadFromRequestAsync(context.Request).ToListAsync();

            Assert.Equal(new[] { "a", "b" }, rows.Select(r => r.Name));
        }

        [Fact]
        public void ProblemDetails_HasTheRowTheColumnAndTheValue()
        {
            var exception = new InvalidColumnValueException("Id", "x", "The value 'x' is not valid.");
            exception.GetType().GetMethod("SetRowIndex", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.Invoke(exception, new object[] { 3 });

            var problem = TinyCsvProblemDetails.Create(exception);

            Assert.Equal(400, problem.Status);
            Assert.Equal(TinyCsvProblemDetails.Title, problem.Title);
            Assert.StartsWith("The value 'x' is not valid.", problem.Detail);
            Assert.Equal(3, problem.Extensions["row"]);
            Assert.Equal("Id", problem.Extensions["column"]);
            Assert.Equal("x", problem.Extensions["value"]);
        }

        [Fact]
        public async Task ExceptionHandler_HandlesOnlyTheCsvExceptions()
        {
            var handler = new TinyCsvExceptionHandler();
            var context = Context();
            context.RequestServices = new ServiceCollection().AddLogging().BuildServiceProvider();

            Assert.False(await handler.TryHandleAsync(context, new InvalidOperationException(), default));
            Assert.True(await handler.TryHandleAsync(context, new CsvException("bad"), default));
            Assert.Equal(400, context.Response.StatusCode);
            Assert.Contains("\"detail\":\"bad\"", Body(context));
        }
    }
}
