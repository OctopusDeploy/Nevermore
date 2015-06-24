using System.Collections.Generic;

namespace Nevermore
{
    public interface IDocumentUsageFinder
    {
        List<ReferencingDocument> FindReferences<TDocument>(IRelationalTransaction transaction, TDocument document);
    }
}