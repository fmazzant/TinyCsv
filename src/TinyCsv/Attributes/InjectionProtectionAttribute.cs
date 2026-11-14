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

namespace Mafesoft.TinyCsv.Attributes
{
    using System;

    /// <summary>
    /// Protects the text values from CSV injection when writing (see CsvOptions.InjectionProtection)
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public sealed class InjectionProtectionAttribute : Attribute
    {
        /// <summary>
        /// How the text values are protected
        /// </summary>
        public CsvInjectionProtection InjectionProtection { get; private set; }

        /// <summary>
        /// The characters that start a formula. Null is the default list (see CsvOptions.DefaultInjectionCharacters).
        /// </summary>
        public char[]? InjectionCharacters { get; private set; }

        /// <summary>
        /// Contructor
        /// </summary>
        /// <param name="injectionProtection"></param>
        /// <param name="injectionCharacters">the characters that start a formula, null for the default list</param>
        public InjectionProtectionAttribute(CsvInjectionProtection injectionProtection = CsvInjectionProtection.Escape, char[]? injectionCharacters = null)
            : base()
        {
            InjectionProtection = injectionProtection;
            InjectionCharacters = injectionCharacters;
        }
    }
}
