namespace Nevermore.IntegrationTests.Model
{
    public class SpecialProductMap : ProductMap<SpecialProduct>
    {
        public SpecialProductMap()
        {
            TableName = typeof(Product).Name;
            Column(m => m.BonusMaterial).IsNullable = true;
        }
    }
}