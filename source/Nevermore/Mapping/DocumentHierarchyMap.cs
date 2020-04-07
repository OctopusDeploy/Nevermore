using System;
using System.Collections.Generic;

namespace Nevermore.Mapping
{
    public abstract class DocumentHierarchyMap<TBaseDocument> : DocumentHierarchyMap<TBaseDocument, string>
    {
    }
    
    public abstract class DocumentHierarchyMap<TBaseDocument, TDiscriminator> : DocumentMap<TBaseDocument>, IDocumentHierarchyMap
    {
        protected DocumentHierarchyMap()
        {
            CustomTypeDefinition = new InheritedTypeDesignatingTypeDefinition(() => DerivedTypeMappings, () => TypeDesignatingPropertyName);
        }

        protected abstract IDictionary<TDiscriminator, Type> DerivedTypeMappings { get; }
        protected abstract string TypeDesignatingPropertyName { get; }
        
        public CustomTypeDefinitionBase CustomTypeDefinition { get; }
        
        class InheritedTypeDesignatingTypeDefinition : TypeDesignatingTypeDefinition<TBaseDocument, TDiscriminator>
        {
            readonly Func<IDictionary<TDiscriminator, Type>> derivedTypeMappings;
            readonly Func<string> typeDesignatingPropertyName;

            public InheritedTypeDesignatingTypeDefinition(Func<IDictionary<TDiscriminator, Type>> derivedTypeMappings, Func<string> typeDesignatingPropertyName)
            {
                this.derivedTypeMappings = derivedTypeMappings;
                this.typeDesignatingPropertyName = typeDesignatingPropertyName;
            }

            protected override IDictionary<TDiscriminator, Type> DerivedTypeMappings => derivedTypeMappings();
            protected override string TypeDesignatingPropertyName => typeDesignatingPropertyName();
        }
    }

    
    public interface IDocumentHierarchyMap
    {
        CustomTypeDefinitionBase CustomTypeDefinition { get; }
    }
}