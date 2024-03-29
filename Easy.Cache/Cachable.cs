﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.Caching;
using System.Web.Script.Serialization;

namespace Easy.Cache
{
    public static class Cachable
    {
        private static readonly JavaScriptSerializer JavaScriptSerializer = new JavaScriptSerializer();
        private static readonly ObjectCache Cache = MemoryCache.Default;

        public static int TotalKeys { get { return Cache.Count(); } }

        public static TResult Resolve<TResult, TRegion>(this TRegion region, Expression<Func<TRegion, TResult>> callingMethod, int secondsTimeout, bool cloneResult = true) where TRegion : class
        {
            lock (Cache)
            {
                // If no key, grab data and cache it.
                // If key return it
                var cacheMethodInformation = GetCacheMethodInformation(callingMethod);
                var regionName = typeof(TRegion).Name;

                var cacheKey = CreateCacheKey(cacheMethodInformation.MethodName, regionName, cacheMethodInformation.ParameterValues);

                if (Cache.Contains(cacheKey))
                {
                    // return a clone otherwise cache can get changed by ref
                    var cachedValue = (TResult)Cache.Get(cacheKey);
                    return cloneResult ? cachedValue.Copy() : cachedValue;
                }

                var action = callingMethod.Compile();
                var result = action(region);

                if (result == null)
                {
                    return result;
                }

                // do not cache nulls
                Cache.Add(cacheKey, result, DateTime.Now.AddSeconds(secondsTimeout));

                return cloneResult ? result.Copy() : result;
            }
        }

        public static bool Remove<TResult, TRegion>(Expression<Func<TRegion, TResult>> remove) where TRegion : class
        {
            var methodInformation = GetCacheMethodInformation(remove);
            var regionName = typeof(TRegion).Name;

            return Remove(methodInformation.MethodName, regionName, methodInformation.ParameterValues);
        }

        public static bool Remove(string key, string regionName)
        {
            lock (Cache)
            {
                // If no key grab data and cache it
                // if key return it
                var cacheKey = CreateCacheKey(key, regionName, new object[] { });

                if (Cache.Contains(cacheKey))
                {
                    Cache.Remove(cacheKey);
                    return true;
                }

                return false;
            }
        }

        public static bool Remove(string methodName, string regionName, object[] filterIds)
        {
            lock (Cache)
            {
                // If no key grab data and cache it
                // if key return it
                var cacheKey = CreateCacheKey(methodName, regionName, filterIds);

                string foundKey = Cache.Select(kvp => kvp.Key).FirstOrDefault(w => w == cacheKey);

                if (foundKey != null && Cache.Contains(foundKey))
                {
                    Cache.Remove(cacheKey);
                    return true;
                }

                return false;
            }
        }

        public static IEnumerable<string> Keys()
        {
            foreach (var item in Cache)
            {
                yield return item.Key;
            }
        }

        public static List<string> Bust()
        {
            lock (Cache)
            {
                var removed = new List<string>();
                foreach (var item in Cache)
                {
                    removed.Add(item.Key);
                    Cache.Remove(item.Key);
                }

                return removed;
            }
        }

        private static string CreateCacheKey(string methodName, string regionName, object[] filterIds)
        {
            var aggregatedFilterIds = CreateAggregatedIds(filterIds);
            var idFilter = aggregatedFilterIds != null && aggregatedFilterIds.Count > 0 ? $"{string.Join(",", aggregatedFilterIds)}" : string.Empty;
            return $"{regionName}.{methodName}({idFilter})";
        }

        private static List<object> CreateAggregatedIds(object[] filterIds)
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

        private static string Serialize<T>(T value)
        {
            if (value == null)
            {
                return string.Empty;
            }

            return JavaScriptSerializer.Serialize(value);
        }

        private static CacheMethodInformation GetCacheMethodInformation<TResult, TRegion>(Expression<Func<TRegion, TResult>> method) where TRegion : class
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
