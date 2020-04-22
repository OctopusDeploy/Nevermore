using Nevermore.Diagnostics;
using NUnit.Framework;

namespace Nevermore.Tests.Diagnostics
{
    public class QueryPlanThrashingDetectorFixture
    {
        [Test]
        public void ShouldDetectDuplicateQueries()
        {
            QueryPlanThrashingDetector.Detect("select * from dbo.Customer where Id = @id");
            QueryPlanThrashingDetector.Detect("select * from dbo.Customer where Id = @id");
            QueryPlanThrashingDetector.Detect("select * from dbo.Customer where Id = @id");
            Assert.Throws<DuplicateQueryException>(() => QueryPlanThrashingDetector.Detect("select * from dbo.Customer where Id = @id_1"));
            QueryPlanThrashingDetector.Detect("select * from dbo.Customer where Id = @id");
        }
    }
}