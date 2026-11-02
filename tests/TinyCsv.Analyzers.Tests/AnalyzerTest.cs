namespace TinyCsv.Analyzers.Tests
{
    using System;
    using System.Collections.Immutable;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.Diagnostics;
    using Xunit;

    /// <summary>
    /// Compiles a source with TinyCsv and runs the analyzers on it
    /// </summary>
    internal static class AnalyzerTest
    {
        private const string Usings = @"
using System;
using System.Collections.Generic;
using TinyCsv;
using TinyCsv.Attributes;
using TinyCsv.Conversions;
";

        private static readonly ImmutableArray<MetadataReference> References =
            ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES"))
                .Split(Path.PathSeparator)
                .Where(path => !string.IsNullOrEmpty(path))
                .Append(typeof(TinyCsv<>).Assembly.Location)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Select(path => (MetadataReference)MetadataReference.CreateFromFile(path))
                .ToImmutableArray();

        /// <summary>
        /// Returns the ids of the diagnostics of the analyzers, in the order of their position
        /// </summary>
        public static async Task<string[]> GetDiagnosticIdsAsync(string source, bool referenceTinyCsv = true)
        {
            var diagnostics = await GetDiagnosticsAsync(source, referenceTinyCsv);
            return diagnostics.Select(d => d.Id).ToArray();
        }

        public static async Task<ImmutableArray<Diagnostic>> GetDiagnosticsAsync(string source, bool referenceTinyCsv = true)
        {
            var tree = CSharpSyntaxTree.ParseText(Usings + source, new CSharpParseOptions(LanguageVersion.Latest));
            var references = referenceTinyCsv
                ? References
                : References.Where(r => !string.Equals(Path.GetFileName(r.Display), "Mafe.TinyCsv.dll", StringComparison.OrdinalIgnoreCase)).ToImmutableArray();

            var compilation = CSharpCompilation.Create(
                "AnalyzerTest",
                new[] { tree },
                references,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Disable));

            if (referenceTinyCsv)
            {
                var errors = compilation.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).ToArray();
                Assert.True(errors.Length == 0, "The source does not compile: " + string.Join(Environment.NewLine, errors.Select(e => e.ToString())));
            }

            var diagnostics = await compilation
                .WithAnalyzers(ImmutableArray.Create<DiagnosticAnalyzer>(new MappingAnalyzer()))
                .GetAnalyzerDiagnosticsAsync();

            return diagnostics.OrderBy(d => d.Location.SourceSpan.Start).ToImmutableArray();
        }
    }
}
