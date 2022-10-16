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
namespace TinyCsv.Factory
{
    using System;
    using System.Numerics;
    using TinyCsv.Conversions;

    public static class ValueConverterFactory
    {
        /// <summary>
        /// Returns the value converter by type
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static IValueConverter CreateValueConverterByType(Type type = null)
        {
            switch (type)
            {
                case Type _ when type == typeof(DateTime): return new DateTimeConverter();
                case Type _ when type == typeof(TimeSpan): return new TimeSpanConverter();
                case Type _ when type == typeof(ushort): return new UInt16Converter();
                case Type _ when type == typeof(uint): return new UInt32Converter();
                case Type _ when type == typeof(ulong): return new UInt64Converter();
                case Type _ when type == typeof(short): return new Int16Converter();
                case Type _ when type == typeof(int): return new Int32Converter();
                case Type _ when type == typeof(long): return new Int64Converter();
                case Type _ when type == typeof(BigInteger): return new BigIntegerConverter();
                case Type _ when type == typeof(decimal): return new DecimalConverter();
                case Type _ when type == typeof(float): return new FloatConverter();
                case Type _ when type == typeof(double): return new DoubleConverter();
                case Type _ when type == typeof(bool): return new BooleanConverter();
                case Type _ when type == typeof(Uri): return new UriConverter();
                case Type _ when type == typeof(Enum) || type.IsEnum: return new EnumConverter();
                case Type _ when type == typeof(byte[]): return new ByteArrayConverter();
                case Type _ when type == typeof(char): return new CharConverter();
                case Type _ when type == typeof(Guid): return new GuidConverter();

#if NET6_0_OR_GREATER
                case Type _ when type == typeof(DateOnly): return new DateOnlyConverter();
                case Type _ when type == typeof(TimeOnly): return new TimeOnlyConverter();
#endif

                default: return new DefaultValueConverter();
            }
        }
    }
}
