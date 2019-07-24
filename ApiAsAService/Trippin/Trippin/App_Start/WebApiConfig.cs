// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System.Web.Http;
using Microsoft.AspNet.OData.Extensions;
using Microsoft.AspNet.OData.Routing;
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
            //await config.MapRestierRoute<DynamicApi<Models.TrippinModel>>(
            //    "ApiAsAService", "",
            //    new RestierBatchHandler(server));

            HttpConfiguration dummyConfig = new HttpConfiguration();
            var services = dummyConfig.Services;

            ODataRoute route = await DynamicHelper.MapDynamicRoute(
                typeof(Models.TrippinModel), 
                dummyConfig, 
                "RestierRoute", 
                "", 
                null);

            foreach(var service in dummyConfig.Services.GetServices(typeof(object)))
            {
                config.Services.Add(service.GetType(), service);
            }

            DynamicODataRoute odataRoute = new DynamicODataRoute(route.RoutePrefix, new DynamicRouteConstraint("RestierRoute"));
   //         config.Routes.Remove("RestierRoute");
            config.Routes.Add("DynamicRoute", odataRoute);
        }
    }
}
