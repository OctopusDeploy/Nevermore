using System;
using System.Collections.Generic;
using Nevermore.Serialization;

namespace Nevermore.IntegrationTests.Model
{
    public class EndpointConverter : InheritedClassConverter<Endpoint>
    {
        readonly Dictionary<string, Type> derivedTypeMappings = new Dictionary<string, Type>
        {
            {"PassiveTentacle", typeof(PassiveTentacleEndpoint)},
            {"ActiveTentacle", typeof(ActiveTentacleEndpoint)}
        };

        protected override IDictionary<string, Type> DerivedTypeMappings => derivedTypeMappings;
        protected override string TypeDesignatingPropertyName => "Type";
    }
}