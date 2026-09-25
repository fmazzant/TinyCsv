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

#if NET452
namespace System
{
    /// <summary>
    /// Minimal ReadOnlySpan replacement for .NET Framework 4.5.2, where System.Memory is not available.
    /// It only exposes the members used by TinyCsv, with the same semantics.
    /// </summary>
    internal readonly struct ReadOnlySpan<T>
    {
        private readonly T[] array;
        private readonly int start;

        public ReadOnlySpan(T[] array)
            : this(array, 0, array?.Length ?? 0)
        {
        }

        public ReadOnlySpan(T[] array, int start, int length)
        {
            if (array == null ? start != 0 || length != 0 : (uint)start > (uint)array.Length || (uint)length > (uint)(array.Length - start))
            {
                throw new ArgumentOutOfRangeException(nameof(start));
            }
            this.array = array;
            this.start = start;
            this.Length = length;
        }

        public int Length { get; }

        public bool IsEmpty => Length == 0;

        public T this[int index]
        {
            get
            {
                if ((uint)index >= (uint)Length)
                {
                    throw new IndexOutOfRangeException();
                }
                return array[start + index];
            }
        }

        public ReadOnlySpan<T> Slice(int start)
        {
            return new ReadOnlySpan<T>(array, this.start + start, Length - start);
        }

        public ReadOnlySpan<T> Slice(int start, int length)
        {
            if ((uint)start > (uint)Length || (uint)length > (uint)(Length - start))
            {
                throw new ArgumentOutOfRangeException(nameof(start));
            }
            return new ReadOnlySpan<T>(array, this.start + start, length);
        }

        public override string ToString()
        {
            if (typeof(T) == typeof(char))
            {
                return Length == 0 ? string.Empty : new string((char[])(object)array, start, Length);
            }
            return $"System.ReadOnlySpan<{typeof(T).Name}>[{Length}]";
        }
    }

    /// <summary>
    /// Minimal MemoryExtensions replacement for .NET Framework 4.5.2.
    /// </summary>
    internal static class MemoryExtensions
    {
        public static ReadOnlySpan<char> AsSpan(this string text)
        {
            return text == null ? default : new ReadOnlySpan<char>(text.ToCharArray());
        }

        public static int IndexOf(this ReadOnlySpan<char> span, char value)
        {
            for (int i = 0; i < span.Length; i++)
            {
                if (span[i] == value)
                {
                    return i;
                }
            }
            return -1;
        }

        public static int IndexOf(this ReadOnlySpan<char> span, ReadOnlySpan<char> value)
        {
            if (value.Length == 0)
            {
                return 0;
            }
            for (int i = 0; i <= span.Length - value.Length; i++)
            {
                if (span.Slice(i).StartsWith(value))
                {
                    return i;
                }
            }
            return -1;
        }

        public static int IndexOfAny(this ReadOnlySpan<char> span, ReadOnlySpan<char> values)
        {
            for (int i = 0; i < span.Length; i++)
            {
                var c = span[i];
                for (int j = 0; j < values.Length; j++)
                {
                    if (c == values[j])
                    {
                        return i;
                    }
                }
            }
            return -1;
        }

        public static bool StartsWith(this ReadOnlySpan<char> span, ReadOnlySpan<char> value)
        {
            if (value.Length > span.Length)
            {
                return false;
            }
            for (int i = 0; i < value.Length; i++)
            {
                if (span[i] != value[i])
                {
                    return false;
                }
            }
            return true;
        }
    }
}
#endif
