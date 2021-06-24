using Nevermore.Mapping;

namespace Nevermore.IntegrationTests.Model
{
    public class AircraftMap : DocumentMap<Aircraft>
    {
        public AircraftMap()
        {
            Column(m => m.Registration);
            Column(m => m.Weight);
            Column(m => m.MaxSpeed);
        }
    }
}