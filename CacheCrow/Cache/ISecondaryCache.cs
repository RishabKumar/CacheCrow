using CacheCrow.Model;
using System;
using System.Collections.Concurrent;

namespace CacheCrow.Cache
{
    public interface ISecondaryCache<K, V>
    {
        /// <summary>
        /// Returns count of entries in secondary cache
        /// </summary>
        int Count { get; }

        /// <summary>
        /// Expiry time in milliseconds
        /// </summary>
        double CacheExpireInMilliseconds { get; }

        /// <summary>
        /// Fetches all entries as Dictionary from the secondary cache
        /// </summary>
        /// <returns>Returns Dictionary will all entries</returns>
        ConcurrentDictionary<K, CacheData<V>> ReadCache();

        /// <summary>
        /// Clears and writes Dictionary to secondary caches.
        /// </summary>
        /// <param name="cache"></param>
        void WriteCache(ConcurrentDictionary<K, CacheData<V>> cache);

        /// <summary>
        /// Clears all entries
        /// </summary>
        void Clear();

        /// <summary>
        /// Check if secondary cache exists.
        /// </summary>
        /// <returns>True is exists. False otherwise</returns>
        bool Exists();

        /// <summary>
        /// Check if secondary cache is accessible for read/write operations
        /// </summary>
        /// <returns>True if exists and accessible. False otherwise</returns>
        bool IsAccessible();

        /// <summary>
        /// Checks if exists otherwise creates/initializes it
        /// </summary>
        void EnsureExists();

        /// <summary>
        /// Searches the entries for the key
        /// </summary>
        /// <returns>Returns the found entry</returns>
        CacheData<V> LookUp(K key);

        /// <summary>
        /// Checks if secondary cache is empty
        /// </summary>
        /// <returns>True if not accessible or if empty. False if not empty.</returns>
        bool IsEmpty();
    }
}

