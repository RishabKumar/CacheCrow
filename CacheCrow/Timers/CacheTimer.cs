using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace CacheCrow.Timers
{
    internal class CacheTimer<K> : Timer
    {
        public K key { get; set; }
        public CacheTimer(K key, double interval = 60000) : base(interval)
        {
            this.key = key;
        }
    }
}
