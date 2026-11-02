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
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Diagnostics;

    /// <summary>
    /// Reports at compile time the mappings of TinyCsv that fail at runtime:
    /// the columns defined with options.Columns.AddColumn and the models defined with the attributes.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class MappingAnalyzer : DiagnosticAnalyzer
    {
        /// <inheritdoc />
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(
            DiagnosticDescriptors.ColumnIsNotAProperty,
            DiagnosticDescriptors.PropertyNotReadWrite,
            DiagnosticDescriptors.UnsupportedColumnType,
            DiagnosticDescriptors.InvalidAttributeType,
            DiagnosticDescriptors.DuplicateColumnIndex,
            DiagnosticDescriptors.MapColumnsByHeaderWithoutHeader);

        /// <inheritdoc />
        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterCompilationStartAction(start =>
            {
                var symbols = KnownSymbols.Create(start.Compilation);
                if (symbols == null)
                {
                    // TinyCsv is not referenced
                    return;
                }

                start.RegisterSyntaxNodeAction(c => AnalyzeInvocation(c, symbols), SyntaxKind.InvocationExpression);
                start.RegisterSyntaxNodeAction(c => AnalyzeBlock(c, symbols), SyntaxKind.Block);
                start.RegisterSymbolAction(c => AnalyzeNamedType(c, symbols), SymbolKind.NamedType);
            });
        }

        #region AddColumn

        /// <summary>
        /// Analyzes options.Columns.AddColumn(m => m.Property, ...)
        /// </summary>
        private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context, KnownSymbols symbols)
        {
            var invocation = (InvocationExpressionSyntax)context.Node;
            if (!TryGetAddColumn(invocation, context.SemanticModel, symbols, out var method, context.CancellationToken))
            {
                return;
            }

            var modelType = method.ContainingType.TypeArguments[0];
            var lambda = GetArgument(invocation, method, p => IsExpressionType(p.Type))?.Expression as LambdaExpressionSyntax;
            if (lambda == null)
            {
                // an expression in a variable: it cannot be analyzed
                return;
            }

            var property = GetColumnProperty(lambda, context.SemanticModel, context.CancellationToken);
            if (property == null)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.ColumnIsNotAProperty,
                    lambda.Body.GetLocation(),
                    lambda.Body.ToString(),
                    modelType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)));
                return;
            }

            ReportReadWrite(context.ReportDiagnostic, property, lambda.Body.GetLocation());

            // the converter is chosen by the type argument of AddColumn, that is the property type unless there is a cast
            var columnType = method.TypeArguments.Length == 1 ? method.TypeArguments[0] : property.Type;
            var hasConverter = method.Parameters.Any(p => SymbolEqualityComparer.Default.Equals(p.Type, symbols.ValueConverter));
            if (!hasConverter && !symbols.HasBuiltInConverter(columnType))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.UnsupportedColumnType,
                    lambda.Body.GetLocation(),
                    columnType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat),
                    property.Name));
            }
        }

        /// <summary>
        /// Analyzes the indexes of the AddColumn calls that are statements of the same block:
        /// a column without an index has the index of the number of the columns added before it.
        /// </summary>
        private static void AnalyzeBlock(SyntaxNodeAnalysisContext context, KnownSymbols symbols)
        {
            var block = (BlockSyntax)context.Node;
            var columns = new List<(InvocationExpressionSyntax Invocation, IMethodSymbol Method)>();
            ExpressionSyntax receiver = null;

            foreach (var statement in block.Statements)
            {
                if (statement is ExpressionStatementSyntax expressionStatement
                    && expressionStatement.Expression is InvocationExpressionSyntax invocation
                    && TryGetAddColumn(invocation, context.SemanticModel, symbols, out var method, context.CancellationToken))
                {
                    var invocationReceiver = (invocation.Expression as MemberAccessExpressionSyntax)?.Expression;
                    if (receiver != null && !SyntaxFactory.AreEquivalent(receiver, invocationReceiver))
                    {
                        // columns of different options
                        return;
                    }
                    receiver = invocationReceiver;
                    columns.Add((invocation, method));
                }
                else if (statement.DescendantNodes().OfType<InvocationExpressionSyntax>()
                    .Any(i => TryGetAddColumn(i, context.SemanticModel, symbols, out _, context.CancellationToken)))
                {
                    // a column added in a loop or in a condition: the indexes are not known
                    return;
                }
            }

            if (columns.Count < 2)
            {
                return;
            }

            var indexes = new Dictionary<int, string>();
            for (int i = 0; i < columns.Count; i++)
            {
                var (invocation, method) = columns[i];
                var name = GetColumnDisplayName(invocation, method, context.SemanticModel, context.CancellationToken);

                int index = i;
                var indexArgument = GetArgument(invocation, method, p => p.Type.SpecialType == SpecialType.System_Int32);
                if (indexArgument != null)
                {
                    var constant = context.SemanticModel.GetConstantValue(indexArgument.Expression, context.CancellationToken);
                    if (!constant.HasValue || !(constant.Value is int value))
                    {
                        return;
                    }
                    // a negative index is the position of the column
                    index = value < 0 ? i : value;
                }

                if (indexes.TryGetValue(index, out var other))
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        DiagnosticDescriptors.DuplicateColumnIndex,
                        invocation.GetLocation(),
                        name, index, other));
                }
                else
                {
                    indexes.Add(index, name);
                }
            }
        }

        private static bool TryGetAddColumn(InvocationExpressionSyntax invocation, SemanticModel semanticModel, KnownSymbols symbols, out IMethodSymbol method, System.Threading.CancellationToken cancellationToken)
        {
            method = null;
            if (!(invocation.Expression is MemberAccessExpressionSyntax memberAccess) || memberAccess.Name.Identifier.ValueText != "AddColumn")
            {
                return false;
            }

            method = semanticModel.GetSymbolInfo(invocation, cancellationToken).Symbol as IMethodSymbol;
            return method != null
                && method.Name == "AddColumn"
                && SymbolEqualityComparer.Default.Equals(method.ContainingType?.OriginalDefinition, symbols.CsvOptionsColumns);
        }

        private static ArgumentSyntax GetArgument(InvocationExpressionSyntax invocation, IMethodSymbol method, System.Func<IParameterSymbol, bool> predicate)
        {
            var arguments = invocation.ArgumentList.Arguments;
            for (int i = 0; i < arguments.Count; i++)
            {
                var argument = arguments[i];
                IParameterSymbol parameter;
                if (argument.NameColon != null)
                {
                    parameter = method.Parameters.FirstOrDefault(p => p.Name == argument.NameColon.Name.Identifier.ValueText);
                }
                else
                {
                    parameter = i < method.Parameters.Length ? method.Parameters[i] : null;
                }

                if (parameter != null && predicate(parameter))
                {
                    return argument;
                }
            }
            return null;
        }

        private static bool IsExpressionType(ITypeSymbol type)
            => type is INamedTypeSymbol named
               && named.Name == "Expression"
               && named.IsGenericType
               && named.ContainingNamespace?.ToDisplayString() == "System.Linq.Expressions";

        /// <summary>
        /// Returns the property of m => m.Property (or m => (object)m.Property), null when the expression is something else
        /// </summary>
        private static IPropertySymbol GetColumnProperty(LambdaExpressionSyntax lambda, SemanticModel semanticModel, System.Threading.CancellationToken cancellationToken)
        {
            if (!(lambda.Body is ExpressionSyntax body))
            {
                return null;
            }

            var lambdaParameter = lambda is SimpleLambdaExpressionSyntax simple
                ? simple.Parameter
                : (lambda as ParenthesizedLambdaExpressionSyntax)?.ParameterList.Parameters.FirstOrDefault();
            if (lambdaParameter == null)
            {
                return null;
            }

            body = Unwrap(body);
            if (!(body is MemberAccessExpressionSyntax memberAccess) || !memberAccess.IsKind(SyntaxKind.SimpleMemberAccessExpression))
            {
                return null;
            }

            var target = Unwrap(memberAccess.Expression);
            if (!(target is IdentifierNameSyntax identifier) || identifier.Identifier.ValueText != lambdaParameter.Identifier.ValueText)
            {
                return null;
            }

            var property = semanticModel.GetSymbolInfo(memberAccess, cancellationToken).Symbol as IPropertySymbol;
            return property != null && !property.IsIndexer && !property.IsStatic ? property : null;
        }

        /// <summary>
        /// Removes the parentheses and the casts, that are a Convert in the expression tree
        /// </summary>
        private static ExpressionSyntax Unwrap(ExpressionSyntax expression)
        {
            while (true)
            {
                switch (expression)
                {
                    case ParenthesizedExpressionSyntax parenthesized:
                        expression = parenthesized.Expression;
                        break;
                    case CastExpressionSyntax cast:
                        expression = cast.Expression;
                        break;
                    default:
                        return expression;
                }
            }
        }

        private static string GetColumnDisplayName(InvocationExpressionSyntax invocation, IMethodSymbol method, SemanticModel semanticModel, System.Threading.CancellationToken cancellationToken)
        {
            var nameArgument = GetArgument(invocation, method, p => p.Type.SpecialType == SpecialType.System_String && p.Name == "columnName");
            if (nameArgument != null && semanticModel.GetConstantValue(nameArgument.Expression, cancellationToken).Value is string name)
            {
                return name;
            }

            var lambda = GetArgument(invocation, method, p => IsExpressionType(p.Type))?.Expression as LambdaExpressionSyntax;
            var property = lambda != null ? GetColumnProperty(lambda, semanticModel, cancellationToken) : null;
            return property?.Name ?? invocation.ArgumentList.ToString();
        }

        #endregion

        #region Attributes

        /// <summary>
        /// Analyzes a model with the attributes
        /// </summary>
        private static void AnalyzeNamedType(SymbolAnalysisContext context, KnownSymbols symbols)
        {
            var type = (INamedTypeSymbol)context.Symbol;
            if (type.TypeKind != TypeKind.Class && type.TypeKind != TypeKind.Struct)
            {
                return;
            }

            AnalyzeModelAttributes(context, symbols, type);
            AnalyzeColumnAttributes(context, symbols, type);
        }

        private static void AnalyzeModelAttributes(SymbolAnalysisContext context, KnownSymbols symbols, INamedTypeSymbol type)
        {
            foreach (var attribute in type.GetAttributes())
            {
                if (SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, symbols.SkipRowAttribute)
                    && attribute.ConstructorArguments.Length == 1
                    && attribute.ConstructorArguments[0].Value is INamedTypeSymbol skipRowType)
                {
                    ReportInvalidType(context.ReportDiagnostic, attribute, skipRowType, symbols.SkipRow, "[SkipRow]");
                }
            }

            // the attributes are inherited, as Attribute.GetCustomAttributes(type) reads them
            var mapByHeader = FindAttribute(type, symbols.MapColumnsByHeaderAttribute);
            if (mapByHeader.Attribute == null || !GetBoolArgument(mapByHeader.Attribute, defaultValue: true))
            {
                return;
            }

            var hasHeader = FindAttribute(type, symbols.HasHeaderRecordAttribute);
            if (hasHeader.Attribute != null && GetBoolArgument(hasHeader.Attribute, defaultValue: false))
            {
                return;
            }

            // reported once, on the type that declares the wrong attribute: a derived type without attributes is not reported again
            AttributeData reported = null;
            if (SymbolEqualityComparer.Default.Equals(mapByHeader.DeclaringType, type))
            {
                reported = mapByHeader.Attribute;
            }
            else if (hasHeader.Attribute != null && SymbolEqualityComparer.Default.Equals(hasHeader.DeclaringType, type))
            {
                reported = hasHeader.Attribute;
            }

            if (reported != null)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.MapColumnsByHeaderWithoutHeader,
                    GetLocation(reported, context.CancellationToken) ?? type.Locations.FirstOrDefault(),
                    type.Name));
            }
        }

        private static Location GetLocation(AttributeData attribute, System.Threading.CancellationToken cancellationToken)
            => attribute.ApplicationSyntaxReference?.GetSyntax(cancellationToken).GetLocation();

        private static void AnalyzeColumnAttributes(SymbolAnalysisContext context, KnownSymbols symbols, INamedTypeSymbol type)
        {
            var columns = new List<(IPropertySymbol Property, AttributeData Attribute)>();
            foreach (var property in type.GetMembers().OfType<IPropertySymbol>())
            {
                var column = property.GetAttributes().FirstOrDefault(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, symbols.ColumnAttribute));
                if (column != null)
                {
                    columns.Add((property, column));
                }
            }

            if (columns.Count == 0)
            {
                return;
            }

            var indexes = new Dictionary<int, string>();
            var position = 0;
            foreach (var (property, column) in columns)
            {
                var location = GetLocation(column, context.CancellationToken) ?? property.Locations.FirstOrDefault();

                var converter = GetTypeArgument(column, "converter");
                var formatProvider = GetTypeArgument(column, "formatProvider");

                if (converter != null)
                {
                    ReportInvalidType(context.ReportDiagnostic, column, converter, symbols.ValueConverter, "converter");
                }
                if (formatProvider != null)
                {
                    ReportInvalidType(context.ReportDiagnostic, column, formatProvider, symbols.FormatProvider, "formatProvider");
                }

                ReportReadWrite(context.ReportDiagnostic, property, location);

                if (converter == null && !symbols.HasBuiltInConverter(property.Type))
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        DiagnosticDescriptors.UnsupportedColumnType,
                        location,
                        property.Type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat),
                        property.Name));
                }

                var index = GetIntArgument(column, "index", -1);
                if (index < 0)
                {
                    index = position;
                }
                position++;

                if (indexes.TryGetValue(index, out var other))
                {
                    context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.DuplicateColumnIndex, location, property.Name, index, other));
                }
                else
                {
                    indexes.Add(index, property.Name);
                }
            }
        }

        private static (AttributeData Attribute, INamedTypeSymbol DeclaringType) FindAttribute(INamedTypeSymbol type, INamedTypeSymbol attributeType)
        {
            for (var current = type; current != null; current = current.BaseType)
            {
                var attribute = current.GetAttributes().FirstOrDefault(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, attributeType));
                if (attribute != null)
                {
                    return (attribute, current);
                }
            }
            return (null, null);
        }

        private static bool GetBoolArgument(AttributeData attribute, bool defaultValue)
            => attribute.ConstructorArguments.Length > 0 && attribute.ConstructorArguments[0].Value is bool value ? value : defaultValue;

        private static INamedTypeSymbol GetTypeArgument(AttributeData attribute, string name)
            => GetArgument(attribute, name).Value as INamedTypeSymbol;

        private static int GetIntArgument(AttributeData attribute, string name, int defaultValue)
            => GetArgument(attribute, name).Value is int value ? value : defaultValue;

        /// <summary>
        /// The argument of a constructor parameter: the optional parameters that are not set have their default value
        /// </summary>
        private static TypedConstant GetArgument(AttributeData attribute, string name)
        {
            var parameters = attribute.AttributeConstructor?.Parameters ?? ImmutableArray<IParameterSymbol>.Empty;
            for (int i = 0; i < parameters.Length && i < attribute.ConstructorArguments.Length; i++)
            {
                if (parameters[i].Name == name)
                {
                    return attribute.ConstructorArguments[i];
                }
            }
            return default;
        }

        private static void ReportInvalidType(System.Action<Diagnostic> report, AttributeData attribute, INamedTypeSymbol type, INamedTypeSymbol requiredInterface, string parameterName)
        {
            if (type.TypeKind == TypeKind.Error || requiredInterface == null)
            {
                return;
            }

            string problem = null;
            if (!type.AllInterfaces.Contains(requiredInterface, SymbolEqualityComparer.Default))
            {
                problem = $"implement {requiredInterface.Name}";
            }
            else if (type.IsAbstract || type.TypeKind == TypeKind.Interface)
            {
                problem = "be a class that can be created, not abstract";
            }
            else if (type.TypeKind == TypeKind.Class
                && !type.InstanceConstructors.Any(c => c.Parameters.Length == 0 && c.DeclaredAccessibility == Accessibility.Public))
            {
                problem = "have a public parameterless constructor";
            }

            if (problem != null)
            {
                var location = attribute.ApplicationSyntaxReference?.GetSyntax().GetLocation();
                report(Diagnostic.Create(
                    DiagnosticDescriptors.InvalidAttributeType,
                    location,
                    type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat),
                    parameterName,
                    problem));
            }
        }

        #endregion

        private static void ReportReadWrite(System.Action<Diagnostic> report, IPropertySymbol property, Location location)
        {
            // TinyCsv uses the public accessors (an init accessor is a public set method for the reflection, so it can be read)
            if (property.SetMethod == null || property.SetMethod.DeclaredAccessibility != Accessibility.Public)
            {
                report(Diagnostic.Create(DiagnosticDescriptors.PropertyNotReadWrite, location, property.Name, "setter", "read"));
            }

            if (property.GetMethod == null || property.GetMethod.DeclaredAccessibility != Accessibility.Public)
            {
                report(Diagnostic.Create(DiagnosticDescriptors.PropertyNotReadWrite, location, property.Name, "getter", "written"));
            }
        }
    }
}
