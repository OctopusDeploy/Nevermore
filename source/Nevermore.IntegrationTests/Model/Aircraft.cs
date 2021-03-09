namespace Nevermore.IntegrationTests.Model
{
    public enum AircraftType
    {
        FixedWing,
        RotaryWing
    }
    
    public class Aircraft : Vehicle
    {
        public AircraftType Type { get; set; }

        public Aircraft(AircraftType type, string registration) : base(registration)
        {
            Type = type;
        }
    }
}