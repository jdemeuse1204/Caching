using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Easy.Cache
{
    public class TestRepository
    {
        public int GetOne()
        {
            var result = CacheStore.Resolve(this, w => w.GetOne(), 1);
            return 1;
        }
    }
}
