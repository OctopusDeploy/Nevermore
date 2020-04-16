using Nevermore.Mapping;

namespace Nevermore.Benchmarks.Model
{
    public class BigObjectMap : DocumentMap<BigObject>
    {
        public BigObjectMap(JsonStorageFormat format)
        {
            JsonStorageFormat = format;
            switch (format)
            {
                case JsonStorageFormat.CompressedOnly:
                    TableName += "Compressed";
                    break;
                case JsonStorageFormat.MixedPreferCompressed:
                case JsonStorageFormat.MixedPreferText:
                    TableName += "Mixed";
                    break;
            }
        }
    }
}