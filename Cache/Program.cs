using Easy.Cache;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cache
{
    class Program
    {

        static void Main(string[] args)
        {
            var cacheStore = new CacheStore<Program>();

            var test = new TestClass();

            test.Test(1, new Me
            {
                One = 2,
                Two = 1
            });

        }


    }

    public class Me
    {
        public int One { get; set; }
        public int Two { get; set; }
    }

    public class TestClass
    {
        CacheStore<TestClass> cacheStore = new CacheStore<TestClass>();

        public object Test(int one, string two)
        {
            var result = cacheStore.Resolve(() =>
            {
                return 1;
            }, 8000000, w => w.Test(one, two));

            var keys = cacheStore.Keys().ToList();

            cacheStore.Remove(w => w.Test(one, two));

            return result;
        }

        public object Test(int one, Me me)
        {
            var result = cacheStore.Resolve(() =>
            {
                return 1;
            }, 8000000, w => w.Test(one, me));

            var keys = cacheStore.Keys().ToList();

            cacheStore.Remove(w => w.Test(one, me));

            return result;
        }
    }
}
