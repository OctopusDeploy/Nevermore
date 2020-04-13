using System.Collections.Generic;
using Nevermore.Contracts;

namespace Nevermore.RelatedDocuments
{
    public class EmptyRelatedDocumentStore : IRelatedDocumentStore
    {
        public void PopulateRelatedDocuments<TDocument>(IWriteTransaction transaction, TDocument instance) where TDocument : class, IId
        {
        }

        public void PopulateRelatedDocuments<TDocument>(IWriteTransaction transaction, IEnumerable<TDocument> instance) where TDocument : class, IId
        {
        }
    }
}