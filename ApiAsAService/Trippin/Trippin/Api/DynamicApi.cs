// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.OData.Query;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OData.Edm;
using Microsoft.OData.Edm.Vocabularies;
using Microsoft.OData.Service.ApiAsAService.Models;
using Microsoft.OData.Service.ApiAsAService.Submit;
using Microsoft.Restier.Core;
using Microsoft.Restier.Core.Model;
using Microsoft.Restier.Core.Submit;
using Microsoft.Restier.EntityFramework;
using Microsoft.Restier.AspNet.Model;
using Microsoft.OData.Edm.Csdl;
using System.Xml;
using Microsoft.OData.Edm.Validation;
using Microsoft.AspNet.OData.Routing;
using System.Web.Http;
using Microsoft.Restier.AspNet.Batch;
using Microsoft.Restier.AspNet;
using Microsoft.AspNet.OData.Extensions;
using Microsoft.AspNet.OData.Batch;
using Microsoft.AspNet.OData.Routing.Conventions;

namespace Microsoft.OData.Service.ApiAsAService.Api
{
    public class DynamicApi<T> : EntityFrameworkApi<T> where T : System.Data.Entity.DbContext
    {
        public T ModelContext { get { return DbContext; } }

         public static new IServiceCollection ConfigureApi(Type apiType, IServiceCollection services)
        {
            // Add customized OData validation settings 
            Func<IServiceProvider, ODataValidationSettings> validationSettingFactory = (sp) => new ODataValidationSettings
            {
                MaxAnyAllExpressionDepth = 3,
                MaxExpansionDepth = 3
            };

            IServiceCollection serviceCollection = EntityFrameworkApi<T>.ConfigureApi(apiType, services)
                .AddSingleton<ODataPayloadValueConverter, CustomizedPayloadValueConverter>()
                .AddSingleton<ODataValidationSettings>(validationSettingFactory)
                .AddService<IChangeSetItemFilter, CustomizedSubmitProcessor>()
                .AddService<IModelBuilder, DynamicModelBuilder>();

            return serviceCollection;
        }


        private class DynamicModelBuilder : IModelBuilder
        {
            public IModelBuilder InnerHandler { get; set; }

            public async Task<IEdmModel> GetModelAsync(ModelContext context, CancellationToken cancellationToken)
            {
                IEdmModel model;
                IEnumerable<EdmError> errors;
                var appData = System.Web.HttpContext.Current.Server.MapPath("~/App_Data");
                var file = System.IO.Path.Combine(appData, "Trippin.xml");

                XmlReader xmlReader = XmlReader.Create(file);
                if (CsdlReader.TryParse(xmlReader, out model, out errors))
                {
                    return model;
                }

                throw new Exception("Couldn't parse xml");
            }
        }

        public DynamicApi(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }
    }
}