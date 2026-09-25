#if NET5_0_OR_GREATER
namespace TinyCsv.Tests
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Xunit;

    public class AsyncReaderTests
    {
        private static async Task<List<T>> ToListAsync<T>(IAsyncEnumerable<T> source)
        {
            var result = new List<T>();
            await foreach (var item in source)
            {
                result.Add(item);
            }
            return result;
        }

        [Fact]
        public async Task LoadFromTextAsync_SimpleRows()
        {
            var csv = Csv.Person(o => o.HasHeaderRecord = true);

            var result = await ToListAsync(csv.LoadFromTextAsync("Id;Name;City\n1;Mario;Roma\n2;\"Luigi; Jr\";Milano"));

            Assert.Equal(2, result.Count);
            Assert.Equal("Mario", result[0].Name);
            Assert.Equal("Luigi; Jr", result[1].Name);
        }

        [Fact]
        public async Task LoadFromTextAsync_Cancellation()
        {
            var csv = Csv.Person();
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            await Assert.ThrowsAnyAsync<System.OperationCanceledException>(
                () => ToListAsync(csv.LoadFromTextAsync("1;Mario;Roma", cancellationToken: cts.Token)));
        }

        // Known bugs, see ReaderTests.

        [Fact]
        public async Task LoadFromTextAsync_Multiline()
        {
            var csv = Csv.Person();

            var result = await ToListAsync(csv.LoadFromTextAsync("1;\"Mario\nRossi\";Roma\n2;Luigi;Milano"));

            Assert.Equal(2, result.Count);
            Assert.Equal("Mario\nRossi", result[0].Name);
        }

        [Fact]
        public async Task SkipRow_WithRowsToSkip_SameIndexesAsSync()
        {
            var indexes = new List<int>();
            var csv = Csv.Person(o =>
            {
                o.RowsToSkip = 1;
                o.SkipRow = (row, idx) => { indexes.Add(idx); return false; };
            });

            await ToListAsync(csv.LoadFromTextAsync("skip\n1;a;b\n2;c;d"));

            Assert.Equal(new[] { 1, 2 }, indexes);
        }
    }
}
#endif
