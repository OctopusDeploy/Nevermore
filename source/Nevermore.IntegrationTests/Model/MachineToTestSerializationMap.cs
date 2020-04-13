using Nevermore.Mapping;

namespace Nevermore.IntegrationTests.Model
{
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