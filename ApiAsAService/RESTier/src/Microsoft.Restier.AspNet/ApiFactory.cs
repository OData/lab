using Microsoft.AspNet.OData.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Restier.Core;
using Microsoft.Restier.Core.Model;
using Microsoft.Restier.EntityFramework;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web;

namespace Microsoft.Restier.AspNet
{
    /// <summary>
    /// API Factory
    /// </summary>
    public class ApiFactory
    {
        private IServiceProvider serviceProvider;
        private ApiBase apiBase;

        /// <summary>
        /// Factory for creating an APIBase
        /// </summary>
        /// <param name="serviceProvider"></param>
        public ApiFactory(IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
        }

        /// <summary>
        /// ModelType
        /// </summary>
        public Type ModelType {get; set;}

        /// <summary>
        /// Get ApiBase
        /// </summary>
        /// <returns></returns>
        public ApiBase GetApiBase()
        {
            if (apiBase == null)
            {
                if (ModelType == null)
                {
                    ModelType = typeof(DbContext);
                    return generateApiBase(ModelType, serviceProvider);
                }

                apiBase = generateApiBase(ModelType, serviceProvider);
            }

            return apiBase;
        }

        private static ApiBase generateApiBase(Type modelType, IServiceProvider serviceProvider)
        {
            return (ApiBase)typeof(EntityFrameworkApi<>).MakeGenericType(modelType).GetConstructor(new Type[] { typeof(IServiceProvider) }).Invoke(new object[] { serviceProvider });
        }
    }
}