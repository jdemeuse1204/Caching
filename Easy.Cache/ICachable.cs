using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Easy.Cache
{
    public interface ICachable<TRegion> where TRegion : class
    {
        int TotalKeys { get; }
        TResult Resolve<TResult>(Expression<Func<TRegion, TResult>> callingMethod, int secondsTimeout, bool cloneResult = true);
        void Remove<TResult>(Expression<Func<TRegion, TResult>> remove);
        bool Remove(string key);
        bool Remove(string methodName, object[] filterIds);
        IEnumerable<string> Keys();
        List<string> Bust();
    }
}
