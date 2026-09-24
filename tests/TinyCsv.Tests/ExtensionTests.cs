namespace TinyCsv.Tests
{
    using System;
    using System.Linq;
    using System.Linq.Expressions;
    using TinyCsv.Exceptions;
    using TinyCsv.Extensions;
    using TinyCsv.Helpers;
    using Xunit;

    public class ExtensionTests
    {
        public class Nested
        {
            public Person Person { get; set; }
        }

        private static CsvOptions<Person> Options(Action<CsvOptions<Person>> configure = null)
        {
            var options = Csv.Person().Options;
            configure?.Invoke(options);
            return options;
        }

        [Fact]
        public void TypeExtensions_IsNullable()
        {
            Assert.True(typeof(int?).IsNullable());
            Assert.True(typeof(DateTime?).IsNullable());
            Assert.False(typeof(int).IsNullable());
            Assert.False(typeof(string).IsNullable());
        }

        [Fact]
        public void ExpressionExtensions_GetPropertyName()
        {
            Expression<Func<Person, string>> member = m => m.Name;

            Assert.Equal("Name", member.GetPropertyName());
        }

        [Fact(Skip = KnownBug.BoxedExpression)]
        public void ExpressionExtensions_GetPropertyName_Boxed()
        {
            Expression<Func<Person, object>> boxed = m => m.Id;

            Assert.Equal("Id", boxed.GetPropertyName());
        }

        [Fact]
        public void ExpressionExtensions_NestedMember_Throws()
        {
            Expression<Func<Nested, string>> nested = m => m.Person.Name;

            Assert.Throws<ArgumentException>(() => nested.GetPropertyName());
        }

        [Fact]
        public void ExpressionExtensions_NotAMember_Throws()
        {
            Expression<Func<Person, string>> call = m => m.Name.ToUpper();

            Assert.Throws<ArgumentException>(() => call.GetPropertyName());
        }

        [Fact]
        public void StringExtensions_TrimData()
        {
            Assert.Equal("a", " \"a\" ".TrimData(Options(o => o.TrimData = true)));
            Assert.Equal(" \"a\" ", " \"a\" ".TrimData(Options()));
            Assert.Equal("a", "'a'".TrimData(Options()));
            Assert.Null(((string)null).TrimData(Options()));
        }

        [Fact]
        public void StringExtensions_SplitLine()
        {
            var values = "1;\"a;b\";c".SplitLine(Options());

            Assert.Equal(new[] { "1", "\"a;b\"", "c" }, values);
        }

        [Fact]
        public void StringExtensions_SplitLine_ValidateColumnCount()
        {
            var options = Options(o => o.ValidateColumnCount = true);

            Assert.Equal(3, "1;a;b".SplitLine(options).Length);
            Assert.Throws<InvalidColumnCountException>(() => "1;a".SplitLine(options));
        }

        [Fact]
        public void StringExtensions_SplitLine_EscapedQuotesNotAllowed()
        {
            Assert.Throws<InvalidColumnValueException>(() => "1;\"a\\\"b\";c".SplitLine(Options()));
            Assert.NotEmpty("1;\"a\\\"b\";c".SplitLine(Options(o => o.AllowBackSlashToEscapeQuote = true)));
        }

        [Fact]
        public void StringExtensions_SkipRow()
        {
            var options = Options(o => o.SkipRow = (row, index) => row == "custom");

            Assert.False("1;a".SkipRow(0, options));
            Assert.True("# comment".SkipRow(0, options));
            Assert.True("custom".SkipRow(0, options));
            Assert.Throws<NotAllowCommentException>(() => "# comment".SkipRow(0, Options(o => o.AllowComment = false)));
        }

#pragma warning disable CS0618 // SkipEmptyRows is obsolete
        [Fact]
        public void StringExtensions_SkipRow_SkipEmptyRows()
        {
            Assert.True("  ".SkipRow(0, Options(o => o.SkipEmptyRows = true)));
            Assert.False("  ".SkipRow(0, Options()));
        }
#pragma warning restore CS0618

        [Theory]
        [InlineData("plain", "plain")]
        [InlineData("a;b", "\"a;b\"")]
        [InlineData("say \"hi\"", "\"say \"\"hi\"\"\"")]
        [InlineData("line1\nline2", "\"line1\nline2\"")]
        [InlineData("line1\r\nline2", "\"line1\r\nline2\"")]
        [InlineData("", "")]
        [InlineData(null, null)]
        public void StringExtensions_EnclosedInQuotesIfNecessary(string value, string expected)
        {
            Assert.Equal(expected, value.EnclosedInQuotesIfNecessary(Options()));
        }

        [Theory]
        [InlineData("plain", "plain")]
        [InlineData("C:\\temp", "C:\\\\temp")]
        [InlineData("say \"hi\"", "\"say \\\"hi\\\"\"")]
        public void StringExtensions_EnclosedInQuotesIfNecessary_BackSlash(string value, string expected)
        {
            Assert.Equal(expected, value.EnclosedInQuotesIfNecessary(Options(o => o.AllowBackSlashToEscapeQuote = true)));
        }

        [Fact]
        public void StringExtensions_EnclosedInQuotesIfNecessary_QuotingDisabled()
        {
            Assert.Equal("a;b", "a;b".EnclosedInQuotesIfNecessary(Options(o => o.AllowRowEnclosedInDoubleQuotesValues = false)));
        }

        [Fact]
        public void StringArrayExtensions_GetModelFromStringArray()
        {
            var model = new[] { "5", "Mario" }.GetModelFromStringArray(Options());

            Assert.Equal(5, model.Id);
            Assert.Equal("Mario", model.Name);
            Assert.Null(model.City);
        }

        [Fact]
        public void GenericTypeExtensions_FieldsAndLine()
        {
            var person = new Person { Id = 1, Name = "a;b", City = null };
            var options = Options();

            Assert.Equal(new[] { "1", "\"a;b\"", "" }, person.GetFieldsFromGenericType(options));
            Assert.Equal("1;\"a;b\";", person.GetLineFromGenericType(options));
        }

        [Fact]
        public void PropertyHelper_GetterAndSetter()
        {
            var properties = PropertyHelper<Person>.GetPropertiesExpressionTree(typeof(Person));
            var person = new Person();

            properties["Name"].Setter(person, "Mario");

            Assert.Equal(new[] { "City", "Id", "Name" }, properties.Keys.OrderBy(x => x));
            Assert.Equal("Mario", properties["Name"].Getter(person));
        }

#if NET5_0_OR_GREATER
        [Fact]
        public async System.Threading.Tasks.Task AsyncEnumerableExtensions_ToListAsync()
        {
            // called explicitly: on .NET 10 System.Linq.AsyncEnumerable.ToListAsync makes the extension call ambiguous
            var result = await AsyncEnumerableExtensions.ToListAsync(Csv.Person().LoadFromTextAsync("1;a;b\n2;c;d"));

            Assert.Equal(new[] { 1, 2 }, result.Select(x => x.Id));
        }
#endif
    }
}
