using CacheCrow.Model;
using CacheCrow.Timers;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.AccessControl;
using System.Timers;

namespace CacheCrow.Cache
{
    /// <summary>
    /// Represents A simple LFU based Cache that supports data expiry.
    /// </summary>
    /// <typeparam name="K">Key</typeparam>
    /// <typeparam name="V">Value</typeparam>
    public class CacheCrow<K, V> : ICacheCrow<K, V>
    {
        /// <summary>
        /// Raised when CacheCrow is empty.Note: It is also periodically raised by Cleaner when CacheCrow is empty.
        /// </summary>
        public event EmptyCacheHandler EmptyCacheEvent;
        private readonly int _cacheSize;
        private readonly int _activeCacheExpire;
        private readonly int _dormantCacheExpire;
        private readonly string _cachePath = Path.GetFullPath(@"..\..\_crow\CacheCrow");
        private readonly string _cacheDirectoryPath = Path.GetFullPath(@"..\..\_crow");
        private static ConcurrentDictionary<K, CacheData<V>> _cacheDic;
        private static ConcurrentDictionary<K, Timer> _timerDic;
        private static CacheCrow<K, V> _cache;
        private static Timer _cleaner;
        private int _dormantCacheCount;
        private static System.Threading.Mutex mutex = new System.Threading.Mutex();
        /// <summary>
        /// Initializes CacheCrow, creates a directory '_crow' in root if not present.
        /// </summary>
        /// <param name="size">Count of total entries in Active(in-memory) CacheCrow</param>
        /// <param name="activeCacheExpire">Milli-seconds before each entry in Active CacheCrow is expired</param>
        /// <param name="dormantCacheExpire">Milli-seconds before each entry in Dormant(disk) CacheCrow is expired</param>
        /// <param name="cleanerSnoozeTime">Milli-seconds before Cleaner cleans Dormant CacheCrow. Note: Cleaner is called after every cleanersnoozetime milli-seconds</param>
        /// <returns>Returns instance of ICacheCrow</returns>
        public static ICacheCrow<K, V> Initialize(int size = 1000, int activeCacheExpire = 300000, int dormantCacheExpire = 500000, int cleanerSnoozeTime = 400000)
        {
            if (_cache == null)
            {
                _cache = new CacheCrow<K, V>(size, activeCacheExpire, dormantCacheExpire, cleanerSnoozeTime);
            }
            _cache.LoadCache();
            return _cache;
        }
        /// <summary>
        /// Returns instance of ICacheCrow if it has been initialized
        /// </summary>
        public static ICacheCrow<K, V> GetCacheCrow { get { return _cache; } }
        /// <summary>
        /// Gets number of entries in Active CacheCrow
        /// </summary>
        public int ActiveCount
        {
            get { return _cacheDic.Count; }
        }
        /// <summary>
        /// Gets total number of entries in CacheCrow
        /// </summary>
        public int Count
        {
            get { return ActiveCount + ReadBinary().Count; }
        }
        /// <summary>
        /// Gets a previously calculated total number of entries in CacheCrow. Note: Should be considered if realtime values are not required.
        /// </summary>
        public int PreviousCount
        {
            get { return ActiveCount + _dormantCacheCount; }
        }
        /// <summary>
        /// Removes all entries from CacheCrow, including entries in dormant cache and raises EmptyCacheEvent.
        /// </summary>
        public void Clear()
        {
            _cacheDic.Clear();
            _timerDic.Clear();
            _cleaner.Stop();
            _cleaner.Start();
            WriteCache();
            EmptyCacheEvent?.Invoke(this, new EventArgs());
        }
        /// <summary>
        /// Inputs entry in Active CacheCrow if its size is not exceeded else adds the entry in Dormant CacheCrow.
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="data">Value</param>
        public void Add(K key, V data)
        {
            if (data != null)
            {
                Add(key, data, 1);
            }
        }
        /// <summary>
        /// Searches the key in both Active and Dormant CacheCrow and if found then updates the value.
        /// </summary>
        /// <param name="key">Existing key</param>
        /// <param name="data">New value</param>
        /// <returns>True if value was updated else false</returns>
        public bool Update(K key, V data)
        {
            if (data != null)
            {
                if (_cacheDic.ContainsKey(key))
                {
                    var cacheData = _cacheDic[key];
                    cacheData.CreationDate = DateTime.Now;
                    cacheData.Data = data;
                    _cacheDic[key] = cacheData;
                    _timerDic[key].Stop();
                    _timerDic[key].Start();
                    return true;
                }
                else
                {
                    var dic = ReadBinary();
                    if (dic.ContainsKey(key))
                    {
                        var cacheData = _cacheDic[key];
                        cacheData.CreationDate = DateTime.Now;
                        cacheData.Data = data;
                        _cacheDic[key] = cacheData;
                        _timerDic[key].Stop();
                        _timerDic[key].Start();
                        return true;
                    }
                }
            }
            return false;
        }
        /// <summary>
        /// Checks if key is present in the Active CacheCrow.
        /// </summary>
        /// <param name="key">The key to find</param>
        /// <returns>True if key is found in Active CacheCrow, else false</returns>
        public bool ActiveLookUp(K key)
        {
            if (_cacheDic.ContainsKey(key))
            {
                var cacheData = _cacheDic[key];
                cacheData.Frequency += 1;
                cacheData.CreationDate = DateTime.Now;
                _cacheDic[key] = cacheData;
                _timerDic[key].Stop();
                _timerDic[key].Start();
                return true;
            }
            return false;
        }
        /// <summary>
        /// Checks if key is present in CacheCrow(Active+Dormant)
        /// </summary>
        /// <param name="key">The key to find</param>
        /// <returns>True if key is found, else false</returns>
        public bool LookUp(K key)
        {
            if (_cacheDic.ContainsKey(key))
            {
                var cacheData = _cacheDic[key];
                cacheData.Frequency += 1;
                cacheData.CreationDate = DateTime.Now;
                _cacheDic[key] = cacheData;
                _timerDic[key].Stop();
                _timerDic[key].Start();
                return true;
            }
            return DeepLookUp(key);
        }
        /// <summary>
        /// Removes the entry from Active CacheCrow corresponsing to the key.
        /// </summary>
        /// <param name="key">The key to corresponding value to remove</param>
        /// <returns>If removed then returns removed value as CacheData, else returns empty CacheData</returns>
        public CacheData<V> ActiveRemove(K key)
        {
            CacheData<V> i = new CacheData<V>();
            if (_cacheDic.ContainsKey(key) && (_timerDic.ContainsKey(key)))
            {
                var cacheTimer = _timerDic[key];
                _cacheDic.TryRemove(key, out i);
                cacheTimer.Elapsed -= new ElapsedEventHandler(Elapsed_Event);
                cacheTimer.Enabled = false;
                cacheTimer.AutoReset = false;
                cacheTimer.Stop();
                cacheTimer.Close();
                cacheTimer.Dispose();
                if (_dormantCacheCount == 0 && ActiveCount == 0 && EmptyCacheEvent != null)
                {
                    EmptyCacheEvent(this, new EventArgs());
                }
            }
            return i;
        }
        /// <summary>
        /// Removes the entry from CacheCrow(Active+Dormant) corresponsing to the key.
        /// </summary>
        /// <param name="key">The key to corresponding value to remove</param>
        /// <returns>If removed then returns removed value as CacheData, else returns empty CacheData</returns>
        public CacheData<V> Remove(K key)
        {
            CacheData<V> i = new CacheData<V>();
            if (_cacheDic.ContainsKey(key) && (_timerDic.ContainsKey(key)))
            {
                var cacheTimer = _timerDic[key];
                _cacheDic.TryRemove(key, out i);
                cacheTimer.Elapsed -= new ElapsedEventHandler(Elapsed_Event);
                cacheTimer.Enabled = false;
                cacheTimer.AutoReset = false;
                cacheTimer.Stop();
                cacheTimer.Close();
                cacheTimer.Dispose();
                if (_dormantCacheCount == 0 && ActiveCount == 0 && EmptyCacheEvent != null)
                {
                    EmptyCacheEvent(this, new EventArgs());
                }
            }
            else
            {
                var dic = ReadBinary();
                if (dic.ContainsKey(key))
                {
                    dic.TryRemove(key, out i);
                }
            }
            return i;
        }
        /// <summary>
        /// Lookups the key in Active+Dormant CacheCrow, if found then increments the frequency.
        /// </summary>
        /// <param name="key">Key to corresponding value</param>
        /// <returns>If V is reference type and it is present then Object V else if V is value-type and it is not present then default value of V</returns>
        public V GetValue(K key)
        {
            ConcurrentDictionary<K, CacheData<V>> dic;
            if (ActiveLookUp(key))
            {
                return _cacheDic[key].Data;
            }
            else if ((dic = ReadBinary()) != null && dic.ContainsKey(key))
            {
                var cachedata = dic[key];
                cachedata.Frequency += 1;
                dic[key] = cachedata;
                WriteBinary(dic);
                PerformLFUAndReplace(key, cachedata);
                return cachedata.Data;
            }
            else
                return default(V);
        }
        /// <summary>
        /// Lookups the key in Active CacheCrow, if found then increments the frequency. Note: LFU maybe performed and entries maybe swapped between Active and Dormant CacheCrow.
        /// </summary>
        /// <param name="key">Key to corresponding value</param>
        /// <returns>If V is reference type and it is present then Object V else if V is value-type and it is not present then default value of V</returns>
        public V GetActiveValue(K key)
        {
            if (ActiveLookUp(key))
            {
                return _cacheDic[key].Data;
            }
            else return default(V);
        }
        /// <summary>
        /// Disposes and writes the entries in Active CacheCrow to Dormant CacheCrow.
        /// </summary>
        public void Dispose()
        {
            var dic = ReadBinary();
            if (dic.Count > _cacheDic.Count)
            {
                foreach (var t in _cacheDic)
                {
                    if (dic.ContainsKey(t.Key))
                    {
                        dic[t.Key] = t.Value;
                    }
                    else
                    {
                        dic.TryAdd(t.Key, t.Value);
                    }
                }
                WriteBinary(dic);
            }
            else
            {
                foreach (var t in dic)
                {
                    _cacheDic.TryAdd(t.Key, t.Value);
                }
                WriteBinary(_cacheDic);
            }
            foreach (var cacheTimer in _timerDic)
            {
                cacheTimer.Value.Elapsed -= new ElapsedEventHandler(Elapsed_Event);
                cacheTimer.Value.Enabled = false;
                cacheTimer.Value.AutoReset = false;
                cacheTimer.Value.Stop();
                cacheTimer.Value.Close();
                cacheTimer.Value.Dispose();
            }
            _cacheDic = null;
            _timerDic = null;
            mutex.Close();
        }
        static CacheCrow()
        {
            _cacheDic = new ConcurrentDictionary<K, CacheData<V>>();
            _timerDic = new ConcurrentDictionary<K, Timer>();
        }
        /// <summary>
        /// Loads entries from Dormant CacheCrow into Active CacheCrow
        /// </summary>
        protected void LoadCache()
        {
            var dic = ReadBinary();
            var orderderdic = dic.OrderByDescending(x => x.Value.Frequency).ToList();
            for (int i = 0; i < _cacheSize && i < orderderdic.Count; i++)
            {
                Add(orderderdic[i].Key, orderderdic[i].Value);
            }
            _cleaner.Start();
        }
        private CacheCrow(int size = 1000, int activeCacheExpire = 300000, int dormantCacheExpire = 500000, int cleanerSnoozeTime = 120000) : base()
        {
            Directory.CreateDirectory(_cacheDirectoryPath);
            var ds = new DirectorySecurity(_cacheDirectoryPath, AccessControlSections.Access);
            Directory.SetAccessControl(_cacheDirectoryPath, ds);
            _cacheSize = size;
            _dormantCacheExpire = dormantCacheExpire;
            _activeCacheExpire = activeCacheExpire;
            _dormantCacheCount = -1;
            _cleaner = new Timer(cleanerSnoozeTime);
            _cleaner.Elapsed += new ElapsedEventHandler(Cleaner_Event);
        }
        private void WriteBinary(K item, CacheData<V> value)
        {
            if (value == null)
            {
                return;
            }
            CacheData<V> i = new CacheData<V>();
            var dic = ReadBinary();
            dic.TryRemove(item, out i);
            dic.TryAdd(item, value);
            mutex.WaitOne(733);
            using (FileStream fs = new FileStream(_cachePath, FileMode.Create))
            {
                BinaryFormatter bf = new BinaryFormatter();
                bf.Serialize(fs, dic);
            }
            mutex.ReleaseMutex();
        }
        private void WriteBinary(ConcurrentDictionary<K, CacheData<V>> dic)
        {
            mutex.WaitOne(733);
            using (FileStream fs = new FileStream(_cachePath, FileMode.Create))
            {
                BinaryFormatter bf = new BinaryFormatter();
                bf.Serialize(fs, dic);
            }
            mutex.ReleaseMutex();
        }
        private void WriteCache()
        {
            mutex.WaitOne(733);
            using (FileStream fs = new FileStream(_cachePath, FileMode.Create))
            {
                BinaryFormatter bf = new BinaryFormatter();
                bf.Serialize(fs, _cacheDic);
            }
            mutex.ReleaseMutex();
        }
        private ConcurrentDictionary<K, CacheData<V>> ReadBinary()
        {
            ConcurrentDictionary<K, CacheData<V>> dic;
            try
            {
                mutex.WaitOne(1601);
                using (FileStream fs = new FileStream(_cachePath, FileMode.Open))
                {
                    BinaryFormatter bf = new BinaryFormatter();
                    dic = (ConcurrentDictionary<K, CacheData<V>>)bf.Deserialize(fs);
                    dic = new ConcurrentDictionary<K, CacheData<V>>(dic.Where(x => DateTime.Now.Subtract(x.Value.CreationDate).TotalMilliseconds < _dormantCacheExpire));
                    _dormantCacheCount = dic.Count();
                }
            }
            catch
            {
                dic = new ConcurrentDictionary<K, CacheData<V>>();
                _dormantCacheCount = dic.Count();
            }
            finally
            {
                mutex.ReleaseMutex();
            }
            return dic;
        }
        private void Add(K item, CacheData<V> cachedata, bool force = true)
        {
            if (cachedata == null)
            {
                return;
            }
            Add(item, cachedata.Data, cachedata.Frequency, force);
        }
        private void Add(K item, V data, int frequency, bool force = true)
        {
            if (item != null && !string.IsNullOrWhiteSpace(item.ToString()))
            {
                var cachedata = new CacheData<V>(data, frequency);
                if (ActiveCount < _cacheSize)
                {
                    if (_cacheDic.TryAdd(item, cachedata))
                    {
                        _cacheDic[item] = cachedata;
                        Timer t;
                        _timerDic.TryRemove(item, out t);
                        var timer = new CacheTimer<K>(item, _activeCacheExpire);
                        timer.Elapsed += new ElapsedEventHandler(Elapsed_Event);
                        timer.Start();
                        _timerDic.TryAdd(item, timer);
                        var dic = ReadBinary();
                        dic.TryRemove(item, out cachedata);
                        WriteBinary(dic);
                        return;
                    }
                    else
                    {
                        return;
                    }
                }
                else
                {
                    if (!force)
                    {
                        return;
                    }
                    if (PerformLFUAndReplace(item, cachedata) > -1)
                    {
                        return;
                    }
                    else
                    {
                        WriteBinary(item, cachedata);
                    }
                }
            }
        }
        /// <summary>
        /// Tries to add value from Dormant CacheCrow to Active CacheCrow using LFU.
        /// </summary>
        /// <returns>Returns frequency of added/removed entry</returns>
        protected int PerformLFUAndAdd()
        {

            CacheData<V> i = new CacheData<V>();
            i.Frequency = -1;
            var dic = new ConcurrentDictionary<K, CacheData<V>>(ReadBinary());
            if (dic.Count < 1)
                return -1;
            var pairlist = dic.OrderByDescending(x => x.Value.Frequency).ToList();
            for (int j = 0; j < pairlist.Count && ActiveCount <= _cacheSize; j++)
            {
                if (pairlist[j].Key != null && pairlist[j].Value != null)
                    Add(pairlist[j].Key, pairlist[j].Value, false);
            }
            return i.Frequency;
        }
        // <summary>
        /// Tries to replace value having key to Active CacheCrow or Dormant CacheCrow.
        /// </summary>
        /// <returns>Returns frequency of added/removed entry</returns>
        protected int PerformLFUAndReplace(K key, CacheData<V> value)
        {
            CacheData<V> i = new CacheData<V>();
            i.Frequency = -1;
            if (_cacheSize > _cacheDic.Count)
            {
                var dic = ReadBinary();

                var pair = dic.OrderByDescending(x => x.Value.Frequency).FirstOrDefault();
                if (pair.Key != null && pair.Key.Equals(key) || value == null)
                {
                    dic.TryRemove(pair.Key, out i);
                    WriteBinary(dic);
                    return PerformLFUAndReplace(key, value);
                }
                else if (pair.Key != null && pair.Value.Frequency > value.Frequency)
                {
                    Add(pair.Key, pair.Value);
                    dic.TryRemove(pair.Key, out i);
                    WriteBinary(dic);
                    return PerformLFUAndReplace(key, value);
                }
                else
                {
                    Add(key, value);
                    if (pair.Key != null)
                    {
                        PerformLFUAndReplace(pair.Key, pair.Value);
                    }
                    else
                        return -1;
                }
            }
            else
            {
                var pair = _cacheDic.OrderBy(x => x.Value.Frequency).FirstOrDefault();
                if (pair.Value.Frequency < value.Frequency || pair.Value.Frequency < 2)
                {
                    i = ActiveRemove(pair.Key);
                    if (i.Frequency > -1) // has been removed condition, add to new key to cache and add removed key to disk.
                    {
                        WriteBinary(pair.Key, pair.Value);
                        Add(key, value);
                    }
                }
                else
                {
                    if (key != null && !string.IsNullOrWhiteSpace(key.ToString()))
                    {
                        WriteBinary(key, value);
                    }
                }
            }
            return i.Frequency;
        }
        private bool DeepLookUp(K item)
        {
            Console.WriteLine("Performing Deep Lookup..");
            var dic = ReadBinary();
            if (dic.ContainsKey(item))
            {
                var cachedata = dic[item];
                cachedata.Frequency += 1;
                cachedata.CreationDate = DateTime.Now;
                dic[item] = cachedata;
                WriteBinary(dic);
                PerformLFUAndReplace(item, cachedata);
                return true;
            }
            else
            {
                return false;
            }
        }
        private void Cleaner_Event(object sender, ElapsedEventArgs e)
        {
            System.Threading.Thread cleaner = new System.Threading.Thread(() => {
                var dic = ReadBinary();
                WriteBinary(dic);
                if (_dormantCacheCount == 0 && ActiveCount == 0 && EmptyCacheEvent != null)
                {
                    EmptyCacheEvent(this, new EventArgs());
                }
            });
            cleaner.Start();
        }
        private void Elapsed_Event(object sender, ElapsedEventArgs e)
        {
            System.Threading.Thread cachecleaner = new System.Threading.Thread(() => {
                CacheData<V> i = new CacheData<V>();
                var cachetimer = (CacheTimer<K>)sender;
                cachetimer.Elapsed -= new ElapsedEventHandler(Elapsed_Event);
                cachetimer.Close();
                _cacheDic.TryRemove(cachetimer.key, out i);
                if (_dormantCacheCount == 0 && ActiveCount == 0 && EmptyCacheEvent != null)
                {
                    EmptyCacheEvent(this, new EventArgs());
                }
                else if (_dormantCacheCount != 0)
                {
                    var pair = ReadBinary().OrderByDescending(x => x.Value.Frequency).FirstOrDefault();
                    PerformLFUAndReplace(pair.Key, pair.Value);
                }
            });
            cachecleaner.Start();
        }
    }
}
