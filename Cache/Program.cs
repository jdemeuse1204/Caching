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
            test.GetOne();
            test.GetOne();
        }


    }

    public class Me
    {
        public int One { get; set; }
        public int Two { get; set; }
    }

    public class TestClass
    {
        public int GetOne()
        {
            return this.Resolve(w => w.Test(1, "two"), 100000000);
        }

        public int Test(int one, string two)
        {

            return 1;
        }
    }
}
