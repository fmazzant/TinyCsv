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

namespace TinyCsv.Extensions
{
    using System;
    using System.Linq.Expressions;

    /// <summary>
    /// Expression extensions
    /// </summary>
    public static class ExpressionExtensions
    {
        /// <summary>
        /// Returns property name
        /// </summary>
        /// <param name="expression"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public static string GetPropertyName(this Expression expression)
        {
            if (expression is LambdaExpression lambdaExpression)
            {
                if (lambdaExpression.Body.NodeType == ExpressionType.MemberAccess)
                {
                    return GetPropertyName(lambdaExpression.Body);
                }
            }

            if (expression is MemberExpression memberExpression)
            {
                if (memberExpression.Expression.NodeType == ExpressionType.MemberAccess)
                {
                    throw new ArgumentException(string.Format("Cannot interpret member from {0}", expression));
                }
                return memberExpression.Member.Name;
            }

            if (expression is UnaryExpression unaryExpression)
            {
                if (unaryExpression.NodeType != ExpressionType.Convert)
                {
                    throw new ArgumentException(string.Format("Cannot interpret member from {0}", expression));
                }
                return GetPropertyName(unaryExpression.Operand);
            }

            throw new ArgumentException(string.Format("Could not determine member from {0}", expression));
        }
    }
}