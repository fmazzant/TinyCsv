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

#if NET8_0_OR_GREATER
namespace Mafesoft.TinyCsv.AspNetCore.Extensions
{
    using System;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Net.Http.Headers;

    /// <summary>
    /// TinyCsv in MVC: the <c>text/csv</c> input and output formatters
    /// </summary>
    public static class TinyCsvMvcExtensions
    {
        /// <summary>
        /// Adds <see cref="TinyCsvInputFormatter"/> and <see cref="TinyCsvOutputFormatter"/> for <c>text/csv</c>,
        /// and the <c>csv</c> format for <c>[FormatFilter]</c> (i.e. <c>/orders.csv</c> or <c>?format=csv</c>).
        /// The models use the ITinyCsv&lt;T&gt; registered with AddTinyCsv, otherwise the attributes or the public properties of the model.
        /// </summary>
        public static IMvcBuilder AddTinyCsvFormatters(this IMvcBuilder builder)
        {
            ArgumentNullException.ThrowIfNull(builder);
            return builder.AddMvcOptions(options =>
            {
                options.InputFormatters.Add(new TinyCsvInputFormatter());
                options.OutputFormatters.Add(new TinyCsvOutputFormatter());
                options.FormatterMappings.SetMediaTypeMappingForFormat("csv", MediaTypeHeaderValue.Parse("text/csv"));
            });
        }
    }
}
#endif
