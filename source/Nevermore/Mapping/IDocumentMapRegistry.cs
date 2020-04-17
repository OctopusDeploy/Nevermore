using System;
using System.Collections.Generic;

namespace Nevermore.Mapping
{
    public interface IDocumentMapRegistry
    {
        void Register(DocumentMap map);
        void Register(params DocumentMap[] mappingsToAdd);
        void Register(IEnumerable<DocumentMap> mappingsToAdd);

        bool ResolveOptional(Type type, out DocumentMap map);
        DocumentMap Resolve(Type type);
        DocumentMap Resolve<TDocument>();
        DocumentMap Resolve(object instance);

        string GetId(object instance);
    }
}