
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

namespace TinyCsv.Helpers
{
    using System.Collections.Concurrent;
    using System;
    using System.Linq;
    using LinqExpression = System.Linq.Expressions.Expression;

    /// <summary>
    /// Faster setter and getter on property
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class PropertyHelper<T>
    {
        public string Name { get; set; }
        public Func<T, object> Getter { get; set; }
        public Action<T, object> Setter { get; set; }

        private static readonly ConcurrentDictionary<Type, PropertyHelper<T>[]> CacheExpressionTree = new ConcurrentDictionary<Type, PropertyHelper<T>[]>();

        /// <summary>
        /// Create a ConcurrentDictionary with type as key and list of properties.
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        private static PropertyHelper<T>[] GetPropertiesExpressionTreeInternal(Type type)
        {
            return CacheExpressionTree.GetOrAdd(type, x => x.GetProperties().Select(property =>
            {
                Type eventLogCustomType = property.DeclaringType;
                Type propertyType = property.PropertyType;

                var instance = LinqExpression.Parameter(typeof(T));

                Func<T, object> getter = null;
                var getMethod = property.GetGetMethod();
                if (getMethod != null)
                {
                    getter =
                    LinqExpression.Lambda<Func<T, object>>(
                      LinqExpression.Convert(
                        LinqExpression.Call(
                          LinqExpression.Convert(instance, eventLogCustomType),
                          getMethod),
                        typeof(object)),
                      instance)
                    .Compile();
                }

                Action<T, object> setter = null;
                var setMethod = property.GetSetMethod();
                if (setMethod != null)
                {
                    var parameter = LinqExpression.Parameter(typeof(object));
                    setter =
                    LinqExpression.Lambda<Action<T, object>>(
                      LinqExpression.Call(
                        LinqExpression.Convert(instance, eventLogCustomType),
                        setMethod,
                        LinqExpression.Convert(parameter, propertyType)),
                      instance, parameter)
                    .Compile();
                }

                return new PropertyHelper<T>
                {
                    Name = property.Name,
                    Getter = getter,
                    Setter = setter,
                };
            }).ToArray());
        }

        /// <summary>
        /// Returns a ConcurrentDictionary with property name as key and property helper as value.
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static ConcurrentDictionary<string, PropertyHelper<T>> GetPropertiesExpressionTree(Type type)
        {
            var CacheExpressionTreeDict = new ConcurrentDictionary<string, PropertyHelper<T>>();
            var cache = GetPropertiesExpressionTreeInternal(type);
            foreach (var p in cache)
            {
                CacheExpressionTreeDict.GetOrAdd(p.Name, p);
            }
            return CacheExpressionTreeDict;
        }
    }
}