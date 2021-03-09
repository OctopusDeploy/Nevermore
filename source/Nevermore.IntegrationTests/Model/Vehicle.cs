namespace Nevermore.IntegrationTests.Model
{
    public abstract class Vehicle
    {
        public string Id { get; set; }
        public int Weight { get; set; }
        public int MaxSpeed { get; set; }
        public string Registration { get; set; }

        protected Vehicle(string registration)
        {
            Registration = registration;
        }
    }
}