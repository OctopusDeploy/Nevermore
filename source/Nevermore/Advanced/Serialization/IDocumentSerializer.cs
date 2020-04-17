using System;
using System.IO;
using System.Text;
using Nevermore.Mapping;

namespace Nevermore.Advanced.Serialization
{
    public interface IDocumentSerializer
    {
        public Encoding EncodingForCompressedText { get; set; }
        
        Stream SerializeCompressed(object instance, DocumentMap map);
        TextReader SerializeText(object instance, DocumentMap map);

        object DeserializeSmallText(string text, Type type);
        object DeserializeLargeText(TextReader reader, Type type);
        object DeserializeCompressed(Stream dataReaderStream, Type type);
    }
}