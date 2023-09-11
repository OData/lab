// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.OData.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Restier.AspNet;

namespace Microsoft.OData.Service.ApiAsAService.Controllers
{
    public class DynamicApiController : RestierController
    {
        public async Task<HttpResponseMessage> Get(CancellationToken cancellationToken)
        {

            IServiceProvider serviceProvider = Request.GetRequestContainer();
            ApiFactory factory = Request.GetRequestContainer().GetRequiredService<ApiFactory>();
            base.SetApi(factory.GetApiBase());

            return await base.GetResponse(cancellationToken);
        }
    }
}