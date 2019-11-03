using CacheCrow.Model;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.AccessControl;
using System.Web;

namespace CacheCrow.Cache
{
    internal class DefaultSecondaryCache<K, V> : ISecondaryCache<K, V>
    {
        private readonly string _cachePath;
        private readonly string _cacheDirectoryPath;

        public int Count { get; private set; }
        public double CacheExpireInMilliseconds { get; private set; }

        public DefaultSecondaryCache(double cacheExpireInMilliseconds)
        {
            CacheExpireInMilliseconds = cacheExpireInMilliseconds;
            Count = -1;

            string appDirectory;
            if (!string.IsNullOrEmpty(HttpRuntime.AppDomainAppId))
            {
                appDirectory = HttpRuntime.AppDomainAppPath;
            }
            else
            {
                appDirectory = Path.GetFullPath(@"..\..\_crow");
            }
            _cacheDirectoryPath = appDirectory + "_crow";
            _cachePath = _cacheDirectoryPath + @"\CacheCrow";
            if (!Directory.Exists(_cacheDirectoryPath))
            {
                CreateCacheDirectory();
            }
        }

        public void Clear()
        {
            if (IsAccessible())
            {
                File.Delete(_cachePath);
            }
        }

        public ConcurrentDictionary<K, CacheData<V>> ReadCache()
        {
            ConcurrentDictionary<K, CacheData<V>> dic = null;
            lock (this)
            {
                if (IsEmpty())
                {
                    using (FileStream fs = new FileStream(_cachePath, FileMode.Open))
                    {
                        BinaryFormatter bf = new BinaryFormatter();
                        dic = (ConcurrentDictionary<K, CacheData<V>>)bf.Deserialize(fs);
                        dic = GetValidCache(dic);
                    }
                }
                else
                {
                    dic = new ConcurrentDictionary<K, CacheData<V>>();
                }
                Count = dic.Count;
            }
            return dic;
        }

        public void WriteCache(ConcurrentDictionary<K, CacheData<V>> cache)
        {
            lock (this)
            {
                if (!Exists())
                {
                    CreateCacheDirectory();
                }
                using (FileStream fs = new FileStream(_cachePath, FileMode.Create))
                {
                    BinaryFormatter bf = new BinaryFormatter();
                    bf.Serialize(fs, cache);
                }
            }
        }

        private ConcurrentDictionary<K, CacheData<V>> GetValidCache(ConcurrentDictionary<K, CacheData<V>> cache)
        {
            return new ConcurrentDictionary<K, CacheData<V>>(cache.Where(x => DateTime.Now.Subtract(x.Value.CreationDate).TotalMilliseconds < CacheExpireInMilliseconds));
        }

        private void CreateCacheDirectory()
        {
            Directory.CreateDirectory(_cacheDirectoryPath);
            var ds = new DirectorySecurity(_cacheDirectoryPath, AccessControlSections.Access);
            Directory.SetAccessControl(_cacheDirectoryPath, ds);
        }

        public bool Exists()
        {
            if (File.Exists(_cachePath))
            {
                return true;
            }
            return false;
        }

        public bool IsEmpty()
        {
            if (IsAccessible())
            {
                using (var fs = new FileStream(_cachePath, FileMode.Open))
                {
                    return fs.Length > 0;
                }
            }
            return true;
        }

        public bool IsAccessible()
        {
            if (!Exists())
            {
                return false;
            }

            var fileInfo = new FileInfo(_cachePath);
            try
            {
                using (var fileStream = fileInfo.Open(FileMode.Open, FileAccess.ReadWrite))
                {
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }

        public void EnsureExists()
        {
            if (!Directory.Exists(_cacheDirectoryPath))
            {
                CreateCacheDirectory();
            }
        }

        public CacheData<V> LookUp(K key)
        {
            throw new NotImplementedException();
        }
    }
}
