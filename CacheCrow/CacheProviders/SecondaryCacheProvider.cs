using CacheCrow.Cache;
using System;
using System.Collections.Specialized;
using System.Configuration;

namespace CacheCrow.CacheProviders
{
    public static class SecondaryCacheProvider<K, V>
    {
        private const string CacheKey = "SecondaryCache";
        private const string ExpireTimeKey = "SecondaryCacheExpireTime";
        private const double DefaultExpireInMilliseconds = 500000;
        private static ISecondaryCache<K, V> _secondaryCache;
        public static ISecondaryCache<K, V> GetSecondaryCache()
        {
            if (_secondaryCache != null)
            {
                return _secondaryCache;
            }
            var secondaryCacheType = typeof(DefaultSecondaryCache<K, V>);
            var cacheExpireInMilliseconds = DefaultExpireInMilliseconds;
            var section = GetCacheSectionFromConfig();
            if (section != null && section.Count > 0)
            {
                var cacheFullQualifiedName = section[ CacheKey ];
                var expireTime = section[ ExpireTimeKey ];
                cacheExpireInMilliseconds = double.TryParse(expireTime, out var expire) ? expire : cacheExpireInMilliseconds;
                if (!string.IsNullOrWhiteSpace(cacheFullQualifiedName))
                {
                    secondaryCacheType = Type.GetType(cacheFullQualifiedName);
                }
            }
            return CreateInstance(secondaryCacheType, cacheExpireInMilliseconds);
        }


        private static NameValueCollection GetCacheSectionFromConfig()
        {
            var section = (NameValueCollection)ConfigurationManager.GetSection("cacheCrow");
            return section;
        }

        private static ISecondaryCache<K, V> CreateInstance(Type type, double cacheExpireInMilliseconds)
        {
            if (type.IsGenericTypeDefinition)
            {
                type = type.MakeGenericType(typeof(K), typeof(V));  
            }
            _secondaryCache = Activator.CreateInstance(type, cacheExpireInMilliseconds) as ISecondaryCache<K, V>;
            return _secondaryCache;
        }
    }

}
