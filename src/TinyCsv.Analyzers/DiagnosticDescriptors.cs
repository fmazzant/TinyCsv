/// <summary>
///
/// The MIT License (MIT)
///
/// Copyright (c) 2022 Federico Mazzanti
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

namespace TinyCsv.Analyzers
{
    using Microsoft.CodeAnalysis;

    /// <summary>
    /// The diagnostics reported by the TinyCsv analyzers
    /// </summary>
    internal static class DiagnosticDescriptors
    {
        private const string Category = "TinyCsv.Mapping";
        private const string HelpLink = "https://github.com/fmazzant/TinyCsv#compile-time-analyzers";

        /// <summary>
        /// TCSV001: the expression of AddColumn is not a property of the model
        /// </summary>
        public static readonly DiagnosticDescriptor ColumnIsNotAProperty = new DiagnosticDescriptor(
            id: "TCSV001",
            title: "The column must be a property of the model",
            messageFormat: "The column '{0}' is not a property of '{1}': use an expression like 'm => m.Property'",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: "AddColumn reads and writes a property of the model, so its expression must be a property of the lambda parameter. A nested property, a field, a method call or a computed value throws an exception at runtime.",
            helpLinkUri: HelpLink);

        /// <summary>
        /// TCSV002: the property of the column has no public setter or getter
        /// </summary>
        public static readonly DiagnosticDescriptor PropertyNotReadWrite = new DiagnosticDescriptor(
            id: "TCSV002",
            title: "The property of the column must have a public getter and setter",
            messageFormat: "The property '{0}' has no public {1}: the column cannot be {2}",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "A column without a public setter cannot be read (the read throws an InvalidColumnValueException), a column without a public getter cannot be written.",
            helpLinkUri: HelpLink);

        /// <summary>
        /// TCSV003: the type of the column has no converter
        /// </summary>
        public static readonly DiagnosticDescriptor UnsupportedColumnType = new DiagnosticDescriptor(
            id: "TCSV003",
            title: "The type of the column needs a converter",
            messageFormat: "The type '{0}' of the column '{1}' has no built-in converter: set a converter that implements IValueConverter",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "A column of a type without a built-in converter is written with ToString() and cannot be read back: the read throws an InvalidColumnValueException.",
            helpLinkUri: HelpLink);

        /// <summary>
        /// TCSV004: the type in an attribute does not implement the required interface
        /// </summary>
        public static readonly DiagnosticDescriptor InvalidAttributeType = new DiagnosticDescriptor(
            id: "TCSV004",
            title: "The type must implement the interface and have a public parameterless constructor",
            messageFormat: "The type '{0}' of '{1}' must {2}",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: "The types of the converter and formatProvider parameters of [Column], and the type of [SkipRow], are created when the model is used: a wrong type throws an exception at runtime.",
            helpLinkUri: HelpLink);

        /// <summary>
        /// TCSV005: two columns have the same index
        /// </summary>
        public static readonly DiagnosticDescriptor DuplicateColumnIndex = new DiagnosticDescriptor(
            id: "TCSV005",
            title: "Two columns have the same index",
            messageFormat: "The column '{0}' has the index {1}, that is already the index of the column '{2}'",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "The index of a column is the position of its value in the row. Two columns with the same index read the same value.",
            helpLinkUri: HelpLink);

        /// <summary>
        /// TCSV006: MapColumnsByHeader without the header record
        /// </summary>
        public static readonly DiagnosticDescriptor MapColumnsByHeaderWithoutHeader = new DiagnosticDescriptor(
            id: "TCSV006",
            title: "MapColumnsByHeader requires HasHeaderRecord",
            messageFormat: "'{0}' maps the columns by header, but it has no header record: add [HasHeaderRecord(true)]",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: "MapColumnsByHeader reads the names of the columns from the header record: without HasHeaderRecord, the read throws an InvalidOperationException.",
            helpLinkUri: HelpLink);
    }
}
