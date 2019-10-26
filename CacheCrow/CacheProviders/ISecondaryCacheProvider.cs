using CacheCrow.Model;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CacheCrow.CacheProviders
{
    /// <summary>
    /// 
    /// </summary>
    public interface ISecondaryCacheProvider<K, V> : IDisposable
    {
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
        void Init();

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

        /// <summary>
        /// 
        /// </summary>
        int Count { get; }
    }
}
