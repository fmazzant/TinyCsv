namespace TinyCsv.Tests
{
    using System.IO;
    using System.Linq;
    using System.Text;
    using Xunit;

    public class WriterTests
    {
        private static readonly Person[] People =
        {
            new Person { Id = 1, Name = "Mario", City = "Roma" },
            new Person { Id = 2, Name = "Rossi; Luigi", City = "Milano" },
        };

        [Fact]
        public void GetAllLines_WithHeader()
        {
            var csv = Csv.Person(o => o.HasHeaderRecord = true);

            var lines = csv.GetAllLines(People);

            Assert.Equal(new[] { "Id;Name;City", "1;Mario;Roma", "2;\"Rossi; Luigi\";Milano" }, lines);
        }

        [Fact]
        public void GetAllText_UsesNewLine()
        {
            var csv = Csv.Person(o => o.NewLine = "\n");

            var text = csv.GetAllText(People);

            Assert.Equal("1;Mario;Roma\n2;\"Rossi; Luigi\";Milano", text);
        }

        [Fact]
        public void Save_ToStream()
        {
            var csv = Csv.Person(o => o.NewLine = "\n");
            var stream = new MemoryStream();

            csv.Save(stream, People);

            var text = Encoding.UTF8.GetString(stream.ToArray()).TrimStart('﻿');
            Assert.Contains("1;Mario;Roma", text);
            Assert.Contains("2;\"Rossi; Luigi\";Milano", text);
        }

        [Fact]
        public void RoundTrip()
        {
            var csv = Csv.Person(o => o.HasHeaderRecord = true);

            var result = csv.LoadFromText(csv.GetAllText(People)).ToList();

            Assert.Equal(People.Select(p => (p.Id, p.Name, p.City)), result.Select(p => (p.Id, p.Name, p.City)));
        }

        [Fact]
        public void RoundTrip_QuotesAndNewLines()
        {
            var csv = Csv.Person();
            var people = new[] { new Person { Id = 1, Name = "Mario \"Super\"\nRossi", City = "Roma" } };

            var result = csv.LoadFromText(csv.GetAllText(people)).Single();

            Assert.Equal("Mario \"Super\"\nRossi", result.Name);
            Assert.Equal("Roma", result.City);
        }
    }
}
