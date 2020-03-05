using System;
using System.Collections.Generic;
using Nevermore.Contracts;
using Nevermore.Mapping;
using Nevermore.Serialization;
using Octopus.TinyTypes;

namespace Nevermore.IntegrationTests.Model
{
    public class MachineMap : DocumentMap<Machine>
    {
        public MachineMap()
        {
            Column(x => x.Name);
        }
    }

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

    public class MachineToTestSerialization : IDocument<MachineToTestSerializationId>, IId
    {
        public MachineToTestSerializationId Id { get; protected set; }
        public string Name { get; set; }

        public string JSON { get; set; }
        string IId.Id => Id?.Value;
    }

    public class MachineToTestSerializationId : CaseSensitiveTypedString, IIdWrapper
    {
        public MachineToTestSerializationId(string value) : base(value)
        {
        }
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