using System;

namespace CacheCrow.Model
{
    /// <summary>
    /// Represents User's data value, its frequency and creation date.
    /// </summary>
    /// <typeparam name="V">Value type</typeparam>
    [Serializable]
    public class CacheData<V>
    {
        /// <summary>
        /// The count of how many times it was searched.
        /// </summary>
        public int Frequency = 1;
        /// <summary>
        /// User data
        /// </summary>
        public V Data;
        /// <summary>
        /// Time at which the CacheData was created.
        /// </summary>
        public DateTime CreationDate;
        /// <summary>
        /// Time at which the CacheData was modified.
        /// </summary>
        public DateTime ModifiedDate;

        /// <summary>
        /// 
        /// </summary>
        public Func<V> OnExpire { get; set; }
        /// <summary>
        /// Initializes CacheData with data of type V
        /// </summary>
        /// <param name="data">User data</param>
        public CacheData(V data)
        {
            Data = data;
            CreationDate = DateTime.Now;
        }
        /// <summary>
        /// Initializes CacheData with data of type V and user defined frequency.
        /// </summary>
        /// <param name="data">User data</param>
        /// <param name="frequency">Frequency</param>
        public CacheData(V data, int frequency)
        {
            Data = data;
            Frequency = frequency;
            CreationDate = DateTime.Now;
        }
        /// <summary>
        /// Constructor
        /// </summary>
        public CacheData()
        {
            CreationDate = DateTime.Now;
        }
    }
}
