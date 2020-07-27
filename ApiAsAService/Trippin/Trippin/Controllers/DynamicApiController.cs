// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using Microsoft.AspNet.OData;
using Microsoft.AspNet.OData.Extensions;
using Microsoft.AspNet.OData.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OData.Edm;
using Microsoft.OData.Service.ApiAsAService.Api;
using Microsoft.Restier.AspNet;
using Microsoft.Restier.Core;
using Microsoft.Restier.Core.Model;

namespace Microsoft.OData.Service.ApiAsAService.Controllers
{
    public class DynamicApiController : RestierController
    {
        public async Task<HttpResponseMessage> Get(CancellationToken cancellationToken)
        {

            IServiceProvider serviceProvider = Request.GetRequestContainer();
            ApiFactory factory = Request.GetRequestContainer().GetRequiredService<ApiFactory>();
            //factory.ModelType = dynamicType;
            base.SetApi(factory.GetApiBase());

            return await base.GetResponse(cancellationToken);
        }
    }
}