using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EdmLoadTester
{
    using ODataDemo;

    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                var add = new Address(){City = "Redmond"};
                var entities = new Entities();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
           
        }
    }
}
