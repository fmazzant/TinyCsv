#if NET10_0_OR_GREATER
namespace Mafesoft.TinyCsv.AspNetCore.Tests
{
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Net.Http.Json;
    using System.Text;
    using System.Text.Json;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Mvc.Testing;
    using Xunit;

    /// <summary>
    /// The CSV download and upload of the minimal API sample (samples/CsvSampleMinimalWebApiApp), run in memory by
    /// WebApplicationFactory: the CSV result, IFormFile, the request body, the MVC formatters and the ProblemDetails of the bad data
    /// </summary>
    public class CsvHttpSampleTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private const string Upload = "Id;Name;Price\r\n1;First;1.5\r\n2;Second;2.25\r\n";
        private const string BadUpload = "Id;Name;Price\r\n1;First;1.5\r\nx;Second;2.25\r\n";

        private readonly WebApplicationFactory<Program> factory;

        public CsvHttpSampleTests(WebApplicationFactory<Program> factory)
        {
            this.factory = factory;
        }

        private static string[] Lines(string text) => text.Split("\r\n").Where(line => line.Length > 0).ToArray();

        [Fact]
        public async Task CsvResult_Download()
        {
            var response = await factory.CreateClient().GetAsync("/model1.csv");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("text/csv", response.Content.Headers.ContentType?.MediaType);
            Assert.Equal("utf-8", response.Content.Headers.ContentType?.CharSet);
            Assert.Equal("attachment", response.Content.Headers.ContentDisposition?.DispositionType);
            Assert.Equal("model1.csv", response.Content.Headers.ContentDisposition?.FileName?.Trim('"'));
            var lines = Lines(await response.Content.ReadAsStringAsync());
            Assert.Equal(19, lines.Length);
            Assert.Equal("Id;Name;Price", lines[0]);
            Assert.StartsWith("3;Name 3;", lines[3]);
        }

        [Fact]
        public async Task FormFile_Upload()
        {
            using var content = new MultipartFormDataContent();
            content.Add(new StringContent(Upload, Encoding.UTF8, "text/csv"), "file", "upload.csv");

            var response = await factory.CreateClient().PostAsync("/model1/import", content);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var result = await response.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal(2, result.GetProperty("count").GetInt32());
            Assert.Equal(3.75m, result.GetProperty("total").GetDecimal());
        }

        [Fact]
        public async Task FormFile_BadData_IsAProblemDetails()
        {
            using var content = new MultipartFormDataContent();
            content.Add(new StringContent(BadUpload, Encoding.UTF8, "text/csv"), "file", "upload.csv");

            var response = await factory.CreateClient().PostAsync("/model1/import", content);

            await AssertProblem(response);
        }

        [Fact]
        public async Task RequestBody_Upload()
        {
            var response = await factory.CreateClient().PostAsync("/model1/import-body", new StringContent(Upload, Encoding.UTF8, "text/csv"));

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var models = await response.Content.ReadFromJsonAsync<JsonElement[]>();
            Assert.Equal(new[] { "First", "Second" }, models.Select(m => m.GetProperty("name").GetString()));
        }

        [Fact]
        public async Task RequestBody_BadData_IsAProblemDetails()
        {
            var response = await factory.CreateClient().PostAsync("/model1/import-body", new StringContent(BadUpload, Encoding.UTF8, "text/csv"));

            await AssertProblem(response);
        }

        private static async Task AssertProblem(HttpResponseMessage response)
        {
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
            var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal(400, problem.GetProperty("status").GetInt32());
            Assert.Equal(TinyCsvProblemDetails.Title, problem.GetProperty("title").GetString());
            Assert.Equal(2, problem.GetProperty("row").GetInt32());
            Assert.Equal("Id", problem.GetProperty("column").GetString());
            Assert.Equal("x", problem.GetProperty("value").GetString());
        }

        [Fact]
        public async Task Mvc_OutputFormatter_AcceptTextCsv()
        {
            var client = factory.CreateClient();
            var request = new HttpRequestMessage(HttpMethod.Get, "/api/model1");
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/csv"));

            var response = await client.SendAsync(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("text/csv", response.Content.Headers.ContentType?.MediaType);
            var lines = Lines(await response.Content.ReadAsStringAsync());
            Assert.Equal(19, lines.Length);
            Assert.Equal("Id;Name;Price", lines[0]);
        }

        [Fact]
        public async Task Mvc_OutputFormatter_FormatFilter()
        {
            var response = await factory.CreateClient().GetAsync("/api/model1.csv");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("text/csv", response.Content.Headers.ContentType?.MediaType);
        }

        [Fact]
        public async Task Mvc_DefaultIsStillJson()
        {
            var models = await factory.CreateClient().GetFromJsonAsync<JsonElement[]>("/api/model1");

            Assert.Equal(18, models.Length);
        }

        [Fact]
        public async Task Mvc_InputFormatter()
        {
            var response = await factory.CreateClient().PostAsync("/api/model1", new StringContent(Upload, Encoding.UTF8, "text/csv"));

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var result = await response.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal(2, result.GetProperty("count").GetInt32());
        }

        [Fact]
        public async Task Mvc_InputFormatter_BadData_IsAValidationProblem()
        {
            var response = await factory.CreateClient().PostAsync("/api/model1", new StringContent(BadUpload, Encoding.UTF8, "text/csv"));

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var text = await response.Content.ReadAsStringAsync();
            Assert.Contains("row 2, column Id", text);
        }
    }
}
#endif
