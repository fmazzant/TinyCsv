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
    using Microsoft.CodeAnalysis;

    /// <summary>
    /// The TinyCsv types used by the analyzers, resolved once for each compilation
    /// </summary>
    internal sealed class KnownSymbols
    {
        /// <summary>
        /// The types of ValueConverterFactory.CreateValueConverterByType, by metadata name
        /// </summary>
        private static readonly HashSet<string> BuiltInConverterTypes = new HashSet<string>
        {
            "System.DateTimeOffset",
            "System.TimeSpan",
            "System.Numerics.BigInteger",
            "System.Uri",
            "System.Guid",
            "System.Enum",
            "System.DateOnly",
            "System.TimeOnly",
        };

        public INamedTypeSymbol CsvOptionsColumns { get; private set; }
        public INamedTypeSymbol ValueConverter { get; private set; }
        public INamedTypeSymbol FormatProvider { get; private set; }
        public INamedTypeSymbol SkipRow { get; private set; }
        public INamedTypeSymbol ColumnAttribute { get; private set; }
        public INamedTypeSymbol SkipRowAttribute { get; private set; }
        public INamedTypeSymbol MapColumnsByHeaderAttribute { get; private set; }
        public INamedTypeSymbol HasHeaderRecordAttribute { get; private set; }

        /// <summary>
        /// Returns null when TinyCsv is not referenced by the compilation
        /// </summary>
        public static KnownSymbols Create(Compilation compilation)
        {
            var columns = compilation.GetTypeByMetadataName("TinyCsv.CsvOptionsColumns`1");
            var valueConverter = compilation.GetTypeByMetadataName("TinyCsv.Conversions.IValueConverter");
            if (columns == null || valueConverter == null)
            {
                return null;
            }

            return new KnownSymbols
            {
                CsvOptionsColumns = columns,
                ValueConverter = valueConverter,
                FormatProvider = compilation.GetTypeByMetadataName("System.IFormatProvider"),
                SkipRow = compilation.GetTypeByMetadataName("TinyCsv.Attributes.ISkipRow"),
                ColumnAttribute = compilation.GetTypeByMetadataName("TinyCsv.Attributes.ColumnAttribute"),
                SkipRowAttribute = compilation.GetTypeByMetadataName("TinyCsv.Attributes.SkipRowAttribute"),
                MapColumnsByHeaderAttribute = compilation.GetTypeByMetadataName("TinyCsv.Attributes.MapColumnsByHeaderAttribute"),
                HasHeaderRecordAttribute = compilation.GetTypeByMetadataName("TinyCsv.Attributes.HasHeaderRecordAttribute"),
            };
        }

        /// <summary>
        /// True when TinyCsv has a converter for the type, as ValueConverterFactory.CreateValueConverterByType.
        /// The types that are not known at compile time (type parameters, object, dynamic) are not reported.
        /// </summary>
        public bool HasBuiltInConverter(ITypeSymbol type)
        {
            if (type is INamedTypeSymbol named && named.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T)
            {
                type = named.TypeArguments[0];
            }

            switch (type.TypeKind)
            {
                case TypeKind.Enum:
                case TypeKind.TypeParameter:
                case TypeKind.Dynamic:
                case TypeKind.Error:
                    return true;
                case TypeKind.Array:
                    return ((IArrayTypeSymbol)type).ElementType.SpecialType == SpecialType.System_Byte;
            }

            switch (type.SpecialType)
            {
                case SpecialType.System_Object:
                case SpecialType.System_Boolean:
                case SpecialType.System_Char:
                case SpecialType.System_SByte:
                case SpecialType.System_Byte:
                case SpecialType.System_Int16:
                case SpecialType.System_UInt16:
                case SpecialType.System_Int32:
                case SpecialType.System_UInt32:
                case SpecialType.System_Int64:
                case SpecialType.System_UInt64:
                case SpecialType.System_Decimal:
                case SpecialType.System_Single:
                case SpecialType.System_Double:
                case SpecialType.System_String:
                case SpecialType.System_DateTime:
                case SpecialType.System_Enum:
                    return true;
            }

            return BuiltInConverterTypes.Contains(type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat).Replace("global::", string.Empty));
        }
    }
}
