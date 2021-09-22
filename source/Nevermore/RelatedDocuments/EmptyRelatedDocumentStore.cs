using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Nevermore.RelatedDocuments
{
    public class EmptyRelatedDocumentStore : IRelatedDocumentStore
    {
        public void PopulateRelatedDocuments<TDocument>(IWriteTransaction transaction, TDocument instance) where TDocument : class
        {
        }

        public void PopulateManyRelatedDocuments<TDocument>(IWriteTransaction transaction, IEnumerable<TDocument> instance) where TDocument : class
        {
        }

        public Task PopulateRelatedDocumentsAsync<TDocument>(IWriteTransaction transaction, TDocument instance, CancellationToken cancellationToken = default) where TDocument : class
        {
            return Task.CompletedTask;
        }

        public Task PopulateManyRelatedDocumentsAsync<TDocument>(IWriteTransaction transaction, TDocument instance, CancellationToken cancellationToken = default) where TDocument : class
        {
            return Task.CompletedTask;
        }
    }
}