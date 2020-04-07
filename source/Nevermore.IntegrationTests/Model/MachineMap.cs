using System;
using System.Collections.Generic;
using Nevermore.Contracts;
using Nevermore.Mapping;

namespace Nevermore.IntegrationTests.Model
{
    public class MachineMap : DocumentMap<Machine>
    {
        public MachineMap()
        {
            Column(x => x.Name);
        }
    }

    public class EndpointTypeDefinition : TypeDesignatingTypeDefinition<Endpoint>
    {
        readonly Dictionary<string, Type> derivedTypeMappings = new Dictionary<string, Type>
        {
            {"PassiveTentacle", typeof(PassiveTentacleEndpoint)},
            {"ActiveTentacle", typeof(ActiveTentacleEndpoint)}
        };

        protected override IDictionary<string, Type> DerivedTypeMappings => derivedTypeMappings;
        protected override string TypeDesignatingPropertyName => "Type";
    }

    public class MachineToTestSerialization : IDocument
    {
        public string Id { get; protected set; }
        public string Name { get; set; }

        public string JSON { get; set; }
    }

    public class MachineToTestSerializationMap : DocumentMap<MachineToTestSerialization>
    {
        public MachineToTestSerializationMap()
        {
            TableName = "Machine";
            Column(x => x.Name);
            Column(x => x.JSON);
        }
    }
}