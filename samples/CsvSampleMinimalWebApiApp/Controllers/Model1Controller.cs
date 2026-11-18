namespace Controllers
{
    using Mafesoft.TinyCsv;
    using Microsoft.AspNetCore.Mvc;
    using Models;

    /// <summary>
    /// The MVC formatters (AddTinyCsvFormatters): GET with <c>Accept: text/csv</c> (or <c>/api/model1.csv</c>) writes CSV,
    /// POST with <c>Content-Type: text/csv</c> reads the models. A bad row is a 400 ValidationProblemDetails ([ApiController]).
    /// </summary>
    [ApiController]
    [Route("api/model1")]
    public class Model1Controller : ControllerBase
    {
        private readonly ITinyCsv<Model1> csv;

        public Model1Controller(ITinyCsv<Model1> csv)
        {
            this.csv = csv;
        }

        [HttpGet]
        [HttpGet("~/api/model1.{format}")]
        [FormatFilter]
        public IAsyncEnumerable<Model1> Get() => csv.LoadFromFileAsync("model1.csv");

        [HttpPost]
        [Consumes("text/csv")]
        public ActionResult<object> Post([FromBody] List<Model1> models)
        {
            return new { count = models.Count, total = models.Sum(m => m.Price) };
        }
    }
}
