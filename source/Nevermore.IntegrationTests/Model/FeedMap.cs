using System;
using System.Collections.Generic;
using Nevermore.Mapping;
using Nevermore.Serialization;

namespace Nevermore.IntegrationTests.Model
{
    public class FeedMap : DocumentMap<Feed>
    {
        public FeedMap()
        {
            IdColumn.MaxLength = 210;

            Column(m => m.Name);
            Column(m => m.FeedType);
        }
    }
    
    public class FeedConverter : InheritedClassByExtensibleEnumConverter<Feed, FeedType>
    {
        readonly Dictionary<string, Type> derivedTypeMappings = new Dictionary<string, Type>
        {
            {FeedType.BuiltIn.Name, typeof(BuiltInFeed)},
            {FeedType.NuGet.Name, typeof(NuGetFeed)}
        };

        protected override IDictionary<string, Type> DerivedTypeMappings => derivedTypeMappings;
        protected override string TypeDesignatingPropertyName => nameof(Feed.FeedType);
    }

}