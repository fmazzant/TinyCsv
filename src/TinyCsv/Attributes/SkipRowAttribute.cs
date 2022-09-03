
using System;
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

namespace TinyCsv.Attributes
{
    using TinyCsv.Exceptions;
    using System;
    using TinyCsv.Attributes.Interfaces;

    /// <summary>
    /// Allows skipt row by condition
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public sealed class SkipRowAttribute : Attribute
    {
        /// <summary>
        /// Allows skipt row by condition
        /// </summary>
        public Func<string, int, bool> SkipRow { get; private set; } = (row, index) => false;

        /// <summary>
        /// Skip row type
        /// </summary>
        public Type SkipRowType { get; private set; }

        /// <summary>
        /// Contructor
        /// </summary>
        /// <param name="hasHeaderRecord"></param>
        public SkipRowAttribute(Type skipRowType)
          : base()
        {
            SkipRowType = skipRowType;

            if (typeof(ISkipRow).IsAssignableFrom(skipRowType))
            {
                var skipRowModel = (ISkipRow)Activator.CreateInstance(skipRowType);
                SkipRow = skipRowModel.SkipRow;
            }
            else
            {
                ThrowsUnsupportedTypeException();
            }
        }

        /// <summary>
        /// Throws Unsupported Type Exception 
        /// </summary>
        private void ThrowsUnsupportedTypeException()
        {
            throw new NotImplementedException($"skipRowType not implement the SkipRow function from ISkipRow interface");
        }
    }
}

namespace TinyCsv.Attributes.Interfaces
{
    /// <summary>
    /// Skip row interface
    /// </summary>
    public interface ISkipRow
    {
        Func<string, int, bool> SkipRow { get; }
    }
}
