namespace Nevermore.Transient.Throttling
{
    public enum ThrottledResourceType
    {
        Unknown = -1,
        PhysicalDatabaseSpace = 0,
        PhysicalLogSpace = 1,
        LogWriteIoDelay = 2,
        DataReadIoDelay = 3,
        Cpu = 4,
        DatabaseSize = 5,
        Internal = 6,
        WorkerThreads = 7,
    }
}