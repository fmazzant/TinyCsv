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

namespace Mafesoft.TinyCsv.Streams
{
    using System.IO;
    using System.Text;

    /// <summary>
    /// Text Memory Stream
    /// </summary>
    public class TextMemoryStream : MemoryStream
    {
        /// <summary>
        /// Create an instance of TextMemoryStream from UTF8 text
        /// </summary>
        /// <param name="text"></param>
        public TextMemoryStream(string text)
          : this(text, Encoding.UTF8)
        {
        }

        /// <summary>
        /// Create an instance of TextMemoryStream from text and encoding
        /// </summary>
        /// <param name="text"></param>
        /// <param name="encoding"></param>
        public TextMemoryStream(string text, Encoding encoding)
            : base(encoding.GetBytes(text))
        {
        }

        /// <summary>
        /// The reader of a text. With UTF-8 the text is read as it is, because encoding and decoding it
        /// returns the same text, apart from a leading byte order mark that is removed.
        /// </summary>
        /// <param name="text"></param>
        /// <param name="encoding"></param>
        /// <returns></returns>
        internal static TextReader CreateReader(string text, Encoding encoding)
        {
            if (encoding is UTF8Encoding && text != null)
            {
                return new StringReader(text.Length > 0 && text[0] == '﻿' ? text.Substring(1) : text);
            }
            return new StreamReader(new TextMemoryStream(text, encoding), encoding, true);
        }
    }
}