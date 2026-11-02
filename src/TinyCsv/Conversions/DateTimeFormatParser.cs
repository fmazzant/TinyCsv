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
    using System.Collections.Generic;
    using System.Globalization;

    /// <summary>
    /// A fast parser of the dates with a fixed width format, i.e. "yyyy-MM-dd", "dd/MM/yyyy HH:mm:ss" or "yyyy-MM-ddTHH:mm".
    /// The format can contain yyyy, MM, dd, HH, mm, ss, the date ('/') and time (':') separators of the culture
    /// and the literal chars '-', '.', ',', ' ' and 'T'. The calendar of the culture must be the Gregorian calendar.
    /// When the parser returns true the result is the same of DateTime.TryParseExact with DateTimeStyles.None,
    /// otherwise (i.e. a format that is not supported or a value that is not valid) DateTime.TryParseExact must be used.
    /// </summary>
    internal sealed class DateTimeFormatParser
    {
        private enum Part
        {
            Literal,
            Year,
            Month,
            Day,
            Hour,
            Minute,
            Second,
        }

        private readonly Part[] parts;
        private readonly string[] literals;
        private readonly int length;

        private DateTimeFormatParser(Part[] parts, string[] literals, int length)
        {
            this.parts = parts;
            this.literals = literals;
            this.length = length;
        }

        /// <summary>
        /// True when the format has a time part or the time separator
        /// </summary>
        public bool HasTime { get; private set; }

        /// <summary>
        /// The parser of a format, or null when the format is not supported, for a format info
        /// </summary>
        internal sealed class Cache
        {
            public string Format;
            public DateTimeFormatInfo Info;
            public string DateSeparator;
            public string TimeSeparator;
            public Calendar Calendar;
            public DateTimeFormatParser Parser;

            public bool IsFor(string format, DateTimeFormatInfo info)
            {
                // a format info that is not read only may change
                return ReferenceEquals(Info, info) && Format == format
                    && (info.IsReadOnly || (DateSeparator == info.DateSeparator && TimeSeparator == info.TimeSeparator && ReferenceEquals(Calendar, info.Calendar)));
            }
        }

        /// <summary>
        /// Returns the parser of the format, using the cache of the last format
        /// </summary>
        /// <param name="cache">the cache, updated when it is for another format</param>
        /// <param name="format"></param>
        /// <param name="provider"></param>
        /// <returns>the parser, or null when the format is not supported</returns>
        public static DateTimeFormatParser Get(ref Cache cache, string format, IFormatProvider provider)
        {
            var info = DateTimeFormatInfo.GetInstance(provider);
            var current = cache;
            if (current == null || !current.IsFor(format, info))
            {
                current = new Cache
                {
                    Format = format,
                    Info = info,
                    DateSeparator = info.DateSeparator,
                    TimeSeparator = info.TimeSeparator,
                    Calendar = info.Calendar,
                    Parser = Create(format, info),
                };
                cache = current;
            }
            return current.Parser;
        }

        /// <summary>
        /// Creates the parser of the format, or returns null when the format is not supported
        /// </summary>
        /// <param name="format"></param>
        /// <param name="info"></param>
        /// <returns></returns>
        public static DateTimeFormatParser Create(string format, DateTimeFormatInfo info)
        {
            // a single char is a standard format
            if (format == null || format.Length < 2 || info == null || !(info.Calendar is GregorianCalendar))
            {
                return null;
            }

            var parts = new List<Part>();
            var literals = new List<string>();
            var length = 0;
            var i = 0;
            while (i < format.Length)
            {
                var c = format[i];
                var count = 1;
                while (i + count < format.Length && format[i + count] == c)
                {
                    count++;
                }

                var part = GetPart(c, count);
                if (part.HasValue)
                {
                    parts.Add(part.Value);
                    literals.Add(null);
                    length += count;
                    i += count;
                    continue;
                }

                string literal;
                switch (c)
                {
                    case '/': literal = info.DateSeparator; break;
                    case ':': literal = info.TimeSeparator; break;
                    case '-':
                    case '.':
                    case ',':
                    case ' ':
                    case 'T': literal = c.ToString(); break;
                    default: return null;
                }
                if (string.IsNullOrEmpty(literal) || HasDigitOrWhiteSpace(literal) && c != ' ')
                {
                    return null;
                }
                parts.Add(Part.Literal);
                literals.Add(literal);
                length += literal.Length;
                i++;
            }

            var hasTime = format.IndexOf(':') >= 0 || parts.Contains(Part.Hour) || parts.Contains(Part.Minute) || parts.Contains(Part.Second);
            return new DateTimeFormatParser(parts.ToArray(), literals.ToArray(), length) { HasTime = hasTime };
        }

        private static Part? GetPart(char c, int count)
        {
            switch (c)
            {
                case 'y': return count == 4 ? Part.Year : (Part?)null;
                case 'M': return count == 2 ? Part.Month : (Part?)null;
                case 'd': return count == 2 ? Part.Day : (Part?)null;
                case 'H': return count == 2 ? Part.Hour : (Part?)null;
                case 'm': return count == 2 ? Part.Minute : (Part?)null;
                case 's': return count == 2 ? Part.Second : (Part?)null;
                default: return null;
            }
        }

        private static bool HasDigitOrWhiteSpace(string value)
        {
            foreach (var c in value)
            {
                if ((c >= '0' && c <= '9') || char.IsWhiteSpace(c))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Parses the value. Returns false when the value is not valid: then DateTime.TryParseExact must be used.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        public bool TryParse(ReadOnlySpan<char> value, out DateTime result)
        {
            result = default;
            if (value.Length != length)
            {
                return false;
            }

            int year = -1, month = -1, day = -1, hour = -1, minute = -1, second = -1;
            var position = 0;
            for (int p = 0; p < parts.Length; p++)
            {
                var part = parts[p];
                if (part == Part.Literal)
                {
                    var literal = literals[p];
                    for (int k = 0; k < literal.Length; k++)
                    {
                        if (value[position + k] != literal[k])
                        {
                            return false;
                        }
                    }
                    position += literal.Length;
                    continue;
                }

                var digits = part == Part.Year ? 4 : 2;
                var number = 0;
                for (int k = 0; k < digits; k++)
                {
                    var c = value[position + k];
                    if (c < '0' || c > '9')
                    {
                        return false;
                    }
                    number = number * 10 + (c - '0');
                }
                position += digits;

                // a part twice (i.e. "dd/MM/dd") is left to DateTime.TryParseExact, a missing time part is 0
                switch (part)
                {
                    case Part.Year: if (year >= 0) return false; year = number; break;
                    case Part.Month: if (month >= 0) return false; month = number; break;
                    case Part.Day: if (day >= 0) return false; day = number; break;
                    case Part.Hour: if (hour >= 0) return false; hour = number; break;
                    case Part.Minute: if (minute >= 0) return false; minute = number; break;
                    case Part.Second: if (second >= 0) return false; second = number; break;
                }
            }

            // a date without year, month or day uses the current date in DateTime.TryParseExact
            if (year < 1 || month < 1 || month > 12 || day < 1 || day > DateTime.DaysInMonth(year, month)
                || hour > 23 || minute > 59 || second > 59)
            {
                return false;
            }

            result = new DateTime(year, month, day, Math.Max(hour, 0), Math.Max(minute, 0), Math.Max(second, 0));
            return true;
        }

        /// <summary>
        /// Parses the value as DateTimeOffset.TryParseExact with DateTimeStyles.None does: the offset is the one
        /// of the local time zone. Returns false when the value is not valid: then DateTimeOffset.TryParseExact must be used.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        public bool TryParse(ReadOnlySpan<char> value, out DateTimeOffset result)
        {
            // in the first and the last year the local offset may move the date out of range
            if (!TryParse(value, out DateTime date) || date.Year == 1 || date.Year == 9999)
            {
                result = default;
                return false;
            }
            // as the parser, the Unspecified date takes the offset of the local time zone
            result = new DateTimeOffset(date);
            return true;
        }

#if NET6_0_OR_GREATER
        /// <summary>
        /// Parses the value as DateOnly.TryParseExact does: a format with a time is left to DateOnly.TryParseExact.
        /// Returns false when the value is not valid: then DateOnly.TryParseExact must be used.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        public bool TryParse(ReadOnlySpan<char> value, out DateOnly result)
        {
            if (HasTime || !TryParse(value, out DateTime date))
            {
                result = default;
                return false;
            }
            result = DateOnly.FromDateTime(date);
            return true;
        }
#endif
    }
}
