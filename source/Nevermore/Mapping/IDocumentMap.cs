namespace Nevermore.Mapping
{
    public interface IDocumentMap
    {
        DocumentMap Build(IPrimaryKeyHandlerRegistry primaryKeyHandlerRegistry);
    }
}