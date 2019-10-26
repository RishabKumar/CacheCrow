using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.AccessControl;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using CacheCrow.Model;

namespace CacheCrow.CacheProviders
{
    public abstract class BaseCacheProvider<K, V> : ISecondaryCacheProvider<K, V>
    {
        public BaseCacheProvider()
        {
            Init();
        }

        public abstract int Count { get; }

        public abstract void Clear();

        public abstract void Dispose();

        public abstract void EnsureExists();

        public abstract bool Exists();

        public abstract void Init();

        public abstract bool IsAccessible();

        public abstract bool LookUp();

        public abstract ConcurrentDictionary<K, CacheData<V>> ReadCache();

        public abstract void WriteCache(ConcurrentDictionary<K, CacheData<V>> cache);
    }

    public class DefaultCacheProvider<K, V> : BaseCacheProvider<K, V>
    {
        private readonly string _cachePath;
        private readonly string _cacheDirectoryPath;
        private readonly double _cacheExpireInMilliseconds;
        private Mutex _mutex = new Mutex();
        private int _count;

        public override int Count => _count;

        public DefaultCacheProvider(double cacheExpireInMilliseconds)
        {
            _cacheExpireInMilliseconds = cacheExpireInMilliseconds;
            _count = -1;

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

        public override void Clear()
        {
            File.Delete(_cachePath);
        }

        public override void Dispose()
        {
            throw new NotImplementedException();
        }

        public override void Init()
        {
           
        }

        public override ConcurrentDictionary<K, CacheData<V>> ReadCache()
        {
            ConcurrentDictionary<K, CacheData<V>> dic = null;    
            _mutex.WaitOne(500);
            if (Exists())
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
            _count = dic.Count;
            _mutex.ReleaseMutex();
            return dic;
        }

        public override void WriteCache(ConcurrentDictionary<K, CacheData<V>> cache)
        {
            _mutex.WaitOne(500);
            if (!Exists())
            {
                CreateCacheDirectory();   
            }
            using (FileStream fs = new FileStream(_cachePath, FileMode.Create))
            {
                BinaryFormatter bf = new BinaryFormatter();
                bf.Serialize(fs, cache);
            }
            _mutex.ReleaseMutex();
        }

        private ConcurrentDictionary<K, CacheData<V>> GetValidCache(ConcurrentDictionary<K, CacheData<V>> cache)
        {
            return new ConcurrentDictionary<K, CacheData<V>>(cache.Where(x => DateTime.Now.Subtract(x.Value.CreationDate).TotalMilliseconds < _cacheExpireInMilliseconds));
        }

        private void CreateCacheDirectory()
        {
            Directory.CreateDirectory(_cacheDirectoryPath);
            var ds = new DirectorySecurity(_cacheDirectoryPath, AccessControlSections.Access);
            Directory.SetAccessControl(_cacheDirectoryPath, ds);
        }

        public override bool Exists()
        {
            if (File.Exists(_cachePath))
            {
                return true;
            }
            return false;
        }

        public override bool IsAccessible()
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

        public override void EnsureExists()
        {
            if (!Directory.Exists(_cacheDirectoryPath))
            {
                CreateCacheDirectory();
            }
        }

        public override bool LookUp()
        {
            throw new NotImplementedException();
        }
    }
}
