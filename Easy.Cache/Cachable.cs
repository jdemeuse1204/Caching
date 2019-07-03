using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.Caching;
using System.Runtime.CompilerServices;
using System.Web.Script.Serialization;

namespace Easy.Cache
{
    public class Cachable<TRegion> : ICachable<TRegion> where TRegion : class
    {
        private readonly ObjectCache WebApiCache = MemoryCache.Default;
        private readonly JavaScriptSerializer JavaScriptSerializer = new JavaScriptSerializer();
        private readonly TRegion RegionInstance;

        public int TotalKeys { get { return WebApiCache.Count(); } }

        public Cachable(TRegion regionInstance)
        {
            RegionInstance = regionInstance;
        }

        public TResult NoCache<TResult>(Expression<Func<TRegion, TResult>> callingMethod)
        {
            var action = callingMethod.Compile();
            return action(RegionInstance);
        }

        public TResult Resolve<TResult>(Expression<Func<TRegion, TResult>> callingMethod, int secondsTimeout, bool cloneResult = true)
        {
            lock (WebApiCache)
            {
                // If no key, grab data and cache it.
                // If key return it
                var cacheMethodInformation = GetCacheMethodInformation(callingMethod);

                var cacheKey = CreateCacheKey(cacheMethodInformation.MethodName, cacheMethodInformation.ParameterValues);

                if (WebApiCache.Contains(cacheKey))
                {
                    // return a clone otherwise cache can get changed by ref
                    var cachedValue = (TResult)WebApiCache.Get(cacheKey);
                    return cloneResult ? cachedValue.Copy() : cachedValue;
                }

                var action = callingMethod.Compile();
                var result = action(RegionInstance);

                if (result == null)
                {
                    return result;
                }

                // do not cache nulls
                WebApiCache.Add(cacheKey, result, DateTime.Now.AddSeconds(secondsTimeout));

                return cloneResult ? result.Copy() : result;
            }
        }

        public void Remove<TResult>(Expression<Func<TRegion, TResult>> remove)
        {
            var methodInformation = GetCacheMethodInformation(remove);

            Remove(methodInformation.MethodName, methodInformation.ParameterValues);
        }

        public bool Remove(string key)
        {
            lock (WebApiCache)
            {
                if (WebApiCache.Contains(key))
                {
                    WebApiCache.Remove(key);
                    return true;
                }

                return false;
            }
        }

        public bool Remove(string methodName, object[] filterIds)
        {
            lock (WebApiCache)
            {
                // If no key grab data and cache it
                // if key return it
                var cacheKey = CreateCacheKey(methodName, filterIds);

                string foundKey = WebApiCache.Select(kvp => kvp.Key).FirstOrDefault(w => w == cacheKey);

                if (foundKey != null && WebApiCache.Contains(foundKey))
                {
                    WebApiCache.Remove(cacheKey);
                    return true;
                }

                return false;
            }
        }

        public IEnumerable<string> Keys()
        {
            foreach (var item in WebApiCache)
            {
                yield return item.Key;
            }
        }

        public List<string> Bust()
        {
            lock (WebApiCache)
            {
                var removed = new List<string>();
                foreach (var item in WebApiCache)
                {
                    removed.Add(item.Key);
                    WebApiCache.Remove(item.Key);
                }

                return removed;
            }
        }

        private string CreateCacheKey(string methodName, object[] filterIds)
        {
            var aggregatedFilterIds = CreateAggregatedIds(filterIds);
            var idFilter = aggregatedFilterIds != null && aggregatedFilterIds.Count > 0 ? $"{string.Join(",", aggregatedFilterIds)}" : string.Empty;
            return $"{typeof(TRegion).Name}.{methodName}({idFilter})";
        }

        private List<object> CreateAggregatedIds(object[] filterIds)
        {
            List<object> result = new List<object>();

            if (filterIds == null) { return result; }

            foreach (var item in filterIds)
            {
                if (item is IEnumerable enumberable && !(item is string))
                {
                    foreach (var subItem in enumberable)
                    {
                        result.Add(subItem);
                    }
                    continue;
                }

                // is string or struct
                if ((item is string) || item.GetType().IsValueType)
                {
                    result.Add(item);
                    continue;
                }

                // Serialize a class to xml
                result.Add(Serialize(item));
            }

            return result;
        }

        private string Serialize<T>(T value)
        {
            if (value == null)
            {
                return string.Empty;
            }

            return JavaScriptSerializer.Serialize(value);
        }

        private CacheMethodInformation GetCacheMethodInformation<TResult>(Expression<Func<TRegion, TResult>> method)
        {
            var parameterValues = new List<object>();
            var result = new CacheMethodInformation();

            if (method.Body is MethodCallExpression expression)
            {
                result.MethodName = expression.Method.Name;

                if (expression.Arguments.Count > 0)
                {
                    foreach (var argument in expression.Arguments)
                    {
                        if (argument is ConstantExpression constantExpression)
                        {
                            parameterValues.Add(constantExpression.Value);
                            continue;
                        }

                        if (argument is MemberExpression memberExpression)
                        {
                            var instance = ((ConstantExpression)memberExpression.Expression).Value;
                            parameterValues.Add(instance.GetType().GetField(memberExpression.Member.Name).GetValue(instance));
                            continue;
                        }
                    }
                }

                result.ParameterValues = parameterValues.ToArray();

                return result;
            }

            throw new Exception("Expression is not a method call expression.  eg - Remove(x => x.SomeMethod(1))");
        }

        private class CacheMethodInformation
        {
            public string MethodName { get; set; }
            public object[] ParameterValues { get; set; }
        }
    }
}
