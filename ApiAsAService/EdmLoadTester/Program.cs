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
                using (var context = new Entities())
                {
                    var address = context.Supplier.Create();
                    address.Concurrency = 2;
                }

                using (var context = new Entities())
                {
                    var address = context.Supplier.ToList();
                    if (address.Count != 1)
                    {
                        throw new Exception("Error");
                    }

                    var isTrue = address.FirstOrDefault().Concurrency.Equals(2);
                }

            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }

        }
    }
}
