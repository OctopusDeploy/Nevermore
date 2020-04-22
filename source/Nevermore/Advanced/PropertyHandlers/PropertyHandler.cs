using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace Nevermore.Advanced.PropertyHandlers
{
    internal sealed class PropertyHandler : IPropertyHandler
    {
        readonly PropertyInfo propertyInfo;
        readonly Action<object, object> setterCompiled;
        readonly Func<object, object> getterCompiled;

        public PropertyHandler(PropertyInfo propertyInfo)
        {
            this.propertyInfo = propertyInfo;

            var getterMethod = propertyInfo.GetGetMethod(true);
            if (getterMethod != null)
            {
                getterCompiled = CompileGetter(propertyInfo);
            }

            var setterMethod = propertyInfo.GetSetMethod(true);
            if (setterMethod != null)
            {
                setterCompiled = CompileSetter(propertyInfo);
            }

            if (setterMethod == null && getterMethod != null)
            {
                setterCompiled = TryCompileListSetter(propertyInfo);
            }
        }

        public bool CanRead => getterCompiled != null;

        public object Read(object target)
        {
            if (!CanRead)
                throw new InvalidOperationException($"Cannot read a property value without a getter ({propertyInfo.Name} on {propertyInfo.DeclaringType?.Name ?? "?"}). If the property is meant to be write-only, mark it with LoadOnly() on the column definition in the DocumentMap.");

            return getterCompiled(target);
        }

        public bool CanWrite => setterCompiled != null;

        public void Write(object target, object value)
        {
            if (!CanWrite)
                throw new InvalidOperationException($"Cannot write to a property without a setter ({propertyInfo.Name} on {propertyInfo.DeclaringType?.Name ?? "?"}). If the property is meant to be read-only, mark it with StoreOnly() on the column definition in the DocumentMap.");

            try
            {
                setterCompiled(target, value);
            }
            catch (InvalidCastException e)
            {
                throw new InvalidCastException($"Error assigning property {propertyInfo.DeclaringType.Name}.{propertyInfo.Name} the value '{value}' (type {value?.GetType()?.Name}). " + e.Message, e);
            }
            catch (NullReferenceException e)
            {
                throw new InvalidCastException($"Error assigning a value of 'null' to the property {propertyInfo.DeclaringType.Name}.{propertyInfo.Name}. " + e.Message, e);
            }
        }

        static Func<object, object> CompileGetter(PropertyInfo propertyInfo)
        {
            var targetArgument = Expression.Parameter(typeof(object), "target");
            var getter = Expression.Lambda<Func<object, object>>(
                Expression.Convert(
                    Expression.Property(
                        Expression.Convert(targetArgument, propertyInfo.DeclaringType),
                        propertyInfo),
                    typeof(object)),
                targetArgument);
            return getter.Compile();
        }

        static Action<object, object> CompileSetter(PropertyInfo propertyInfo)
        {
            var targetArgument = Expression.Parameter(typeof(object), "target");
            var valueArgument = Expression.Parameter(typeof(object), "value");
            var assign =
                Expression.Assign(
                    Expression.Property(Expression.Convert(targetArgument, propertyInfo.DeclaringType), propertyInfo),
                    Expression.Convert(valueArgument, propertyInfo.PropertyType));

            var setter = Expression.Lambda<Action<object, object>>(assign, targetArgument, valueArgument);
            return setter.Compile();
        }

        static Action<object, object> TryCompileListSetter(PropertyInfo propertyInfo)
        {
            var targetArgument = Expression.Parameter(typeof(object), "target");
            var valueArgument = Expression.Parameter(typeof(object), "value");

            var typeT = GetGenericListArgumentType(propertyInfo.PropertyType);
            if (typeT == null)
                return null;
            
            var collectionT = typeof(ICollection<>).MakeGenericType(typeT);
            var enumerableT = typeof(IEnumerable<>).MakeGenericType(typeT);

            var assignMethod = typeof(PropertyHandler).GetMethod(nameof(AssignCollectionValues), BindingFlags.NonPublic | BindingFlags.Static);
            if (assignMethod == null)
                return null;

            assignMethod = assignMethod.MakeGenericMethod(typeT);

            if (!collectionT.IsAssignableFrom(propertyInfo.PropertyType))
                return null;
            
            var setter = Expression.Lambda<Action<object, object>>(
                Expression.Call(null, assignMethod, 
                    Expression.Convert(
                        Expression.Property(
                            Expression.Convert(targetArgument, propertyInfo.DeclaringType),
                            propertyInfo),
                        collectionT),
                        
                    Expression.Convert(
                        valueArgument,
                        enumerableT
                    )
                ),
                targetArgument, valueArgument);
            return setter.Compile();
        }

        static Type GetGenericListArgumentType(Type propertyType)
        {
            var interfaces = propertyType.GetInterfaces();

            foreach (var implementedInterface in interfaces)
            {
                if (!implementedInterface.IsGenericType)
                    continue;

                var definition = implementedInterface.GetGenericTypeDefinition();
                if (definition == typeof(ICollection<>))
                {
                    return implementedInterface.GenericTypeArguments[0];
                }
            }
            
            return null;
        }

        static void AssignCollectionValues<T>(ICollection<T> collection, IEnumerable<T> values)
        {
            collection.Clear();
            foreach (var item in values)
            {
                collection.Add(item);
            }
        }
    }
}