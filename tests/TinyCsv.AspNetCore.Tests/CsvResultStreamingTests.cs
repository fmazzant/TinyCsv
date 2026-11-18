#if NET10_0_OR_GREATER
namespace Mafesoft.TinyCsv.AspNetCore.Tests
{
    using System.Collections.Generic;
    using System.IO;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;
    using Mafesoft.TinyCsv.AspNetCore.Extensions;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.TestHost;
    using Xunit;

    /// <summary>
    /// The CSV result writes the records one at a time: the first rows reach the client
    /// while the most of the records are not yet created, so the memory does not grow with the number of rows
    /// </summary>
    public class CsvResultStreamingTests
    {
        public class Row
        {
            public int Id { get; set; }
            public string Name { get; set; }
        }

        private const int Count = 1_000_000;

        private static int produced;

        private static async IAsyncEnumerable<Row> Rows()
        {
            for (int i = 0; i < Count; i++)
            {
                Interlocked.Increment(ref produced);
                yield return new Row { Id = i, Name = "Name" };
                if (i % 1000 == 0)
                {
                    await Task.Yield();
                }
            }
        }

        [Fact]
        public async Task Export_OneMillionRows_IsStreamed()
        {
            var builder = WebApplication.CreateBuilder();
            builder.WebHost.UseTestServer();
            await using var app = builder.Build();
            var csv = new TinyCsv<Row>(o => o.HasHeaderRecord = true);
            app.MapGet("/rows.csv", () => Results.Extensions.Csv(csv, Rows(), "rows.csv"));
            await app.StartAsync();

            var client = app.GetTestClient();
            using var response = await client.GetAsync("/rows.csv", HttpCompletionOption.ResponseHeadersRead);
            using var reader = new StreamReader(await response.Content.ReadAsStreamAsync());

            Assert.Equal("Id;Name", await reader.ReadLineAsync());
            Assert.Equal("0;Name", await reader.ReadLineAsync());
            // the response is not buffered: most of the rows are not yet created
            Assert.True(Volatile.Read(ref produced) < Count / 2, $"{produced} rows created before the first row was read.");

            var lines = 2;
            while (await reader.ReadLineAsync() != null)
            {
                lines++;
            }
            Assert.Equal(Count + 1, lines);
        }
    }
}
#endif
