using System.Diagnostics;

namespace Nevermore.Contracts
{
    [DebuggerDisplay("{Name}")]
    public abstract class ExtensibleEnum
    {
        protected ExtensibleEnum(string name, string description = null)
        {
            Name = name;
            Description = description;
        }

        public string Name { get; }
        public string Description { get; }

        public override string ToString()
        {
            return Name;
        }
    }
}