// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System.Web.Http;
using Microsoft.AspNet.OData.Extensions;
using Microsoft.OData.Service.ApiAsAService.Api;
using Microsoft.Restier.AspNet.Batch;

namespace Microsoft.OData.Service.ApiAsAService
{
    public static class WebApiConfig
    {
        public static void Register(HttpConfiguration config)
        {
            RegisterService(config, GlobalConfiguration.DefaultServer);
        }

        public static async void RegisterService(
            HttpConfiguration config, HttpServer server)
        {
            // enable query options for all properties
            config.Filter().Expand().Select().OrderBy().MaxTop(null).Count();
            await config.MapRestierRoute<DynamicApi>(
                "ApiAsAService", "",
                new RestierBatchHandler(server));
        }

        
    }
}
