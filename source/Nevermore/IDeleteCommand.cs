namespace Nevermore
{
    public interface IDeleteCommand
    {
        void Delete(IRelationalTransaction transaction, IDocument document);
    }
}