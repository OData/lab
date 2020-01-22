using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.OData.ConnectedService.Templates
{
    class ODataT4CodeGeneratorFactory: IODataT4CodeGeneratorFactory
    {
        public ODataT4CodeGenerator Create()
        {
            return new ODataT4CodeGenerator();
        }
    }
}
