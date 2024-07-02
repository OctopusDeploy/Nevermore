namespace Nevermore
{
    public enum ConcurrencyMode
    {
        /// <summary>
        /// When queries are executed in parallel, no locking will be performed.
        /// </summary>
        NoLock,
        
        /// <summary>
        /// When queries are executed in parallel, locking will be performed preventing queries from running in parallel against the same transaction.
        /// </summary>
        LockOnly,
        
        /// <summary>
        /// When queries are executed in parallel, a log message will be written.
        /// </summary>
        LogOnly,
        
        /// <summary>
        /// When queries are executed in parallel, locking will be performed and a log message will be written.
        /// </summary>
        LockWithLogging
    }
}