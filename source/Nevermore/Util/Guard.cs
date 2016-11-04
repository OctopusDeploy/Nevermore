using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace Nevermore.Util
{
    [DebuggerNonUserCode]
    internal static class Guard
    {

        public static void ArgumentNotNull(object argument, string parameterName)
        {
            if (argument == null)
                throw new ArgumentNullException(parameterName);
        }

        public static void ArgumentIsOfType(object argument, Type type, string parameterName)
        {
            if (argument == null || !type.GetTypeInfo().IsInstanceOfType(argument))
                throw new ArgumentException(parameterName);
        }

        public static void ArgumentNotNullOrEmpty(string argument, string parameterName)
        {
            ArgumentNotNull(argument, parameterName);
            if (argument.Trim().Length == 0)
            {
                throw new ArgumentException(string.Format("The parameter '{0}' cannot be empty.", parameterName), parameterName);
            }
        }


        public static void ArgumentNotNegativeValue(long argumentValue, string argumentName)
        {
            if (argumentValue < 0)
                throw new ArgumentOutOfRangeException(argumentName, $"Argument {argumentName} cannot be negative, but was: {argumentValue}");
        }

        public static void ArgumentNotGreaterThan(double argumentValue, double ceilingValue, string argumentName)
        {
            if (argumentValue > ceilingValue)
                throw new ArgumentOutOfRangeException(argumentName, $"Argument {argumentName} cannot be greater than {ceilingValue} but was {argumentValue}");
        }
    }
}