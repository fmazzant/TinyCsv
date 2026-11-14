
namespace Mafesoft.TinyCsv.Data
{
    using System;
    using System.Linq.Expressions;
    using System.Reflection;

    /// <summary>
    /// The column of a nested property (m => m.Address.City): when reading, the null objects of the path are created;
    /// when writing, a null object of the path is an empty value.
    /// </summary>
    internal sealed class NestedColumnAccessor<T> : CsvColumnAccessor<T>
    {
        private readonly PropertyInfo[] path;
        private readonly Func<object, object?>[] getters;
        private readonly Action<object, object?>?[] setters;
        private readonly Func<object>[] constructors;

        public NestedColumnAccessor(PropertyInfo[] path)
        {
            this.path = path;
            getters = new Func<object, object?>[path.Length];
            setters = new Action<object, object?>?[path.Length];
            constructors = new Func<object>[path.Length];
            for (int i = 0; i < path.Length; i++)
            {
                var property = path[i];
                var target = Expression.Parameter(typeof(object), "target");
                var value = Expression.Parameter(typeof(object), "value");
                var declaringType = property.DeclaringType ?? throw new ArgumentException($"The property {property.Name} has no declaring type.");
                var member = Expression.Property(Expression.Convert(target, declaringType), property);
                getters[i] = Expression.Lambda<Func<object, object?>>(Expression.Convert(member, typeof(object)), target).Compile();
                if (property.GetSetMethod() != null)
                {
                    var assign = Expression.Assign(member, Expression.Convert(value, property.PropertyType));
                    setters[i] = Expression.Lambda<Action<object, object?>>(assign, target, value).Compile();
                }
                if (i < path.Length - 1)
                {
                    var create = Expression.Convert(Expression.New(property.PropertyType), typeof(object));
                    constructors[i] = Expression.Lambda<Func<object>>(create).Compile();
                }
            }
        }

        public override void SetValue(T model, CsvField value, CsvColumnBinding<T> binding, bool validateValues)
        {
            SetObject(model, binding.Column.ConvertBack(value.ToString(), binding.Provider, validateValues));
        }

        public override void SetObject(T model, object? value)
        {
            object target = model ?? throw new ArgumentNullException(nameof(model));
            var last = path.Length - 1;
            for (int i = 0; i < last; i++)
            {
                var next = getters[i](target);
                if (next == null)
                {
                    next = constructors[i]();
                    Setter(i)(target, next);
                }
                target = next;
            }
            Setter(last)(target, value);
        }

        private Action<object, object?> Setter(int index)
        {
            return setters[index] ?? throw new InvalidOperationException($"The property {path[index].Name} has no public setter.");
        }

        public override void AppendValue(T model, CsvLineBuilder<T> line, CsvColumnBinding<T> binding)
        {
            object? value = model;
            for (int i = 0; i < path.Length && value != null; i++)
            {
                value = getters[i](value);
            }
            var text = binding.Column.Converter.Convert(value, null, binding.Provider);
            if (value == null)
            {
                line.AppendNull(text, binding);
                return;
            }
            line.AppendValue(text, binding);
        }
    }
}
