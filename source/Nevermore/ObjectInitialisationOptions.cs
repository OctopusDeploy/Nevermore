using System;

namespace Nevermore
{
    [Flags]
    public enum ObjectInitialisationOptions
    {
        None = 0,
        UseNonPublicConstructors = 1
    }
}