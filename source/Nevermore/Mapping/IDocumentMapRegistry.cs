using System;
using System.Collections.Generic;

namespace Nevermore.Mapping
{
    public interface IDocumentMapRegistry
    {
        void Register(DocumentMap map);
        void Register(IDocumentMap map);
        void Register(params IDocumentMap[] mappingsToAdd);
        void Register(IEnumerable<IDocumentMap> mappingsToAdd);

        bool ResolveOptional(Type type, out DocumentMap map);
        DocumentMap Resolve(Type type);
        DocumentMap Resolve<TDocument>();
        DocumentMap Resolve(object instance);

        object GetId(object instance);
    }
}