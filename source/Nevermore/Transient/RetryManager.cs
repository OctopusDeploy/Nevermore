using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Nevermore.Util;

namespace Nevermore.Transient
{
    /// <summary>
    /// Provides the entry point to the retry functionality.
    /// </summary>
    public class RetryManager
    {
        static RetryManager defaultRetryManager;
        static object setDefaultRetryManagerMutex = new object();

        readonly IDictionary<string, RetryStrategy> retryStrategies;
        readonly IDictionary<string, RetryStrategy> defaultRetryStrategiesMap;
        string defaultRetryStrategyName;
        RetryStrategy defaultStrategy;

        /// <summary>
        /// Initializes a new instance of the <see cref="T:Octopus.Shared.TransientFaultHandling.RetryManager" /> class.
        /// </summary>
        /// <param name="retryStrategies">The complete set of retry strategies.</param>
        public RetryManager(IEnumerable<RetryStrategy> retryStrategies) : this(retryStrategies, null, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:Octopus.Shared.TransientFaultHandling.RetryManager" /> class with the specified retry strategies and default retry strategy name.
        /// </summary>
        /// <param name="retryStrategies">The complete set of retry strategies.</param>
        /// <param name="defaultRetryStrategyName">The default retry strategy.</param>
        public RetryManager(IEnumerable<RetryStrategy> retryStrategies, string defaultRetryStrategyName) : this(retryStrategies, defaultRetryStrategyName, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:Octopus.Shared.TransientFaultHandling.RetryManager" /> class with the specified retry strategies and defaults.
        /// </summary>
        /// <param name="retryStrategies">The complete set of retry strategies.</param>
        /// <param name="defaultRetryStrategyName">The default retry strategy.</param>
        /// <param name="defaultRetryStrategyNamesMap">The names of the default strategies for different technologies.</param>
        public RetryManager(IEnumerable<RetryStrategy> retryStrategies, string defaultRetryStrategyName, IDictionary<string, string> defaultRetryStrategyNamesMap)
        {
            this.retryStrategies = retryStrategies.ToDictionary((RetryStrategy p) => p.Name);
            var defaultRetryStrategyNamesMap1 = defaultRetryStrategyNamesMap;
            DefaultRetryStrategyName = defaultRetryStrategyName;
            defaultRetryStrategiesMap = new Dictionary<string, RetryStrategy>();
            if (defaultRetryStrategyNamesMap1 != null)
            {
                using (var enumerator = defaultRetryStrategyNamesMap1.Where((KeyValuePair<string, string> x) => !string.IsNullOrWhiteSpace(x.Value)).GetEnumerator())
                {
                    while (enumerator.MoveNext())
                    {
                        var current = enumerator.Current;
                        RetryStrategy retryStrategy;
                        if (!this.retryStrategies.TryGetValue(current.Value, out retryStrategy))
                        {
                            throw new ArgumentOutOfRangeException("defaultRetryStrategyNamesMap", string.Format(CultureInfo.CurrentCulture, "Default retry mapping strategy not found {0} {1}", current.Key, current.Value));
                        }
                        defaultRetryStrategiesMap.Add(current.Key, retryStrategy);
                    }
                }
            }
        }

        /// <summary>
        /// Gets the default <see cref="T:Octopus.Shared.TransientFaultHandling.RetryManager" /> for the application.
        /// </summary>
        /// <remarks>You can update the default retry manager by calling the <see cref="M:Octopus.Shared.TransientFaultHandling.RetryManager.SetDefault(Octopus.Shared.TransientFaultHandling.RetryManager,System.Boolean)" /> method.</remarks>
        public static RetryManager Instance
        {
            get
            {
                if (defaultRetryManager == null)
                    lock (setDefaultRetryManagerMutex)
                        if (defaultRetryManager == null)
                            TransientFaultHandling.InitializeRetryManager();

                return defaultRetryManager;
            }
        }

        /// <summary>
        /// Gets or sets the default retry strategy name.
        /// </summary>
        public string DefaultRetryStrategyName
        {
            get { return defaultRetryStrategyName; }
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    defaultRetryStrategyName = null;
                    return;
                }
                RetryStrategy defaultStrategy;
                if (retryStrategies.TryGetValue(value, out defaultStrategy))
                {
                    defaultRetryStrategyName = value;
                    this.defaultStrategy = defaultStrategy;
                    return;
                }
                throw new ArgumentOutOfRangeException("value", string.Format(CultureInfo.CurrentCulture, "Retry strategy not found: {0}", value));
            }
        }

        /// <summary>
        /// Sets the specified retry manager as the default retry manager.
        /// </summary>
        /// <param name="retryManager">The retry manager.</param>
        /// <param name="throwIfSet">true to throw an exception if the manager is already set; otherwise, false. Defaults to <see langword="true" />.</param>
        /// <exception cref="T:System.InvalidOperationException">The singleton is already set and <paramref name="throwIfSet" /> is true.</exception>
        public static void SetDefault(RetryManager retryManager, bool throwIfSet = true)
        {
            if (defaultRetryManager != null && throwIfSet && retryManager != defaultRetryManager)
            {
                throw new InvalidOperationException("Retry manager already set");
            }
            defaultRetryManager = retryManager;
        }

        /// <summary>
        /// Returns a retry policy with the specified error detection strategy and the default retry strategy defined in the configuration.
        /// </summary>
        /// <typeparam name="T">The type that implements the <see cref="T:Octopus.Shared.TransientFaultHandling.ITransientErrorDetectionStrategy" /> interface that is responsible for detecting transient conditions.</typeparam>
        /// <returns>A new retry policy with the specified error detection strategy and the default retry strategy defined in the configuration.</returns>
        public virtual RetryPolicy<T> GetRetryPolicy<T>() where T : ITransientErrorDetectionStrategy, new()
        {
            return new RetryPolicy<T>(GetRetryStrategy());
        }

        /// <summary>
        /// Returns a retry policy with the specified error detection strategy and retry strategy.
        /// </summary>
        /// <typeparam name="T">The type that implements the <see cref="T:Octopus.Shared.TransientFaultHandling.ITransientErrorDetectionStrategy" /> interface that is responsible for detecting transient conditions.</typeparam>
        /// <param name="retryStrategyName">The retry strategy name, as defined in the configuration.</param>
        /// <returns>A new retry policy with the specified error detection strategy and the default retry strategy defined in the configuration.</returns>
        public virtual RetryPolicy<T> GetRetryPolicy<T>(string retryStrategyName) where T : ITransientErrorDetectionStrategy, new()
        {
            return new RetryPolicy<T>(GetRetryStrategy(retryStrategyName));
        }

        /// <summary>
        /// Returns the default retry strategy defined in the configuration.
        /// </summary>
        /// <returns>The retry strategy that matches the default strategy.</returns>
        public virtual RetryStrategy GetRetryStrategy()
        {
            return defaultStrategy;
        }

        /// <summary>
        /// Returns the retry strategy that matches the specified name.
        /// </summary>
        /// <param name="retryStrategyName">The retry strategy name.</param>
        /// <returns>The retry strategy that matches the specified name.</returns>
        public virtual RetryStrategy GetRetryStrategy(string retryStrategyName)
        {
            Guard.ArgumentNotNullOrEmpty(retryStrategyName, "retryStrategyName");
            RetryStrategy result;
            if (!retryStrategies.TryGetValue(retryStrategyName, out result))
            {
                throw new ArgumentOutOfRangeException(string.Format(CultureInfo.CurrentCulture, "Retry strategy not found: {0}", retryStrategyName));
            }
            return result;
        }

        /// <summary>
        /// Returns the retry strategy for the specified technology.
        /// </summary>
        /// <param name="technology">The techonolgy to get the default retry strategy for.</param>
        /// <returns>The retry strategy for the specified technology.</returns>
        public virtual RetryStrategy GetDefaultRetryStrategy(string technology)
        {
            Guard.ArgumentNotNullOrEmpty(technology, "techonology");
            RetryStrategy defaultStrategy;
            if (!defaultRetryStrategiesMap.TryGetValue(technology, out defaultStrategy))
            {
                defaultStrategy = this.defaultStrategy;
            }
            if (defaultStrategy == null)
            {
                throw new ArgumentOutOfRangeException(string.Format(CultureInfo.CurrentCulture, "Retry strategy not found: {0}", technology));
            }
            return defaultStrategy;
        }
    }
}