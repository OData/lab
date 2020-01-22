using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.OData.ConnectedService.Templates
{
    interface IODataT4CodeGeneratorFactory
    {
        ODataT4CodeGenerator Create();
    }
}
