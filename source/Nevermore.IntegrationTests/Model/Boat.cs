namespace Nevermore.IntegrationTests.Model
{
    public class Boat : Vehicle
    {
        public string Name { get; set; }
        public Boat(string name, string registration) : base(registration)
        {
        }
    }
}