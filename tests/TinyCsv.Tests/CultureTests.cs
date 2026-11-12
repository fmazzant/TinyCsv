namespace Mafesoft.TinyCsv.Tests
{
    using System;
    using System.Globalization;
    using System.Linq;
    using Mafesoft.TinyCsv.Attributes;
    using Xunit;

    public class CultureModel
    {
        public DateTime Date { get; set; }
        public decimal Amount { get; set; }
        public DateTimeOffset Offset { get; set; }
        public TimeSpan Duration { get; set; }
        public byte Byte { get; set; }
#if NET6_0_OR_GREATER
        public DateOnly Day { get; set; }
        public TimeOnly Time { get; set; }
#endif
    }

    [Culture("it-IT")]
    public class ItalianCultureModel
    {
        [Column]
        public decimal Amount { get; set; }
    }

    public class CultureTests
    {
        private static readonly DateTime Date = new DateTime(2026, 11, 2, 10, 30, 0);

        /// <summary>
        /// Sets the culture of the current thread (the "machine" culture) until disposed
        /// </summary>
        private sealed class MachineCulture : IDisposable
        {
            private readonly CultureInfo previous = CultureInfo.CurrentCulture;

            public MachineCulture(string name)
            {
                CultureInfo.CurrentCulture = new CultureInfo(name);
            }

            public void Dispose()
            {
                CultureInfo.CurrentCulture = previous;
            }
        }

        private static CultureModel Sample()
        {
            return new CultureModel
            {
                Date = Date,
                Amount = 1234.5m,
                Offset = new DateTimeOffset(Date, TimeSpan.FromHours(1)),
                Duration = new TimeSpan(1, 2, 3),
                Byte = 200,
#if NET6_0_OR_GREATER
                Day = new DateOnly(2026, 11, 2),
                Time = new TimeOnly(10, 30),
#endif
            };
        }

        private static void AssertSameValues(CultureModel expected, CultureModel actual)
        {
            Assert.Equal(expected.Date, actual.Date);
            Assert.Equal(expected.Amount, actual.Amount);
            Assert.Equal(expected.Offset, actual.Offset);
            Assert.Equal(expected.Duration, actual.Duration);
            Assert.Equal(expected.Byte, actual.Byte);
#if NET6_0_OR_GREATER
            Assert.Equal(expected.Day, actual.Day);
            Assert.Equal(expected.Time, actual.Time);
#endif
        }

        [Theory]
        [InlineData("it-IT")]
        [InlineData("en-US")]
        [InlineData("de-DE")]
        [InlineData("ja-JP")]
        public void SavedFileIsReadBack_OnAnyMachineCulture(string machineCulture)
        {
            using (new MachineCulture(machineCulture))
            {
                var csv = new TinyCsv<CultureModel>(o => o.HasHeaderRecord = true);
                var sample = Sample();

                var result = csv.LoadFromText(csv.GetAllText(new[] { sample })).Single();

                AssertSameValues(sample, result);
            }
        }

        [Fact]
        public void Default_IsInvariant_WhateverTheMachineCulture()
        {
            using (new MachineCulture("it-IT"))
            {
                var csv = new TinyCsv<CultureModel>(o => o.HasHeaderRecord = false);

                var line = csv.GetAllLines(new[] { Sample() }).Single();
                var result = csv.LoadFromText("11/02/2026 10:30:00;1234.5").Single();

                Assert.Same(CultureInfo.InvariantCulture, csv.Options.Culture);
                Assert.StartsWith("11/02/2026 10:30:00;1234.5;", line);
                Assert.Equal(Date, result.Date);
                Assert.Equal(1234.5m, result.Amount);
            }
        }

        [Fact]
        public void Culture_IsUsedToReadAndWrite()
        {
            using (new MachineCulture("en-US"))
            {
                var csv = new TinyCsv<CultureModel>(o =>
                {
                    o.Culture = new CultureInfo("it-IT");
                    o.Columns.AddColumn(m => m.Date);
                    o.Columns.AddColumn(m => m.Amount);
                });

                var line = csv.GetAllLines(new[] { Sample() }).Single();
                var result = csv.LoadFromText("02/11/2026 10:30:00;1234,5").Single();

                Assert.Equal("02/11/2026 10:30:00;1234,5", line);
                Assert.Equal(Date, result.Date);
                Assert.Equal(1234.5m, result.Amount);
            }
        }

        [Fact]
        public void Culture_IsUsedByTheColumnsWithAFormat()
        {
            using (new MachineCulture("en-US"))
            {
                var csv = new TinyCsv<CultureModel>(o =>
                {
                    o.Culture = new CultureInfo("it-IT");
                    o.Columns.AddColumn(m => m.Date, "dd MMMM yyyy");
                    o.Columns.AddColumn(m => m.Amount, "N2");
                });

                var line = csv.GetAllLines(new[] { Sample() }).Single();
                var result = csv.LoadFromText(line).Single();

                Assert.Equal("02 novembre 2026;1.234,50", line);
                Assert.Equal(Date.Date, result.Date);
                Assert.Equal(1234.5m, result.Amount);
            }
        }

        [Fact]
        public void ColumnsWithAFormat_UseTheInvariantCultureByDefault()
        {
            using (new MachineCulture("it-IT"))
            {
                var csv = new TinyCsv<CultureModel>(o =>
                {
                    o.Columns.AddColumn(m => m.Date, "dd MMMM yyyy");
                    o.Columns.AddColumn(m => m.Amount, "N2");
                });

                var line = csv.GetAllLines(new[] { Sample() }).Single();

                Assert.Equal("02 November 2026;1,234.50", line);
                Assert.Equal(Date.Date, csv.LoadFromText(line).Single().Date);
            }
        }

        [Fact]
        public void ColumnFormatProvider_TakesPrecedenceOverCulture()
        {
            var csv = new TinyCsv<CultureModel>(o =>
            {
                o.Culture = new CultureInfo("it-IT");
                o.Columns.AddColumn(0, "Amount", m => m.Amount, CultureInfo.InvariantCulture);
            });

            Assert.Equal("1234.5", csv.GetAllLines(new[] { Sample() }).Single());
            Assert.Equal(1234.5m, csv.LoadFromText("1234.5").Single().Amount);
        }

        [Fact]
        public void Culture_CurrentCulture_ReadsAsTheMachine()
        {
            using (new MachineCulture("it-IT"))
            {
                var csv = new TinyCsv<CultureModel>(o =>
                {
                    o.Culture = CultureInfo.CurrentCulture;
                    o.Columns.AddColumn(m => m.Date);
                });

                Assert.Equal(new DateTime(2026, 11, 2), csv.LoadFromText("02/11/2026").Single().Date);
            }
        }

        [Fact]
        public void Culture_Attribute()
        {
            var csv = new TinyCsv<ItalianCultureModel>();

            Assert.Equal("it-IT", csv.Options.Culture.Name);
            Assert.Equal(1234.5m, csv.LoadFromText("1234,5").Single().Amount);
        }

        [Fact]
        public void Culture_Null_IsInvariant()
        {
            using (new MachineCulture("it-IT"))
            {
                var csv = new TinyCsv<CultureModel>(o =>
                {
                    o.Culture = null;
                    o.Columns.AddColumn(m => m.Date);
                    o.Columns.AddColumn(m => m.Amount, "N2");
                });

                Assert.Equal("11/02/2026 10:30:00;1,234.50", csv.GetAllLines(new[] { Sample() }).Single());
                Assert.Equal(Date, csv.LoadFromText("11/02/2026 10:30:00").Single().Date);
            }
        }
    }
}
