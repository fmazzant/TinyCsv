namespace TinyCsv.Tests
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Xunit;

    public class RecordWriterTests
    {
        private static string Normalize(string text) => text.Replace("\r\n", "\n");

        [Fact]
        public void WriteRecord_WritesTheHeaderAndTheRecords()
        {
            var csv = Csv.Person(o => o.HasHeaderRecord = true);
            var stream = new MemoryStream();

            using (var writer = csv.CreateWriter(stream))
            {
                writer.WriteRecord(new Person { Id = 1, Name = "Mario", City = "Roma" });
                writer.WriteRecord(new Person { Id = 2, Name = "Rossi; Luigi", City = "Milano" });
                Assert.Equal(3, writer.Count);
            }

            Assert.Equal("Id;Name;City\n1;Mario;Roma\n2;\"Rossi; Luigi\";Milano\n", Normalize(Encoding.UTF8.GetString(stream.ToArray())));
        }

        [Fact]
        public void CreateWriter_Stream_IsLeftOpen()
        {
            var csv = Csv.Person();
            var stream = new MemoryStream();

            using (var writer = csv.CreateWriter(stream))
            {
                writer.WriteRecord(new Person { Id = 1 });
            }

            Assert.True(stream.CanWrite);
        }

        [Fact]
        public void CreateWriter_File_WritesManyRecordsOneAtATime()
        {
            var csv = Csv.Person();
            var path = Path.GetTempFileName();
            try
            {
                using (var writer = csv.CreateWriter(path))
                {
                    for (int i = 0; i < 100000; i++)
                    {
                        writer.WriteRecord(new Person { Id = i, Name = "Name", City = "City" });
                    }
                }

                var people = csv.LoadFromFile(path).ToArray();
                Assert.Equal(100000, people.Length);
                Assert.Equal(99999, people.Last().Id);
            }
            finally
            {
                File.Delete(path);
            }
        }

        [Fact]
        public void Dispose_WithoutRecords_WritesTheHeader()
        {
            var csv = Csv.Person(o => o.HasHeaderRecord = true);
            var stream = new MemoryStream();

            using (csv.CreateWriter(stream))
            {
            }

            Assert.Equal("Id;Name;City\n", Normalize(Encoding.UTF8.GetString(stream.ToArray())));
        }

        [Fact]
        public void CreateWriter_StreamWriter_RaisesTheEvents()
        {
            var events = new List<string>();
            var csv = Csv.Person(o =>
            {
                o.HasHeaderRecord = true;
                o.EnableHandlers = true;
                o.Handlers.Start += (s, e) => events.Add("start");
                o.Handlers.Write.RowHeader += (s, e) => events.Add($"header:{e.Index}");
                o.Handlers.Write.RowWritten += (s, e) => events.Add($"written:{e.Index}:{e.Row}");
                o.Handlers.Completed += (s, e) => events.Add($"completed:{e.Elements}");
            });
            var streamWriter = new StreamWriter(new MemoryStream());

            using (var writer = csv.CreateWriter(streamWriter))
            {
                writer.WriteRecord(new Person { Id = 1, Name = "Mario", City = "Roma" });
            }

            Assert.Equal(new[] { "start", "header:0", "written:1:1;Mario;Roma", "completed:2" }, events);
            streamWriter.Write("still open");
        }

        [Fact]
        public async Task WriteRecordAsync_WritesTheHeaderAndTheRecords()
        {
            var csv = Csv.Person(o => o.HasHeaderRecord = true);
            var stream = new MemoryStream();

            var writer = csv.CreateWriter(stream);
            await writer.WriteRecordAsync(new Person { Id = 1, Name = "Mario", City = "Roma" });
            await writer.WriteRecordsAsync(new[] { new Person { Id = 2, Name = "Luigi", City = "Milano" } });
            writer.Dispose();

            Assert.Equal("Id;Name;City\n1;Mario;Roma\n2;Luigi;Milano\n", Normalize(Encoding.UTF8.GetString(stream.ToArray())));
        }

#if NET5_0_OR_GREATER
        [Fact]
        public async Task DisposeAsync_FlushesTheRecords()
        {
            var csv = Csv.Person();
            var stream = new MemoryStream();

            await using (var writer = csv.CreateWriter(stream))
            {
                await writer.WriteRecordAsync(new Person { Id = 1, Name = "Mario", City = "Roma" });
            }

            Assert.Equal("1;Mario;Roma\n", Normalize(Encoding.UTF8.GetString(stream.ToArray())));
        }
#endif

        [Fact]
        public void Save_StillWritesTheModels()
        {
            var csv = Csv.Person(o => o.HasHeaderRecord = true);
            var stream = new MemoryStream();

            csv.Save(stream, new[] { new Person { Id = 1, Name = "Mario", City = "Roma" } });

            Assert.Equal("Id;Name;City\n1;Mario;Roma\n", Normalize(Encoding.UTF8.GetString(stream.ToArray())));
        }

        [Fact]
        public async Task SaveAsync_Stream_PassesTheCancellationToken()
        {
            var csv = Csv.Person();
            var cts = new System.Threading.CancellationTokenSource();
            cts.Cancel();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(
                () => csv.SaveAsync(new MemoryStream(), new[] { new Person() }, cts.Token));
        }
    }
}
