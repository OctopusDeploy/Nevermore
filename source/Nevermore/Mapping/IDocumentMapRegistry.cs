using System;
using System.Collections.Generic;
using Nevermore.Contracts;

namespace Nevermore.Mapping
{
    public interface IDocumentMapRegistry
    {
        void Register(DocumentMap map);
        void Register(params DocumentMap[] mappingsToAdd);
        void Register(IEnumerable<DocumentMap> mappingsToAdd);

        bool ResolveOptional(Type type, out DocumentMap map);
        DocumentMap Resolve(Type type);
        DocumentMap Resolve<TDocument>() where TDocument : IId;
        DocumentMap Resolve(object instance);
    }
}