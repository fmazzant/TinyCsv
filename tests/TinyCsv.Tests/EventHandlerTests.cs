namespace TinyCsv.Tests
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Xunit;

    public class EventHandlerTests
    {
        private static readonly Person[] People =
        {
            new Person { Id = 1, Name = "Mario", City = "Roma" },
            new Person { Id = 2, Name = "Luigi", City = "Milano" },
        };

        [Fact]
        public void Read_StartAndCompleted()
        {
            var events = new List<string>();
            var csv = Csv.Person(o =>
            {
                o.HasHeaderRecord = true;
                o.EnableHandlers = true;
                o.Handlers.Start += (s, e) => events.Add("start");
                o.Handlers.Completed += (s, e) => events.Add($"completed:{e.Elements}");
            });

            csv.LoadFromText("Id;Name;City\n1;Mario;Roma\n2;Luigi;Milano").ToList();

            Assert.Equal(new[] { "start", "completed:3" }, events);
        }

        [Fact]
        public void Read_RowEventsWithModel()
        {
            var header = new List<(int, string)>();
            var reading = new List<(int, string)>();
            var read = new List<(int, string, string)>();
            var csv = Csv.Person(o =>
            {
                o.HasHeaderRecord = true;
                o.EnableHandlers = true;
                o.Handlers.Read.RowHeader += (s, e) => header.Add((e.Index, e.RowHeader));
                o.Handlers.Read.RowReading += (s, e) => reading.Add((e.Index, e.Row));
                o.Handlers.Read.RowRead += (s, e) => read.Add((e.Index, e.Model.Name, e.Row));
            });

            csv.LoadFromText("Id;Name;City\n1;Mario;Roma\n2;Luigi;Milano").ToList();

            Assert.Equal(new[] { (0, "Id;Name;City") }, header);
            Assert.Equal(new[] { (0, "Id;Name;City"), (1, "1;Mario;Roma"), (2, "2;Luigi;Milano") }, reading);
            Assert.Equal(new[] { (1, "Mario", "1;Mario;Roma"), (2, "Luigi", "2;Luigi;Milano") }, read);
        }

        [Fact]
        public void Write_Events()
        {
            var events = new List<string>();
            var csv = Csv.Person(o =>
            {
                o.HasHeaderRecord = true;
                o.EnableHandlers = true;
                o.Handlers.Start += (s, e) => events.Add("start");
                o.Handlers.Write.RowHeader += (s, e) => events.Add($"header:{e.Index}:{e.RowHeader}");
                o.Handlers.Write.RowWriting += (s, e) => events.Add($"writing:{e.Index}:{e.Model.Name}");
                o.Handlers.Write.RowWrittin += (s, e) => events.Add($"written:{e.Index}:{e.Row}");
                o.Handlers.Completed += (s, e) => events.Add($"completed:{e.Elements}");
            });

            csv.Save(new MemoryStream(), People);

            Assert.Equal(new[]
            {
                "start",
                "header:0:Id;Name;City",
                "writing:1:Mario",
                "written:1:1;Mario;Roma",
                "writing:2:Luigi",
                "written:2:2;Luigi;Milano",
                "completed:3",
            }, events);
        }

        [Fact]
        public void Handlers_DisabledByDefault()
        {
            var raised = 0;
            var csv = Csv.Person(o =>
            {
                o.HasHeaderRecord = true;
                o.Handlers.Start += (s, e) => raised++;
                o.Handlers.Completed += (s, e) => raised++;
                o.Handlers.Read.RowHeader += (s, e) => raised++;
                o.Handlers.Read.RowReading += (s, e) => raised++;
                o.Handlers.Read.RowRead += (s, e) => raised++;
                o.Handlers.Write.RowHeader += (s, e) => raised++;
                o.Handlers.Write.RowWriting += (s, e) => raised++;
                o.Handlers.Write.RowWrittin += (s, e) => raised++;
            });

            csv.LoadFromText("Id;Name;City\n1;Mario;Roma").ToList();
            csv.Save(new MemoryStream(), People);

            Assert.Equal(0, raised);
            Assert.False(csv.Options.Handlers.Enabled);
        }

        [Fact]
        public void Handlers_Enabled_FollowsOption()
        {
            var options = new CsvOptions<Person>();

            options.EnableHandlers = true;

            Assert.True(options.Handlers.Enabled);
            Assert.True(options.Handlers.Read.Enabled);
            Assert.True(options.Handlers.Write.Enabled);
        }
    }
}
