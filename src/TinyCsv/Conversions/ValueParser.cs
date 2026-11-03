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

namespace TinyCsv.Conversions
{
    using System;
    using System.Globalization;

    /// <summary>
    /// A fast parser of the values of a column, resolved once for a read from the format provider of the column
    /// (see ParsableValueConverter.GetParser), so the culture and the format are not looked up for each value.
    /// When it returns true the result is the same of the converter, otherwise the converter must be used:
    /// a parser handles only the common values (i.e. "123", "-1.50", "2022-04-02") and leaves the others to the converter.
    /// </summary>
    /// <typeparam name="TValue"></typeparam>
    internal abstract class ValueParser<TValue>
        where TValue : struct
    {
        /// <summary>
        /// Parses the value. Returns false when the value is left to the converter.
        /// </summary>
        public abstract bool TryParse(ReadOnlySpan<char> value, out TValue result);

        /// <summary>
        /// The number format info of the provider, when it is read only (the parsers rely on it not changing)
        /// </summary>
        protected static NumberFormatInfo GetReadOnlyNumberFormat(IFormatProvider provider)
        {
            var info = NumberFormatInfo.GetInstance(provider);
            return info.IsReadOnly ? info : null;
        }

        /// <summary>
        /// The date time format info of the provider, when it is read only (the parsers rely on it not changing)
        /// </summary>
        protected static DateTimeFormatInfo GetReadOnlyDateTimeFormat(IFormatProvider provider)
        {
            var info = DateTimeFormatInfo.GetInstance(provider);
            return info.IsReadOnly ? info : null;
        }

        /// <summary>
        /// Parses an optional '-' followed by 1 to maxDigits ASCII digits, as the integer types do with NumberStyles.Integer.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="allowMinus">true when the negative sign of the culture is '-'</param>
        /// <param name="maxDigits">the digits that cannot overflow the type</param>
        /// <param name="result"></param>
        /// <returns></returns>
        protected static bool TryParseInteger(ReadOnlySpan<char> value, bool allowMinus, int maxDigits, out long result)
        {
            result = 0;
            var i = 0;
            var negative = false;
            if (allowMinus && value.Length > 0 && value[0] == '-')
            {
                negative = true;
                i = 1;
            }
            var digits = value.Length - i;
            if (digits <= 0 || digits > maxDigits)
            {
                return false;
            }
            long number = 0;
            for (; i < value.Length; i++)
            {
                var digit = (uint)(value[i] - '0');
                if (digit > 9)
                {
                    return false;
                }
                number = number * 10 + digit;
            }
            result = negative ? -number : number;
            return true;
        }
    }

    /// <summary>
    /// int values: an optional '-' and up to 9 digits
    /// </summary>
    internal sealed class Int32ValueParser : ValueParser<int>
    {
        private readonly bool allowMinus;

        private Int32ValueParser(bool allowMinus)
        {
            this.allowMinus = allowMinus;
        }

        public static ValueParser<int> Create(IFormatProvider provider)
        {
            var info = GetReadOnlyNumberFormat(provider);
            return info == null ? null : new Int32ValueParser(info.NegativeSign == "-");
        }

        public override bool TryParse(ReadOnlySpan<char> value, out int result)
        {
            var parsed = TryParseInteger(value, allowMinus, 9, out var number);
            result = (int)number;
            return parsed;
        }
    }

    /// <summary>
    /// long values: an optional '-' and up to 18 digits
    /// </summary>
    internal sealed class Int64ValueParser : ValueParser<long>
    {
        private readonly bool allowMinus;

        private Int64ValueParser(bool allowMinus)
        {
            this.allowMinus = allowMinus;
        }

        public static ValueParser<long> Create(IFormatProvider provider)
        {
            var info = GetReadOnlyNumberFormat(provider);
            return info == null ? null : new Int64ValueParser(info.NegativeSign == "-");
        }

        public override bool TryParse(ReadOnlySpan<char> value, out long result)
        {
            return TryParseInteger(value, allowMinus, 18, out result);
        }
    }

    /// <summary>
    /// decimal values: an optional '-', the digits and an optional decimal separator followed by digits, up to 18 digits.
    /// The result keeps the scale, as decimal.Parse does (i.e. "1.10" is 1.10).
    /// </summary>
    internal sealed class DecimalValueParser : ValueParser<decimal>
    {
        private const int MaxDigits = 18;

        private readonly bool allowMinus;
        private readonly char separator;

        private DecimalValueParser(bool allowMinus, char separator)
        {
            this.allowMinus = allowMinus;
            this.separator = separator;
        }

        /// <summary>
        /// The parser, when the decimal converter parses the values with NumberStyles.Number (see DecimalConverter)
        /// and the decimal separator is a char that is not a digit, a sign or the group separator
        /// </summary>
        public static ValueParser<decimal> Create(IFormatProvider provider)
        {
            var info = GetReadOnlyNumberFormat(provider);
            if (info == null
                || info.CurrencyDecimalSeparator != info.NumberDecimalSeparator
                || info.CurrencyGroupSeparator != info.NumberGroupSeparator
                || info.NumberDecimalSeparator.Length != 1
                || info.NumberDecimalSeparator == info.NumberGroupSeparator)
            {
                return null;
            }
            var separator = info.NumberDecimalSeparator[0];
            if ((separator >= '0' && separator <= '9') || separator == '-' || separator == '+')
            {
                return null;
            }
            return new DecimalValueParser(info.NegativeSign == "-", separator);
        }

        public override bool TryParse(ReadOnlySpan<char> value, out decimal result)
        {
            result = default;
            var i = 0;
            var negative = false;
            if (allowMinus && value.Length > 0 && value[0] == '-')
            {
                negative = true;
                i = 1;
            }

            long mantissa = 0;
            var integerDigits = 0;
            var fractionDigits = 0;
            var hasSeparator = false;
            for (; i < value.Length; i++)
            {
                var c = value[i];
                var digit = (uint)(c - '0');
                if (digit <= 9)
                {
                    if (integerDigits + fractionDigits == MaxDigits)
                    {
                        return false;
                    }
                    mantissa = mantissa * 10 + digit;
                    if (hasSeparator)
                    {
                        fractionDigits++;
                    }
                    else
                    {
                        integerDigits++;
                    }
                    continue;
                }
                if (c == separator && !hasSeparator)
                {
                    hasSeparator = true;
                    continue;
                }
                return false;
            }

            // "1.", ".5" and the negative zero are left to the converter
            if (integerDigits == 0 || (hasSeparator && fractionDigits == 0) || (negative && mantissa == 0))
            {
                return false;
            }
            result = new decimal((int)mantissa, (int)(mantissa >> 32), 0, negative, (byte)fractionDigits);
            return true;
        }
    }

    /// <summary>
    /// The dates of a column with a format supported by DateTimeFormatParser
    /// </summary>
    internal sealed class DateTimeValueParser : ValueParser<DateTime>
    {
        private readonly DateTimeFormatParser parser;

        private DateTimeValueParser(DateTimeFormatParser parser)
        {
            this.parser = parser;
        }

        public static ValueParser<DateTime> Create(string format, IFormatProvider provider)
        {
            var parser = CreateFormatParser(format, provider);
            return parser == null ? null : new DateTimeValueParser(parser);
        }

        internal static DateTimeFormatParser CreateFormatParser(string format, IFormatProvider provider)
        {
            if (format == null)
            {
                return null;
            }
            var info = GetReadOnlyDateTimeFormat(provider);
            return info == null ? null : DateTimeFormatParser.Create(format, info);
        }

        public override bool TryParse(ReadOnlySpan<char> value, out DateTime result)
        {
            return parser.TryParse(value, out result);
        }
    }

    /// <summary>
    /// The dates of a column with a format supported by DateTimeFormatParser
    /// </summary>
    internal sealed class DateTimeOffsetValueParser : ValueParser<DateTimeOffset>
    {
        private readonly DateTimeFormatParser parser;

        private DateTimeOffsetValueParser(DateTimeFormatParser parser)
        {
            this.parser = parser;
        }

        public static ValueParser<DateTimeOffset> Create(string format, IFormatProvider provider)
        {
            var parser = DateTimeValueParser.CreateFormatParser(format, provider);
            return parser == null ? null : new DateTimeOffsetValueParser(parser);
        }

        public override bool TryParse(ReadOnlySpan<char> value, out DateTimeOffset result)
        {
            return parser.TryParse(value, out result);
        }
    }

#if NET6_0_OR_GREATER
    /// <summary>
    /// The dates of a column with a format supported by DateTimeFormatParser
    /// </summary>
    internal sealed class DateOnlyValueParser : ValueParser<DateOnly>
    {
        private readonly DateTimeFormatParser parser;

        private DateOnlyValueParser(DateTimeFormatParser parser)
        {
            this.parser = parser;
        }

        public static ValueParser<DateOnly> Create(string format, IFormatProvider provider)
        {
            var parser = DateTimeValueParser.CreateFormatParser(format, provider);
            return parser == null ? null : new DateOnlyValueParser(parser);
        }

        public override bool TryParse(ReadOnlySpan<char> value, out DateOnly result)
        {
            return parser.TryParse(value, out result);
        }
    }
#endif
}
