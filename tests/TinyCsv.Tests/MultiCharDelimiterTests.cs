namespace Mafesoft.TinyCsv.Tests
{
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Xunit;

    public class MultiCharDelimiterTests
    {
        [Theory]
        [InlineData("||", "a|", "\"a|\"")]
        [InlineData("||", "|", "\"|\"")]
        [InlineData("||", "|a", "|a")]
        [InlineData("||", "a|b", "a|b")]
        [InlineData(";;;", "a;", "\"a;\"")]
        [InlineData(";;;", "a;;", "\"a;;\"")]
        [InlineData("<>", "a<", "\"a<\"")]
        [InlineData("<>", "a>", "a>")]
        public void ValueEndingWithTheStartOfTheDelimiter_IsQuoted(string delimiter, string name, string written)
        {
            var csv = Csv.Person(o => o.Delimiter = delimiter);
            var model = new Person { Id = 1, Name = name, City = "b" };

            var line = csv.GetAllLines(new[] { model }).Single();

            Assert.Equal("1" + delimiter + written + delimiter + "b", line);
        }

        [Theory]
        [InlineData("||")]
        [InlineData(";;;")]
        [InlineData("<>")]
        public async Task ValueEndingWithTheStartOfTheDelimiter_IsReadBack(string delimiter)
        {
            var csv = Csv.Person(o => o.Delimiter = delimiter);
            var start = delimiter.Substring(0, delimiter.Length - 1);
            var models = new[]
            {
                new Person { Id = 1, Name = "a" + start, City = "b" },
                new Person { Id = 2, Name = start, City = start },
                new Person { Id = 3, Name = "x" + delimiter + "y", City = "c" + start },
            };

            var text = csv.GetAllLines(models).Aggregate((a, b) => a + "\r\n" + b);
            var stream = new MemoryStream();
            await csv.SaveAsync(stream, models);
            var saved = Encoding.UTF8.GetString(stream.ToArray()).TrimStart('﻿');

            foreach (var source in new[] { text, saved })
            {
                var read = csv.LoadFromText(source).ToArray();
                Assert.Equal(models.Select(m => (m.Id, m.Name, m.City)), read.Select(m => (m.Id, m.Name, m.City)));
            }
        }
    }
}
