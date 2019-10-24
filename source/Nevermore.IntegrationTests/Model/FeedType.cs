using System;
using System.Collections.Generic;
using Nevermore.Contracts;
using Nevermore.Serialization;

namespace Nevermore.IntegrationTests.Model
{
    public class FeedType : ExtensibleEnum
    {
        public static readonly FeedType BuiltIn = new FeedType("BuiltIn");
        public static readonly FeedType NuGet = new FeedType("NuGet");

        public FeedType(string name, string description = null) : base(name, description)
        {
        }
    }
    
    public class FeedTypeConverter : ExtensibleEnumConverter<FeedType>
    {
        protected override IDictionary<string, FeedType> Mappings { get; } = new Dictionary<string, FeedType>
        {
            {FeedType.BuiltIn.Name, FeedType.BuiltIn},
            {FeedType.NuGet.Name, FeedType.NuGet},
        };
    }
}