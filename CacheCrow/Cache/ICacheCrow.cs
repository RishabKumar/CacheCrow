using CacheCrow.Model;
using System;

namespace CacheCrow.Cache
{
    /// <summary>
    /// Interface for implementing LFU based cache
    /// </summary>
    /// <typeparam name="K"></typeparam>
    /// <typeparam name="V"></typeparam>
    public interface ICacheCrow<K, V> : IDisposable
    {
        /// <summary>
        /// Gets number of entries in Active CacheCrow
        /// </summary>
        int ActiveCount { get; }
        /// <summary>
        /// Gets total number of entries in CacheCrow
        /// </summary>
        int Count { get; }
        /// <summary>
        /// Gets a previously calculated total number of entries in CacheCrow. Note: Should be considered if realtime values are not required.
        /// </summary>
        int PreviousCount { get; }
        /// <summary>
        /// Removes all entries from CacheCrow, including entries in dormant cache and raises EmptyCacheEvent.
        /// </summary>
        void Clear();
        /// <summary>
        /// Inputs entry in Active CacheCrow if its size is not exceeded else adds the entry in Dormant CacheCrow.
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="data">Value</param>
        void Add(K key, V data);
        /// <summary>
        /// Inputs entry in Active CacheCrow with an option to update the cache value on expiration. The timer is reset after updation.
        /// </summary>
        /// <param name="Key"></param>
        /// <param name="data"></param>
        /// <param name="UpdateOnExpire"></param>
        void Add(K Key, V data, Func<V> UpdateOnExpire);
        /// <summary>
        /// Searches the key in both Active and Dormant CacheCrow and if found then updates the value.
        /// </summary>
        /// <param name="key">Existing key</param>
        /// <param name="data">New value</param>
        /// <returns>True if value was updated else false</returns>
        bool Update(K key, V data);
        /// <summary>
        /// Checks if key is present in the Active CacheCrow.
        /// </summary>
        /// <param name="key">The key to find</param>
        /// <returns>True if key is found in Active CacheCrow, else false</returns>
        bool ActiveLookUp(K key);
        /// <summary>
        /// Checks if key is present in CacheCrow(Active+Dormant), Note: LFU maybe performed and entries maybe swapped between Active and Dormant CacheCrow.
        /// </summary>
        /// <param name="key">The key to find</param>
        /// <returns>True if key is found, else false</returns>
        bool LookUp(K key);
        /// <summary>
        /// Removes the entry from Active CacheCrow corresponding to the key.
        /// </summary>
        /// <param name="key">The key to corresponding value to remove</param>
        /// <returns>If removed then returns removed value as CacheData, else returns empty CacheData</returns>
        CacheData<V> ActiveRemove(K key);
        /// <summary>
        /// Removes the entry from CacheCrow(Active+Dormant) corresponding to the key.
        /// </summary>
        /// <param name="key">The key to corresponding value to remove</param>
        /// <returns>If removed then returns removed value as CacheData, else returns empty CacheData</returns>
        CacheData<V> Remove(K key);
        /// <summary>
        /// Lookups the key in Active+Dormant CacheCrow, if found then increments the frequency.
        /// </summary>
        /// <param name="key">Key to corresponding value</param>
        /// <returns>If V is reference type and it is present then Object V else if V is value-type and it is not present then default value of V</returns>
        V GetValue(K key);
        /// <summary>
        /// Lookups the key in Active CacheCrow, if found then increments the frequency. Note: LFU maybe performed and entries maybe swapped between Active and Dormant CacheCrow.
        /// </summary>
        /// <param name="key">Key to corresponding value</param>
        /// <returns>If V is reference type and it is present then Object V else if V is value-type and it is not present then default value of V</returns>
        V GetActiveValue(K key);
        /// <summary>
        /// Raised when CacheCrow is empty.Note: It is also periodically raised by Cleaner when CacheCrow is empty.
        /// </summary>
        event EmptyCacheHandler EmptyCacheEvent;
    }
    /// <summary>
    /// Handler to handle EmptyCacheEvent.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="args"></param>
    public delegate void EmptyCacheHandler(object sender, EventArgs args);
}
