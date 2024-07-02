namespace Nevermore.Advanced.Concurrency
{
    public enum ConcurrencyMode
    {
        /// <summary>
        /// When a query is executed in parallel, no locking is performed.
        /// </summary>
        NoLocking,
        
        /// <summary>
        /// When a query is executed in parallel, locking is performed.
        /// </summary>
        LockOnly,
        
        /// <summary>
        /// When a query is executed in parallel, locking is performed and a warning is logged.
        /// </summary>
        LockAndWarn,
        
        /// <summary>
        /// When a query is executed in parallel, a warning is logged.
        /// </summary>
        WarnOnly
    }
}