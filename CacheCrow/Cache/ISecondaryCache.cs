using CacheCrow.Model;
using System;
using System.Collections.Concurrent;

namespace CacheCrow.Cache
{
    public interface ISecondaryCache<K, V> : IDisposable
    {
        /// <summary>
        /// 
        /// </summary>
        int Count { get; }

        /// <summary>
        /// 
        /// </summary>
        double CacheExpireInMilliseconds { get; }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        ConcurrentDictionary<K, CacheData<V>> ReadCache();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="cache"></param>
        void WriteCache(ConcurrentDictionary<K, CacheData<V>> cache);

        /// <summary>
        /// 
        /// </summary>
        void Clear();

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        bool Exists();

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        bool IsAccessible();

        /// <summary>
        /// 
        /// </summary>
        void EnsureExists();

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        bool LookUp();
    }
}

