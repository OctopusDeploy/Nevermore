using System;
using System.Collections.Generic;
using Nevermore.Mapping;
using Nevermore.Serialization;

namespace Nevermore.IntegrationTests.Model
{
    public class BrandConverter : InheritedClassConverter<Brand>
    {
        readonly Dictionary<string, Type> derivedTypeMappings = new Dictionary<string, Type>
        {
            {BrandA.BrandType, typeof(BrandA)},
            {BrandB.BrandType, typeof(BrandB)}
        };

        public BrandConverter(IDocumentMapRegistry documentMapRegistry) : base(documentMapRegistry)
        {
        }

        protected override IDictionary<string, Type> DerivedTypeMappings => derivedTypeMappings;
        protected override string TypeDesignatingPropertyName => "Type";
    }
}