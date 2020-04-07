using System;

namespace Nevermore.Mapping
{
    interface IRelationalMappings
    {
        bool TryGet(Type type, out DocumentMap map);
        DocumentMap Get(object instance);
        DocumentMap Get(Type type);
    }
}