namespace TinyCsv.Analyzers.Tests
{
    using System.Linq;
    using System.Threading.Tasks;
    using Xunit;

    public class AddColumnTests
    {
        private const string Model = @"
public class Model
{
    public int Id { get; set; }
    public string Name { get; set; }
    public decimal? Price { get; set; }
    public DateTime CreatedOn { get; set; }
    public Guid Code { get; set; }
    public byte[] Data { get; set; }
    public DayOfWeek Day { get; set; }
    public DayOfWeek? OptionalDay { get; set; }
    public Uri Link { get; set; }
    public TimeSpan Duration { get; set; }
    public int ReadOnly => Id;
    public int PrivateSetter { get; private set; }
    public int InitOnly { get; init; }
    public Child Child { get; set; }
    public List<int> Numbers { get; set; }
    public int Field;
    public string GetName() => Name;
}

public class Child { public int Id { get; set; } }

public class ChildConverter : IValueConverter
{
    public string Convert(object value, object parameter, IFormatProvider provider) => """";
    public object ConvertBack(string value, Type targetType, object parameter, IFormatProvider provider) => new Child();
}
";

        private static Task<string[]> Analyze(string columns) => AnalyzerTest.GetDiagnosticIdsAsync(Model + @"
public static class Mapping
{
    public static TinyCsv<Model> Create() => new TinyCsv<Model>(options =>
    {
        options.HasHeaderRecord = true;
" + columns + @"
    });
}");

        [Fact]
        public async Task ValidColumns_NoDiagnostic()
        {
            var ids = await Analyze(@"
        options.Columns.AddColumn(m => m.Id);
        options.Columns.AddColumn(""Product name"", m => m.Name);
        options.Columns.AddColumn(m => m.Price, System.Globalization.CultureInfo.InvariantCulture);
        options.Columns.AddColumn(m => m.CreatedOn, ""dd/MM/yyyy"");
        options.Columns.AddColumn(m => m.Code);
        options.Columns.AddColumn(m => m.Data);
        options.Columns.AddColumn(m => m.Day);
        options.Columns.AddColumn(m => m.OptionalDay);
        options.Columns.AddColumn(m => m.Link);
        options.Columns.AddColumn(m => m.Duration);
        options.Columns.AddColumn(m => m.InitOnly);
        options.Columns.AddColumn(m => (object)m.Id);
        options.Columns.AddColumn((m) => (m.Name));
        options.Columns.AddColumn(m => m.Child, new ChildConverter());");

            Assert.Empty(ids);
        }

        [Theory]
        [InlineData("m => m.Name.Length")]
        [InlineData("m => m.Child.Id")]
        [InlineData("m => m.GetName()")]
        [InlineData("m => m.Name.ToUpper()")]
        [InlineData("m => m.Field")]
        [InlineData("m => m.Id + 1")]
        [InlineData("m => 42")]
        [InlineData("m => other.Id")]
        public async Task ExpressionIsNotAProperty_TCSV001(string expression)
        {
            var ids = await Analyze("var other = new Model();\n        options.Columns.AddColumn(" + expression + ");");

            Assert.Equal(new[] { "TCSV001" }, ids);
        }

        [Theory]
        [InlineData("ReadOnly")]
        [InlineData("PrivateSetter")]
        public async Task PropertyWithoutPublicSetter_TCSV002(string property)
        {
            var diagnostics = await AnalyzerTest.GetDiagnosticsAsync(Model + @"
public static class Mapping
{
    public static TinyCsv<Model> Create() => new TinyCsv<Model>(options => options.Columns.AddColumn(m => m." + property + @"));
}");

            var diagnostic = Assert.Single(diagnostics);
            Assert.Equal("TCSV002", diagnostic.Id);
            Assert.Contains("setter", diagnostic.GetMessage());
        }

        [Theory]
        [InlineData("m => m.Child")]
        [InlineData("m => m.Numbers")]
        public async Task TypeWithoutConverter_TCSV003(string expression)
        {
            var ids = await Analyze("options.Columns.AddColumn(" + expression + ");");

            Assert.Equal(new[] { "TCSV003" }, ids);
        }

        [Fact]
        public async Task TypeWithoutConverter_WithConverter_NoDiagnostic()
        {
            var ids = await Analyze(@"
        options.Columns.AddColumn(m => m.Child, new ChildConverter());
        options.Columns.AddColumn(""Child"", m => m.Child, new ChildConverter());
        options.Columns.AddColumn(2, ""Child"", m => m.Child, new ChildConverter());");

            Assert.Empty(ids);
        }

        [Fact]
        public async Task DuplicateIndex_TCSV005()
        {
            var diagnostics = await AnalyzerTest.GetDiagnosticsAsync(Model + @"
public static class Mapping
{
    public static TinyCsv<Model> Create() => new TinyCsv<Model>(options =>
    {
        options.Columns.AddColumn(m => m.Id);
        options.Columns.AddColumn(m => m.Name);
        options.Columns.AddColumn(0, ""Child"", m => m.Child, new ChildConverter());
    });
}");

            var diagnostic = Assert.Single(diagnostics);
            Assert.Equal("TCSV005", diagnostic.Id);
            Assert.Equal("The column 'Child' has the index 0, that is already the index of the column 'Id'", diagnostic.GetMessage());
        }

        [Fact]
        public async Task ExplicitIndexes_Different_NoDiagnostic()
        {
            var ids = await Analyze(@"
        options.Columns.AddColumn(m => m.Id);
        options.Columns.AddColumn(m => m.Name);
        options.Columns.AddColumn(5, ""Child"", m => m.Child, new ChildConverter());");

            Assert.Empty(ids);
        }

        [Fact]
        public async Task ColumnsAddedInALoop_IndexesNotAnalyzed()
        {
            var ids = await Analyze(@"
        options.Columns.AddColumn(m => m.Id);
        for (int i = 0; i < 1; i++) { options.Columns.AddColumn(m => m.Name); }
        options.Columns.AddColumn(1, ""Child"", m => m.Child, new ChildConverter());");

            Assert.Empty(ids);
        }

        [Fact]
        public async Task OptionsAsVariable_Analyzed()
        {
            var ids = await AnalyzerTest.GetDiagnosticIdsAsync(Model + @"
public static class Mapping
{
    public static TinyCsv<Model> Create()
    {
        var options = new CsvOptions<Model>();
        options.Columns.AddColumn(m => m.Name.Length);
        return new TinyCsv<Model>(options);
    }
}");

            Assert.Equal(new[] { "TCSV001" }, ids);
        }

        [Fact]
        public async Task DiagnosticLocation_IsTheExpression()
        {
            var diagnostics = await Analyze_Diagnostics("options.Columns.AddColumn(m => m.Name.Length);");

            Assert.Equal("m.Name.Length", diagnostics.Single().Location.SourceTree.ToString().Substring(
                diagnostics.Single().Location.SourceSpan.Start, diagnostics.Single().Location.SourceSpan.Length));
        }

        [Fact]
        public async Task WithoutTinyCsv_NoDiagnostic()
        {
            var ids = await AnalyzerTest.GetDiagnosticIdsAsync("public class Model { public int Id; }", referenceTinyCsv: false);

            Assert.Empty(ids);
        }

        private static async Task<System.Collections.Immutable.ImmutableArray<Microsoft.CodeAnalysis.Diagnostic>> Analyze_Diagnostics(string columns)
            => await AnalyzerTest.GetDiagnosticsAsync(Model + @"
public static class Mapping
{
    public static TinyCsv<Model> Create() => new TinyCsv<Model>(options =>
    {
" + columns + @"
    });
}");
    }
}
