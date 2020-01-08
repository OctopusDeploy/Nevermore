using System;
using System.IO;
using Assent;

namespace Nevermore.Tests.Util
{
    public class OtherFixtureNamer : INamer
    {
        readonly Type otherFixtureType;

        public OtherFixtureNamer(Type otherFixtureType)
        {
            this.otherFixtureType = otherFixtureType;
        }

        public virtual string GetName(TestMetadata metadata)
        {
            return Path.Combine(Path.GetDirectoryName(metadata.FilePath), otherFixtureType.Name + "." + metadata.TestName);
        }
    }
}