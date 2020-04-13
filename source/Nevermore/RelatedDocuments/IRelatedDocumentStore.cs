using System.Collections.Generic;
using Nevermore.Contracts;

namespace Nevermore.RelatedDocuments
{
    public interface IRelatedDocumentStore
    {
        void PopulateRelatedDocuments<TDocument>(IWriteTransaction transaction, TDocument instance) where TDocument : class, IId;
        void PopulateRelatedDocuments<TDocument>(IWriteTransaction transaction, IEnumerable<TDocument> instance) where TDocument : class, IId;
    }
}
