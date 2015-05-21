//namespace Nevermore
//{
//    public class SemanticVersionReaderWriter : PropertyReaderWriterDecorator
//    {
//        public SemanticVersionReaderWriter(IPropertyReaderWriter<object> original) : base(original)
//        {
//        }

//        public override object Read(object target)
//        {
//            var value = base.Read(target) as SemanticVersion;
//            if (value == null)
//                return "";

//            return value.ToString();
//        }

//        public override void Write(object target, object value)
//        {
//            var valueAsString = (value ?? string.Empty).ToString();

//            base.Write(target, SemanticVersion.Parse(valueAsString, true));
//        }
//    }
//}