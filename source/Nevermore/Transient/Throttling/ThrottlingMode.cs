namespace Nevermore.Transient.Throttling
{
    public enum ThrottlingMode
    {
        Unknown = -1,
        NoThrottling = 0,
        RejectUpdateInsert = 1,
        RejectAllWrites = 2,
        RejectAll = 3,
    }
}