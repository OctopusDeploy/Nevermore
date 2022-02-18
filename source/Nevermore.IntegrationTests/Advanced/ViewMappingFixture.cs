using FluentAssertions;
using Nevermore.IntegrationTests.SetUp;
using Nevermore.Mapping;
using NUnit.Framework;

namespace Nevermore.IntegrationTests.Advanced
{
    public class ViewMappingFixture : FixtureWithRelationalStore
    {
        class ExtinctAnimal
        {
            public string Name { get; set; }
            public int LivedInMillionsAgo { get; set; }
            public string Class { get; set; }
        }

        class ExtinctAnimalMap : DocumentMap<ExtinctAnimal>
        {
            public ExtinctAnimalMap()
            {
                Id(u => u.Name);
                Column(u => u.LivedInMillionsAgo);
                Column(u => u.Class);
                JsonStorageFormat = JsonStorageFormat.NoJson;
            }
        }

        public override void OneTimeSetUp()
        {
            base.OneTimeSetUp();

            NoMonkeyBusiness();

            ExecuteSql(@"create view TestSchema.ExtinctAnimal as 
                            select 
	                            'Longisquama' as 'Name',
	                            242 as 'LivedInMillionsAgo',
	                            'Reptilia' as 'Class'
                            union
                            select 
	                            'Helicoprion' as 'Name',
	                            290 as 'LivedInMillionsAgo',
	                            'Chondrichthyes' as 'Class' 
                            ");
            Configuration.DocumentMaps.Register(new ExtinctAnimalMap());
        }

        [Test]
        public void ShouldLoad()
        {
            using var transaction = Store.BeginTransaction();

            var animals = transaction.Query<ExtinctAnimal>().ToArray();
            animals.Should().BeEquivalentTo(
                new[]
                {
                    new ExtinctAnimal()
                    {
                        Name = "Longisquama",
                        LivedInMillionsAgo = 242,
                        Class = "Reptilia"
                    },
                    new ExtinctAnimal()
                    {
                        Name = "Helicoprion",
                        LivedInMillionsAgo = 290,
                        Class = "Chondrichthyes"
                    }
                }
            );
        }
    }
}