using Nevermore.Mapping;

namespace Nevermore
{
    interface IPropertyReaderWriterWithConverter
    {
        void Initialize(IAmazingConverter amazingConverter);
    }
}