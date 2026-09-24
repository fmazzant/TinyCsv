namespace TinyCsv.Tests
{
    /// <summary>
    /// Skip reasons for tests that document bugs of the current implementation.
    /// Remove the Skip once the bug is fixed.
    /// </summary>
    internal static class KnownBug
    {
        public const string ColumnName = "Known bug: [Column(name: ...)] uses the column name to look up the property (KeyNotFoundException).";
        public const string TextEncoding = "Known bug: the TextEncoding is used to write the text stream but the StreamReader always reads UTF-8.";
        public const string BoxedExpression = "Known bug: GetPropertyName does not support a lambda whose body is a Convert (m => (object)m.Id).";
        public const string EnumUnknownValue = "Known bug: EnumConverter returns null instead of the default enum value.";
        public const string NullableEnum = "Known bug: EnumConverter throws with a nullable enum (Enum.GetNames on Nullable<T>).";
    }
}
