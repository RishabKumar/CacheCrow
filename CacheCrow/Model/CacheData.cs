using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        /// Initializes CacheData with data of type V
        /// </summary>
        /// <param name="data">User data</param>
        public CacheData(V data)
        {
            this.Data = data;
            CreationDate = DateTime.Now;
        }
        /// <summary>
        /// Initializes CacheData with data of type V and user defined frequency.
        /// </summary>
        /// <param name="data">User data</param>
        /// <param name="frequency">Frequency</param>
        public CacheData(V data, int frequency)
        {
            this.Data = data;
            this.Frequency = frequency;
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
