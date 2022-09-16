using System;

namespace Nevermore.Advanced.InstanceTypeResolvers
{
    /// <summary>
    /// When reading documents from the database, if the result set contains a 'Type' column, Nevermore will look for
    /// an <see cref="IInstanceTypeResolver"/> that can resolve it. A use case for this is when you have an inheritance
    /// hierarchy of documents, and want to instantiate different concrete types when derserializing.
    /// </summary>
    public interface IInstanceTypeResolver
    {
        /// <summary>
        /// Gets the priority for this resolver. Instance type resolvers are asked in order based on priority, since
        /// more than one may know how to handle a given type. This allows you to register a fallback for a given type
        /// - such as for when a subclass of a type is not found.
        /// </summary>
        public int Order => 0;
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="baseType"></param>
        /// <param name="typeColumnValue"></param>
        /// <returns></returns>
        Type ResolveTypeFromValue(Type baseType, object typeColumnValue);

        object ResolveValueFromType(Type type);
    }
}