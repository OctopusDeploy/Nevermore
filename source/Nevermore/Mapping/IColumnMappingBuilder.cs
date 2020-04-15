namespace Nevermore.Mapping
{
    public interface IColumnMappingBuilder
    {
        IColumnMappingBuilder Nullable();
        IColumnMappingBuilder MaxLength(int max);
        IColumnMappingBuilder ReadOnly();
    }
}