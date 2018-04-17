using Nevermore.AST;

namespace Nevermore
{
    public class Parameter
    {
        readonly string parameterName;

        public Parameter(string parameterName, IDataType dataType)
        {
            this.parameterName = parameterName;
            DataType = dataType;
        }

        public Parameter(string parameterName)
        {
            this.parameterName = parameterName;
        }

        public string ParameterName => Normalise(parameterName);

        // Only certain characters are allowed in SQL parameter names: https://msdn.microsoft.com/en-us/library/ms175874.aspx?f=255&mspperror=-2147217396#Anchor_1
        // but for now we will keep it simple (e.g by not using a generic regex here) 
        // to make sure we don't put any unnecessary load on our Server that is already struggling in certain scenarios.  
        // https://blogs.msdn.microsoft.com/debuggingtoolbox/2008/04/02/comparing-regex-replace-string-replace-and-stringbuilder-replace-which-has-better-performance/
        static string Normalise(string value)
        {
            return value
                .Replace('-', '_')
                .Replace(' ', '_')
                .ToLower();
        }

        // Data type must be specified if you are creating a stored proc or function, otherwise it is not required
        public IDataType DataType { get; }
    }

    public class UniqueParameter : Parameter
    {
        protected UniqueParameter(string parameterName, IDataType dataType) : base(parameterName, dataType)
        {
        }
    }
}