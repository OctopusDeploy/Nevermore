namespace Nevermore
{
    /// <summary>
    /// Runs when the application starts up to prepare the document store for use. 
    /// </summary>
    public interface IStoreInitializer
    {
        void Initialize();
        void Stop();
    }
}