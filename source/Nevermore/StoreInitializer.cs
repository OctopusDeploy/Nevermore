namespace Nevermore
{
    public class StoreInitializer : IStoreInitializer
    {
        readonly IRelationalStore store;
        readonly IInitializeRelationalStore[] initializers;

        public StoreInitializer(IRelationalStore store, IInitializeRelationalStore[] initializers)
        {
            this.store = store;
            this.initializers = initializers;
        }

        public void Initialize()
        {
            foreach (var initializer in initializers)
            {
                initializer.Initialize(store);
            }
        }

        public void Stop()
        {
        }
    }
}