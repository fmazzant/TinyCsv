using System;
using System.Linq.Expressions;
using System.Reflection;

namespace TinyCsv.Extentsions
{
    public static class ExpressionExtensions
    {
        public static string GetPropertyName(this Expression expression)
        {
            var memberExpression = expression.GetType().GetRuntimeProperty("Body").GetValue(expression) as MemberExpression;

            if (expression.NodeType == ExpressionType.Convert)
            {
                var body = (UnaryExpression)expression;
                memberExpression = body.Operand as MemberExpression;
            }
            else if (expression.NodeType == ExpressionType.MemberAccess)
            {
                memberExpression = expression as MemberExpression;
            }
            if (memberExpression == null)
            {
                throw new ArgumentException("Not a member access", nameof(expression));
            }
            return memberExpression.Member.Name;
        }
    }
}
