using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Easy.Cache
{
    public interface ICacheStore<TRegion> where TRegion : class
    {
        int TotalKeys { get; }
        TResult Resolve<TResult>(Func<TResult> action, int secondsTimeout, Expression<Func<TRegion, TResult>> callingMethod, bool cloneResult = true, [CallerMemberName] string methodName = null);
        void Remove(Expression<Func<TRegion, object>> remove);
        bool Remove(string key);
        bool Remove(string methodName, object[] filterIds);
        IEnumerable<string> Keys();
        List<string> Bust();
    }
}
