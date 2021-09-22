using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Nevermore.RelatedDocuments
{
    public interface IRelatedDocumentStore
    {
        void PopulateRelatedDocuments<TDocument>(IWriteTransaction transaction, TDocument instance) where TDocument : class;
        void PopulateManyRelatedDocuments<TDocument>(IWriteTransaction transaction, IEnumerable<TDocument> instance) where TDocument : class;
        Task PopulateRelatedDocumentsAsync<TDocument>(IWriteTransaction transaction, TDocument instance, CancellationToken cancellationToken = default) where TDocument : class;
        Task PopulateManyRelatedDocumentsAsync<TDocument>(IWriteTransaction transaction, IEnumerable<TDocument> instance, CancellationToken cancellationToken = default) where TDocument : class;
    }
}
