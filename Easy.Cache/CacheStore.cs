using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.Caching;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace Easy.Cache
{
    public static class CacheStore
    {
        private static readonly ObjectCache WebApiCache = MemoryCache.Default;
        private static readonly JavaScriptSerializer JavaScriptSerializer = new JavaScriptSerializer();

        public static TResult Resolve<TResult, TRegion>(TRegion region, Expression<Func<TRegion, TResult>> callingMethod, int secondsTimeout, bool cloneResult = true)
        {
            lock (WebApiCache)
            {
                // If no key, grab data and cache it.
                // If key return it
                var cacheMethodInformation = GetCacheMethodInformation(callingMethod);

                var regionName = typeof(TRegion).Name;

                var cacheKey = CreateCacheKey(cacheMethodInformation.MethodName, regionName, cacheMethodInformation.ParameterValues);

                if (WebApiCache.Contains(cacheKey))
                {
                    // return a clone otherwise cache can get changed by ref
                    var cachedValue = (TResult)WebApiCache.Get(cacheKey);
                    return cloneResult ? cachedValue.Copy() : cachedValue;
                }

                var action = callingMethod.Compile();
                var result = action(region);

                if (result == null)
                {
                    return result;
                }

                // do not cache nulls
                WebApiCache.Add(cacheKey, result, DateTime.Now.AddSeconds(secondsTimeout));

                return cloneResult ? result.Copy() : result;
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

        private static CacheMethodInformation GetCacheMethodInformation<TResult, TRegion>(Expression<Func<TRegion, TResult>> method)
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
