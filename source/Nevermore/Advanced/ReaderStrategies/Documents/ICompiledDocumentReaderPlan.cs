namespace Nevermore.Advanced.ReaderStrategies.Documents
{
    internal interface ICompiledDocumentReaderPlan
    {
        IDocumentReader CreateReader();
    }
}