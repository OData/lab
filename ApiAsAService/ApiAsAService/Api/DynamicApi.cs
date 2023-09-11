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
using Microsoft.OData.Service.ApiAsAService.Submit;
using Microsoft.Restier.Core;
using Microsoft.Restier.Core.Model;
using Microsoft.Restier.Core.Submit;
using Microsoft.Restier.EntityFramework;
using Microsoft.OData.Edm.Csdl;
using System.Xml;
using Microsoft.OData.Edm.Validation;


namespace Microsoft.OData.Service.ApiAsAService.Api
{
    public class DynamicApi : ApiBase
    {
        public static new IServiceCollection ConfigureApi(Type apiType, IServiceCollection services)
        {
            // Add customized OData validation settings 
            Func<IServiceProvider, ODataValidationSettings> validationSettingFactory = (sp) => new ODataValidationSettings
            {
                MaxAnyAllExpressionDepth = 3,
                MaxExpansionDepth = 3
            };

            IServiceCollection serviceCollection = ApiBase.ConfigureApi(apiType, services)
//                .AddSingleton<ODataPayloadValueConverter, CustomizedPayloadValueConverter>()
                .AddSingleton<ODataValidationSettings>(validationSettingFactory)
                .AddService<IChangeSetItemFilter, CustomizedSubmitProcessor>()
                .AddSingleton<IModelBuilder, DynamicModelBuilder>()
                .AddScoped<IEdmModel>(sp => ((DynamicModelBuilder)sp.GetRequiredService<IModelBuilder>()).GetModel());

            return serviceCollection;
        }

        public DynamicApi(IServiceProvider serviceProvider) : base(serviceProvider) { }
    }

    public class DynamicModelBuilder : IModelBuilder
    { 
        public IModelBuilder InnerHandler { get; set; }

        public string DataSourceName { get; set; }
        public IEdmModel GetModel()
        {
            IEdmModel model;
            IEnumerable<EdmError> errors;
            var appData = System.Web.HttpContext.Current.Server.MapPath("~/App_Data");
            var file = System.IO.Path.Combine(appData, DataSourceName + ".xml");

            XmlReader xmlReader = XmlReader.Create(file);
            if (CsdlReader.TryParse(xmlReader, out model, out errors))
            {
                return model;
            }

            throw new Exception("Couldn't parse xml");
        }

        public async Task<IEdmModel> GetModelAsync(ModelContext context, CancellationToken cancellationToken)
        {
            return await Task.FromResult(GetModel());
        }

    }
}