using Microsoft.AspNet.OData.Routing;
using Microsoft.OData.Service.ApiAsAService.Api;
using Microsoft.Restier.AspNet.Batch;
using Microsoft.Restier.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Web.Http;

namespace Microsoft.OData.Service.ApiAsAService
{
    public static class DynamicHelper
    {
        private const string MapRestierRouteMethod = "MapRestierRoute";
        private const string RestierAssembly = "Microsoft.Restier.AspNet";
        private const string httpConfigurationExtensionsType = "System.Web.Http.HttpConfigurationExtensions";
        private const string ApiAsAServiceAssembly = "Microsoft.OData.Service.ApiAsAService";
        private const string DynamicApiType = "Microsoft.OData.Service.ApiAsAService.Api.DynamicApi`1";
        private static Type dynamicApiType = Assembly.GetExecutingAssembly().GetType(DynamicApiType);
        
        public static Task<ODataRoute> MapDynamicRoute(Type tApi, HttpConfiguration config, string routeName,
            string routePrefix, RestierBatchHandler batchHandler)
        {
        Type restierType = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.GetName().Name == RestierAssembly).GetType(httpConfigurationExtensionsType);
        Type dynamicType = dynamicApiType.MakeGenericType(tApi);
            MethodInfo method = restierType.GetMethod(
                 MapRestierRouteMethod,
                 BindingFlags.Static | BindingFlags.Public,
                 null,
                 new[] { typeof(HttpConfiguration), typeof(string), typeof(string), typeof(RestierBatchHandler) },
                 null);
            return (Task<ODataRoute>)method.MakeGenericMethod(dynamicType).Invoke(null, new object[] { config, routeName, routePrefix, batchHandler });
        }

        public static ApiBase CreateDynamicApi(Type tApi, IServiceProvider serviceProvider)
        {
           return (ApiBase)dynamicApiType.MakeGenericType(tApi).GetConstructor(new Type[] { typeof(IServiceProvider) }).Invoke(new object[] { serviceProvider });
        }

        //private static Type GetDynamicApiType()
        //{
        //    if (dynamicApiType == null)
        //    {
        //        dynamicApiType = Assembly.GetExecutingAssembly().GetType(DynamicApiType);
        //    }

        //    return dynamicApiType;
        //}
    }
}