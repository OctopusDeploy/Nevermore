//using System.Linq;
//using System.Text;

//namespace Nevermore
//{
//    public class ReferenceCollectionReaderWriter : PropertyReaderWriterDecorator
//    {
//        public ReferenceCollectionReaderWriter(IPropertyReaderWriter<object> original) : base(original)
//        {
//        }

//        public override object Read(object target)
//        {
//            var value = base.Read(target) as ReferenceCollection;
//            if (value == null || value.Count == 0)
//                return "";

//            var items = new StringBuilder();
//            items.Append("|");
//            foreach (var item in value)
//            {
//                items.Append(item);
//                items.Append("|");
//            }
//            return items.ToString();
//        }

//        public override void Write(object target, object value)
//        {
//            var valueAsString = (value ?? string.Empty).ToString().Split('|');

//            var collection = base.Read(target) as ReferenceCollection;
//            if (collection == null)
//            {
//                base.Write(target, collection = new ReferenceCollection());
//            }

//            collection.ReplaceAll(valueAsString.Where(v => !string.IsNullOrWhiteSpace(v)));
//        }
//    }
//}