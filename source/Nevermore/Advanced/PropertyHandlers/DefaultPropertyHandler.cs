using System;
using System.Linq.Expressions;
using System.Reflection;

namespace Nevermore.Advanced.PropertyHandlers
{
    internal sealed class PropertyHandler : IPropertyHandler
    {
        readonly Action<object, object> setterCompiled;
        readonly Func<object, object> getterCompiled;

        public PropertyHandler(PropertyInfo propertyInfo)
        {
            var targetArgument = Expression.Parameter(typeof(object), "target");

            var setterMethod = propertyInfo.GetSetMethod(true);
            if (setterMethod != null)
            {            
                var valueArgument = Expression.Parameter(typeof(object), "value");
                var assign =
                    Expression.Assign(
                        Expression.Property(Expression.Convert(targetArgument, propertyInfo.DeclaringType), propertyInfo),
                        Expression.Convert(targetArgument, propertyInfo.PropertyType));
                
                var setter = Expression.Lambda<Action<object, object>>(assign, targetArgument, valueArgument);
                setterCompiled = setter.Compile();
            }

            var getterMethod = propertyInfo.GetGetMethod(true);
            if (getterMethod != null)
            {
                var getter = Expression.Lambda<Func<object, object>>(
                    Expression.Convert(
                        Expression.Property(
                            Expression.Convert(targetArgument, propertyInfo.DeclaringType), 
                            propertyInfo),
                        typeof(object)),
                    targetArgument);
                getterCompiled = getter.Compile();
            }
        }

        public bool CanRead => getterCompiled != null;

        public object Read(object target)
        {
            if (!CanRead)
                throw new InvalidOperationException("Cannot read a property value without a getter. If the property is meant to be write-only, mark it with LoadOnly() on the column definition in the DocumentMap.");

            return getterCompiled(target);
        }

        public bool CanWrite => setterCompiled != null;

        public void Write(object target, object value)
        {
            if (!CanWrite)
                throw new InvalidOperationException("Cannot write to a property without a setter. If the property is meant to be read-only, mark it with StoreOnly() on the column definition in the DocumentMap.");

            setterCompiled(target, value);
        }
    }
}