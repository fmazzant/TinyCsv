namespace TinyCsv.Analyzers.Tests
{
    using System.Threading.Tasks;
    using Xunit;

    public class AttributeTests
    {
        private const string Types = @"
public class Child { public int Id { get; set; } }

public class ChildConverter : IValueConverter
{
    public string Convert(object value, object parameter, IFormatProvider provider) => """";
    public object ConvertBack(string value, Type targetType, object parameter, IFormatProvider provider) => new Child();
}

public class ConverterWithParameter : IValueConverter
{
    public ConverterWithParameter(int x) { }
    public string Convert(object value, object parameter, IFormatProvider provider) => """";
    public object ConvertBack(string value, Type targetType, object parameter, IFormatProvider provider) => null;
}

public abstract class AbstractConverter : IValueConverter
{
    public abstract string Convert(object value, object parameter, IFormatProvider provider);
    public abstract object ConvertBack(string value, Type targetType, object parameter, IFormatProvider provider);
}

public class SkipBlank : ISkipRow
{
    public Func<string, int, bool> SkipRow { get; } = (row, index) => string.IsNullOrWhiteSpace(row);
}
";

        [Fact]
        public async Task ValidModel_NoDiagnostic()
        {
            var ids = await AnalyzerTest.GetDiagnosticIdsAsync(Types + @"
[HasHeaderRecord(true)]
[MapColumnsByHeader]
[SkipRow(typeof(SkipBlank))]
public class Model
{
    [Column] public int Id { get; set; }
    [Column(name: ""Product name"")] public string Name { get; set; }
    [Column(format: ""dd/MM/yyyy"", formatProvider: typeof(System.Globalization.NumberFormatInfo))] public DateTime CreatedOn { get; set; }
    [Column(converter: typeof(ChildConverter))] public Child Child { get; set; }
    public List<int> NotAColumn { get; set; }
}");

            Assert.Empty(ids);
        }

        [Theory]
        [InlineData("[Column(converter: typeof(string))]", "implement IValueConverter")]
        [InlineData("[Column(converter: typeof(ConverterWithParameter))]", "have a public parameterless constructor")]
        [InlineData("[Column(converter: typeof(AbstractConverter))]", "not abstract")]
        [InlineData("[Column(formatProvider: typeof(ChildConverter))]", "implement IFormatProvider")]
        public async Task InvalidColumnType_TCSV004(string attribute, string message)
        {
            var diagnostics = await AnalyzerTest.GetDiagnosticsAsync(Types + @"
public class Model
{
    " + attribute + @" public string Name { get; set; }
}");

            var diagnostic = Assert.Single(diagnostics);
            Assert.Equal("TCSV004", diagnostic.Id);
            Assert.Contains(message, diagnostic.GetMessage());
        }

        [Fact]
        public async Task InvalidSkipRowType_TCSV004()
        {
            var ids = await AnalyzerTest.GetDiagnosticIdsAsync(Types + @"
[SkipRow(typeof(ChildConverter))]
public class Model
{
    [Column] public int Id { get; set; }
}");

            Assert.Equal(new[] { "TCSV004" }, ids);
        }

        [Fact]
        public async Task ColumnWithoutSetter_TCSV002()
        {
            var ids = await AnalyzerTest.GetDiagnosticIdsAsync(Types + @"
public class Model
{
    [Column] public int Id { get; set; }
    [Column] public int Double => Id * 2;
}");

            Assert.Equal(new[] { "TCSV002" }, ids);
        }

        [Fact]
        public async Task ColumnTypeWithoutConverter_TCSV003()
        {
            var ids = await AnalyzerTest.GetDiagnosticIdsAsync(Types + @"
public class Model
{
    [Column] public Child Child { get; set; }
}");

            Assert.Equal(new[] { "TCSV003" }, ids);
        }

        [Theory]
        [InlineData("[Column(index: 1)]", "[Column(index: 1)]")]
        [InlineData("[Column]", "[Column(index: 0)]")]
        [InlineData("[Column(0)]", "[Column(0)]")]
        public async Task DuplicateIndex_TCSV005(string first, string second)
        {
            var ids = await AnalyzerTest.GetDiagnosticIdsAsync(Types + @"
public class Model
{
    " + first + @" public int X { get; set; }
    " + second + @" public int Y { get; set; }
}");

            Assert.Equal(new[] { "TCSV005" }, ids);
        }

        [Fact]
        public async Task ExplicitIndexes_Different_NoDiagnostic()
        {
            var ids = await AnalyzerTest.GetDiagnosticIdsAsync(Types + @"
public class Model
{
    [Column(index: 1)] public int X { get; set; }
    [Column(index: 0)] public int Y { get; set; }
}");

            Assert.Empty(ids);
        }

        [Theory]
        [InlineData("[MapColumnsByHeader]")]
        [InlineData("[MapColumnsByHeader(true)]")]
        [InlineData("[MapColumnsByHeader, HasHeaderRecord(false)]")]
        public async Task MapColumnsByHeaderWithoutHeader_TCSV006(string attributes)
        {
            var ids = await AnalyzerTest.GetDiagnosticIdsAsync(Types + attributes + @"
public class Model
{
    [Column] public int Id { get; set; }
}");

            Assert.Equal(new[] { "TCSV006" }, ids);
        }

        [Theory]
        [InlineData("[MapColumnsByHeader, HasHeaderRecord(true)]")]
        [InlineData("[MapColumnsByHeader(false)]")]
        public async Task MapColumnsByHeader_Valid_NoDiagnostic(string attributes)
        {
            var ids = await AnalyzerTest.GetDiagnosticIdsAsync(Types + attributes + @"
public class Model
{
    [Column] public int Id { get; set; }
}");

            Assert.Empty(ids);
        }

        [Fact]
        public async Task MapColumnsByHeaderWithoutHeader_Inherited_ReportedOnce()
        {
            var ids = await AnalyzerTest.GetDiagnosticIdsAsync(Types + @"
[MapColumnsByHeader]
public class BaseModel { }

public class Model : BaseModel
{
    [Column] public int Id { get; set; }
}");

            Assert.Equal(new[] { "TCSV006" }, ids);
        }

        [Fact]
        public async Task HeaderInBaseClass_NoDiagnostic()
        {
            var ids = await AnalyzerTest.GetDiagnosticIdsAsync(Types + @"
[HasHeaderRecord(true)]
public class BaseModel { }

[MapColumnsByHeader]
public class Model : BaseModel
{
    [Column] public int Id { get; set; }
}");

            Assert.Empty(ids);
        }
    }
}
