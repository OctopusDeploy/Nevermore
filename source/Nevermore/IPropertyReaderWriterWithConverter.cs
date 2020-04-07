using Nevermore.Mapping;

namespace Nevermore
{
    interface IPropertyReaderWriterWithConverter
    {
        void Initialize(IDatabaseValueConverter databaseValueConverter);
    }
}