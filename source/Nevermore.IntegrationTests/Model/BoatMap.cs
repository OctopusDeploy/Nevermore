using Nevermore.Mapping;

namespace Nevermore.IntegrationTests.Model
{
    public class BoatMap : DocumentMap<Boat>
    {
        public BoatMap()
        {
            Column(m => m.Registration);
            Column(m => m.Weight);
            Column(m => m.MaxSpeed);
        }
    }
}