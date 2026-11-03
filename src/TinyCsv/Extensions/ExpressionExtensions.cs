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
    using System.Collections.Generic;
    using System.Linq.Expressions;
    using System.Reflection;

    /// <summary>
    /// Expression extensions
    /// </summary>
    public static class ExpressionExtensions
    {
        /// <summary>
        /// The separator of the names of a nested property in the column name: m => m.Address.City is Address_City
        /// </summary>
        internal const string NestedColumnNameSeparator = "_";

        /// <summary>
        /// The column name of a property: the property name, or the names of a nested property joined by an underscore
        /// </summary>
        internal static string GetColumnName(this Expression expression)
        {
            var path = expression.GetNestedPropertyPath();
            return path == null ? expression.GetPropertyName() : GetColumnName(path);
        }

        internal static string GetColumnName(IEnumerable<PropertyInfo> path)
        {
            return JoinNames(path, NestedColumnNameSeparator);
        }

        /// <summary>
        /// The internal name of a nested property: Address.City
        /// </summary>
        internal static string GetPropertyPathName(IEnumerable<PropertyInfo> path)
        {
            return JoinNames(path, ".");
        }

        private static string JoinNames(IEnumerable<PropertyInfo> path, string separator)
        {
            var names = new List<string>();
            foreach (var property in path)
            {
                names.Add(property.Name);
            }
            return string.Join(separator, names);
        }

        /// <summary>
        /// The properties of a nested property (m => m.Address.City), null when the property is not nested (m => m.Name).
        /// Each property except the last must be a class with a public parameterless constructor, created when it is null.
        /// </summary>
        /// <exception cref="ArgumentException"></exception>
        internal static PropertyInfo[] GetNestedPropertyPath(this Expression expression)
        {
            var body = expression is LambdaExpression lambdaExpression ? lambdaExpression.Body : expression;
            if (body is UnaryExpression unaryExpression && unaryExpression.NodeType == ExpressionType.Convert)
            {
                body = unaryExpression.Operand;
            }
            if (!(body is MemberExpression member) || !(member.Expression is MemberExpression))
            {
                return null;
            }

            var path = new List<PropertyInfo>();
            while (body is MemberExpression memberExpression)
            {
                if (!(memberExpression.Member is PropertyInfo property))
                {
                    throw new ArgumentException(string.Format("Cannot interpret member from {0}: {1} is not a property", expression, memberExpression.Member.Name));
                }
                path.Insert(0, property);
                body = memberExpression.Expression;
            }
            if (!(body is ParameterExpression))
            {
                throw new ArgumentException(string.Format("Could not determine member from {0}", expression));
            }
            for (int i = 0; i < path.Count - 1; i++)
            {
                if (!IsNestedType(path[i].PropertyType))
                {
                    throw new ArgumentException(string.Format("Cannot interpret member from {0}: {1} must be a class with a public parameterless constructor", expression, path[i].PropertyType.Name));
                }
            }
            return path.ToArray();
        }

        /// <summary>
        /// A type whose properties can be columns: a class, that is not a string or a collection, with a public parameterless constructor
        /// </summary>
        internal static bool IsNestedType(Type type)
        {
            return type.IsClass
                && !type.IsAbstract
                && type != typeof(string)
                && !typeof(System.Collections.IEnumerable).IsAssignableFrom(type)
                && type.GetConstructor(Type.EmptyTypes) != null;
        }
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
                // m => m.Name or m => (object)m.Id
                return GetPropertyName(lambdaExpression.Body);
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