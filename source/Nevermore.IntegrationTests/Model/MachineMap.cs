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
}