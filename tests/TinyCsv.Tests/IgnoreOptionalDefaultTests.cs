/// <summary>
/// 
/// The MIT License (MIT)
/// 
/// Copyright (c) 2020 Federico Mazzanti
/// 
/// Permission is hereby granted, free of charge, to any person
/// obtaining a copy of this software and associated documentation
/// files (the "Software"), to deal in the Software without
/// restriction, including without limitation the rights to use,
/// copy, modify, merge, publish, distribute, sublicense, and/or sell
/// copies of the Software, and to permit persons to whom the
/// Software is furnished to do so, subject to the following
/// conditions:
/// 
/// The above copyright notice and this permission notice shall be
/// included in all copies or substantial portions of the Software.
/// 
/// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
/// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
/// OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
/// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
/// HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
/// WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
/// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
/// OTHER DEALINGS IN THE SOFTWARE.
/// 
/// </summary>

namespace Mafesoft.TinyCsv.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Mafesoft.TinyCsv.Attributes;
    using Mafesoft.TinyCsv.Exceptions;
    using Xunit;

    public class OrderLine
    {
        public decimal Amount { get; set; }
    }

    public class AutoOrder
    {
        public int Id { get; set; }

        [Ignore]
        public string Internal { get; set; }

        [Optional, Default(5)]
        public int Priority { get; set; }

        [Optional]
        public string Note { get; set; }

        [Default("none")]
        public string Status { get; set; }

        [Ignore]
        public OrderLine Line { get; set; }

        public decimal Total => Line?.Amount ?? 0;
    }

    [HasHeaderRecord(true)]
    [MapColumnsByHeader(true)]
    public class AttributeOrder
    {
        [Column]
        public int Id { get; set; }

        [Column, Ignore]
        public string Internal { get; set; }

        [Column, Optional, Default(DayOfWeek.Monday)]
        public DayOfWeek Day { get; set; }

        [Column(name: "amount"), Optional, Default("1.5")]
        public decimal Amount { get; set; }
    }

    public class WrongDefaultOrder
    {
        [Default("abc")]
        public int Priority { get; set; }
    }

    public class NullDefaultOrder
    {
        [Default(null)]
        public int Priority { get; set; }
    }

    public class IgnoreOptionalDefaultTests
    {
        public class Order
        {
            public int Id { get; set; }
            public string Internal { get; set; }
            public int Priority { get; set; }
            public int? Quantity { get; set; }
            public DateTime Date { get; set; }
        }

        private sealed class OrderMap : CsvClassMap<Order>
        {
            public OrderMap()
            {
                Options(o =>
                {
                    o.HasHeaderRecord = true;
                    o.MapColumnsByHeader = true;
                });
                Map(m => m.Id);
                Map(m => m.Internal).Ignore();
                Map(m => m.Priority).Optional().Default(7);
                Map(m => m.Quantity).Default(1);
                Map(m => m.Date).Optional();
            }
        }

        private static TinyCsv<AutoOrder> AutoByHeader()
        {
            return new TinyCsv<AutoOrder>(o =>
            {
                o.HasHeaderRecord = true;
                o.MapColumnsByHeader = true;
            });
        }

        // ---------- ignore ----------

        [Fact]
        public void Ignore_AutomaticColumns_SkipTheProperty()
        {
            var csv = AutoByHeader();

            Assert.Equal(new[] { "Id", "Priority", "Note", "Status" }, csv.Options.Columns.Select(c => c.ColumnName));
        }

        [Fact]
        public void Ignore_AutomaticColumns_NotWritten()
        {
            var csv = AutoByHeader();

            var lines = csv.GetAllLines(new[] { new AutoOrder { Id = 1, Internal = "x", Priority = 2, Note = "n", Status = "s" } });

            Assert.Equal(new[] { "Id;Priority;Note;Status", "1;2;n;s" }, lines);
        }

        [Fact]
        public void Ignore_ClassMap_ColumnIsNotAdded()
        {
            var csv = new TinyCsv<Order>(new OrderMap());

            Assert.DoesNotContain(csv.Options.Columns, c => c.ColumnName == "Internal");
            var lines = csv.GetAllLines(new[] { new Order { Id = 1, Internal = "x", Priority = 2, Quantity = 3, Date = new DateTime(2024, 1, 31) } });
            Assert.Equal("Id;Priority;Quantity;Date", lines[0]);
            Assert.Equal("1;2;3;01/31/2024 00:00:00", lines[1]);
        }

        [Fact]
        public void Ignore_ColumnAttribute_IsSkipped()
        {
            var csv = new TinyCsv<AttributeOrder>();

            Assert.Equal(new[] { "Id", "Day", "amount" }, csv.Options.Columns.Select(c => c.ColumnName));
        }

        // ---------- optional ----------

        [Fact]
        public void Optional_MissingColumn_DoesNotThrow_AndKeepsTheDefault()
        {
            var result = AutoByHeader().LoadFromText("Status;Id\ns;1").Single();

            Assert.Equal(1, result.Id);
            Assert.Equal(5, result.Priority);
            Assert.Null(result.Note);
            Assert.Equal("s", result.Status);
        }

        [Fact]
        public void Optional_PresentColumn_IsRead()
        {
            var result = AutoByHeader().LoadFromText("Id;Priority;Note;Status\n1;3;n;s").Single();

            Assert.Equal((1, 3, "n", "s"), (result.Id, result.Priority, result.Note, result.Status));
        }

        [Fact]
        public void Optional_RequiredColumnMissing_StillThrows()
        {
            var exception = Assert.Throws<MissingColumnException>(() => AutoByHeader().LoadFromText("Priority\n1").ToList());

            Assert.Equal(new[] { "Id", "Status" }, exception.MissingColumns);
        }

        [Fact]
        public void Optional_ClassMap()
        {
            var csv = new TinyCsv<Order>(new OrderMap());

            var result = csv.LoadFromText("Quantity;Id\n4;1").Single();

            Assert.Equal(1, result.Id);
            Assert.Equal(7, result.Priority);
            Assert.Equal(4, result.Quantity);
            Assert.Equal(default(DateTime), result.Date);
        }

        [Fact]
        public void Optional_ClassMap_MissingRequiredColumn_Throws()
        {
            var csv = new TinyCsv<Order>(new OrderMap());

            var exception = Assert.Throws<MissingColumnException>(() => csv.LoadFromText("Id\n1").ToList());

            Assert.Equal(new[] { "Quantity" }, exception.MissingColumns);
        }

        [Fact]
        public void Optional_Attributes()
        {
            var csv = new TinyCsv<AttributeOrder>();

            var result = csv.LoadFromText("Id\n1").Single();

            Assert.Equal(1, result.Id);
            Assert.Equal(DayOfWeek.Monday, result.Day);
            Assert.Equal(1.5m, result.Amount);
        }

        [Fact]
        public void Optional_WrittenNormally()
        {
            var csv = new TinyCsv<AttributeOrder>();

            var text = csv.GetAllText(new[] { new AttributeOrder { Id = 1, Day = DayOfWeek.Friday, Amount = 2m } });

            Assert.Equal("Id;Day;amount\r\n1;Friday;2", text);
        }

        // ---------- default ----------

        [Fact]
        public void Default_EmptyField_UsesTheDefault()
        {
            var result = AutoByHeader().LoadFromText("Id;Priority;Note;Status\n1; ;;").Single();

            Assert.Equal(5, result.Priority);
            Assert.Equal("", result.Note);
            Assert.Equal("none", result.Status);
        }

        [Fact]
        public void Default_ShortRecord_UsesTheDefault()
        {
            var map = new TinyCsv<Order>(new PositionalMap());
            var result = map.LoadFromText("1").Single();

            Assert.Equal(1, result.Id);
            Assert.Equal(9, result.Priority);
        }

        [Fact]
        public void Default_ClassMap_NullableColumn()
        {
            var csv = new TinyCsv<Order>(new OrderMap());

            var result = csv.LoadFromText("Id;Quantity\n1;").Single();

            Assert.Equal(1, result.Quantity);
        }

        [Fact]
        public void Default_WithNullValues_NullValueWins()
        {
            var csv = new TinyCsv<Order>(new OrderMap());
            csv.Options.NullValues = new[] { "NULL" };

            var result = csv.LoadFromText("Id;Quantity\n1;NULL").Single();

            Assert.Null(result.Quantity);
        }

        [Fact]
        public void Default_IsConvertedToThePropertyType()
        {
            var csv = new TinyCsv<AttributeOrder>();

            var amount = csv.Options.Columns.Single(c => c.ColumnName == "amount");

            Assert.True(amount.HasDefaultValue);
            Assert.Equal(1.5m, amount.DefaultValue);
            Assert.True(amount.IsOptional);
        }

        [Fact]
        public void Default_WrongType_Throws()
        {
            Assert.Throws<ArgumentException>(() => new TinyCsv<WrongDefaultOrder>(o => { }));
            Assert.Throws<ArgumentException>(() => new TinyCsv<Order>(new WrongDefaultMap()));
        }

        [Fact]
        public void Default_NullForAValueType_Throws()
        {
            Assert.Throws<ArgumentException>(() => new TinyCsv<NullDefaultOrder>(o => { }));
        }

        [Fact]
        public void Default_NotUsedOnWrite()
        {
            var csv = new TinyCsv<Order>(new OrderMap());

            var lines = csv.GetAllLines(new[] { new Order { Id = 1 } });

            Assert.Equal("1;0;;01/01/0001 00:00:00", lines[1]);
        }

        private sealed class PositionalMap : CsvClassMap<Order>
        {
            public PositionalMap()
            {
                Map(m => m.Id);
                Map(m => m.Priority).Default(9);
            }
        }

        private sealed class WrongDefaultMap : CsvClassMap<Order>
        {
            public WrongDefaultMap()
            {
                Map(m => m.Date).Default("not a date");
            }
        }
    }
}
