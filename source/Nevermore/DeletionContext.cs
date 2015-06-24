namespace Nevermore
{
    public class DeletionContext
    {
        readonly IRelationalTransaction transaction;
        readonly string referencedDocumentId;
        readonly string referencingDocumentId;

        public DeletionContext(IRelationalTransaction transaction, string referencedDocumentId, string referencingDocumentId)
        {
            this.transaction = transaction;
            this.referencedDocumentId = referencedDocumentId;
            this.referencingDocumentId = referencingDocumentId;
        }

        public IRelationalTransaction Transaction
        {
            get { return transaction; }
        }

        public string ReferencedDocumentId
        {
            get { return referencedDocumentId; }
        }

        public string ReferencingDocumentId
        {
            get { return referencingDocumentId; }
        }
    }
}