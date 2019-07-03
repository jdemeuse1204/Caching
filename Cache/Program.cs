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
            var test = new TestClass();
            var otherStore = new Cachable<TestClass>(test);

            var x = otherStore.Resolve(w => w.Test(1, "Two"), 100000);
        }


    }

    public class Me
    {
        public int One { get; set; }
        public int Two { get; set; }
    }

    public class TestClass
    {
        public object Test(int one, string two)
        {

            return "Test";
        }
    }
}
