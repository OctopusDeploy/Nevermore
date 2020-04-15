using System;
using System.Collections.Concurrent;
using System.Reflection;

namespace Nevermore.Mapping
{
    static class PropertyReaderFactory
    {
        static readonly ConcurrentDictionary<string, object> Readers = new ConcurrentDictionary<string, object>();

        public static IPropertyReaderWriter Create<TCast>(Type objectType, string propertyName)
        {
            var key = objectType.AssemblyQualifiedName + "-" + propertyName;
            IPropertyReaderWriter result = null;
            if (Readers.ContainsKey(key))
            {
                result = Readers[key] as IPropertyReaderWriter;
            }

            if (result != null)
                return result;

            var propertyInfo = objectType.GetTypeInfo().GetProperty(propertyName);
            if (propertyInfo != null)
            {
                if (!typeof (TCast).GetTypeInfo().IsAssignableFrom(propertyInfo.PropertyType))
                {
                    throw new InvalidOperationException(string.Format("Property type '{0}' for property '{1}.{2}' cannot be converted to type '{3}", propertyInfo.PropertyType, propertyInfo.DeclaringType == null ? "??" : propertyInfo.DeclaringType.Name, propertyInfo.Name, typeof (TCast).Name));
                }

                var delegateReaderType = typeof (Func<,>).MakeGenericType(propertyInfo.DeclaringType, propertyInfo.PropertyType);
                var delegateWriterType = typeof (Action<,>).MakeGenericType(propertyInfo.DeclaringType, propertyInfo.PropertyType);
                var readerType = typeof (DelegatePropertyReaderWriter<,,>).MakeGenericType(propertyInfo.DeclaringType, propertyInfo.PropertyType, typeof (TCast));
                var propertyGetterMethodInfo = propertyInfo.GetGetMethod();
                if (propertyGetterMethodInfo == null)
                {
                    throw new ArgumentException(string.Format("The property '{0}' on type '{1}' does not contain a getter which could be accessed by the OctoDB binding infrastructure.", propertyName, propertyInfo.DeclaringType));
                }

                var propertyGetterDelegate = propertyGetterMethodInfo.CreateDelegate(delegateReaderType);

                var propertySetterMethodInfo = propertyInfo.GetSetMethod(true);
                Delegate propertySetterDelegate = null;
                if (propertySetterMethodInfo != null)
                {
                    propertySetterDelegate = propertySetterMethodInfo.CreateDelegate(delegateWriterType);
                }

                result = (IPropertyReaderWriter)Activator.CreateInstance(readerType, propertyGetterDelegate, propertySetterDelegate);
                Readers[key] = result;
            }
            else
            {
                var fieldInfo = objectType.GetTypeInfo().GetField(propertyName, BindingFlags.Instance | BindingFlags.NonPublic);
                if (fieldInfo == null)
                    throw new InvalidOperationException(string.Format("The type '{0}' does not define a property or field named '{1}'", objectType.FullName, propertyName));

                if (!typeof (TCast).GetTypeInfo().IsAssignableFrom(fieldInfo.FieldType))
                {
                    throw new InvalidOperationException(string.Format("Field type '{0}' for field '{1}.{2}' cannot be converted to type '{3}", fieldInfo.FieldType, fieldInfo.DeclaringType == null ? "??" : fieldInfo.DeclaringType.Name, fieldInfo.Name, typeof (TCast).Name));
                }

                result = new FieldReaderWriter(fieldInfo);
                Readers[key] = result;
            }

            return result;
        }

        class DelegatePropertyReaderWriter<TInput, TReturn, TCast> : IPropertyReaderWriter
            where TReturn : TCast
        {
            readonly Func<TInput, TReturn> caller;
            readonly Action<TInput, TReturn> writer;

            public DelegatePropertyReaderWriter(Func<TInput, TReturn> caller, Action<TInput, TReturn> writer)
            {
                this.caller = caller;
                this.writer = writer;
            }

            public object Read(object target)
            {
                return caller((TInput)target);
            }

            public void Write(object target, object value)
            {
                if (writer == null)
                {
                    throw new InvalidOperationException("Cannot write to a property without a setter");
                }

                var returnable = (TReturn) value;
                writer((TInput)target, returnable);
            }
        }

        class FieldReaderWriter : IPropertyReaderWriter
        {
            readonly FieldInfo field;

            public FieldReaderWriter(FieldInfo field)
            {
                this.field = field;
            }

            public object Read(object target)
            {
                return field.GetValue(target);
            }

            public void Write(object target, object value)
            {
                field.SetValue(target, value);
            }
        }
    }
}