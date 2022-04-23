﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Nevermore.Util
{
    public static class TypeExtensions
    {
        public static Type GetSequenceType(this Type type)
        {
            var typeInfo = type.GetTypeInfo();
            if (typeInfo.IsGenericTypeDefinition)
                throw new ArgumentException();

            var enumerableType = typeInfo
                .ImplementedInterfaces
                .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>));

            if (enumerableType == null)
                throw new ArgumentException("Provided type is not an IEnumerable.");

            return enumerableType.GetGenericArguments().Single();
        }

        public static object GetDefaultValue(this Type type)
        {
            return type.IsValueType
                ? Activator.CreateInstance(type)
                : null;
        }

        public static bool IsScalar(this Type type)
        {
            return Type.GetTypeCode(type) switch
            {
                TypeCode.Object => false,
                _ => true
            };
        }
    }
}