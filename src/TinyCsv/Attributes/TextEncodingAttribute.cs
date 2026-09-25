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
    using System;
    using System.Text;

    /// <summary>
    /// Represents a character encoding
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public sealed class TextEncodingAttribute : Attribute
    {
        /// <summary>
        /// Represents a character encoding
        /// </summary>
        public Encoding TextEncoding { get; private set; }

        /// <summary>
        /// Contructor, UTF-8 encoding
        /// </summary>
        public TextEncodingAttribute()
            : base()
        {
            TextEncoding = Encoding.UTF8;
        }

        /// <summary>
        /// Contructor, usable as attribute: [TextEncoding("utf-16")]
        /// </summary>
        /// <param name="encodingName">the encoding name, i.e. "utf-8", "utf-16", "us-ascii", "iso-8859-1"</param>
        public TextEncodingAttribute(string encodingName)
            : base()
        {
            TextEncoding = string.IsNullOrEmpty(encodingName) ? Encoding.UTF8 : Encoding.GetEncoding(encodingName);
        }

        /// <summary>
        /// Contructor, usable as attribute: [TextEncoding(1200)]
        /// </summary>
        /// <param name="codePage">the encoding code page</param>
        public TextEncodingAttribute(int codePage)
            : base()
        {
            TextEncoding = Encoding.GetEncoding(codePage);
        }

        /// <summary>
        /// Contructor. Encoding is not a valid attribute parameter type: to use it as attribute use the name or the code page.
        /// </summary>
        /// <param name="encoding"></param>
        public TextEncodingAttribute(Encoding encoding)
            : base()
        {
            TextEncoding = encoding ?? Encoding.UTF8;
        }
    }

}
