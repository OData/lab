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
using Microsoft.OData.Service.ApiAsAService.Models;
using Microsoft.Restier.AspNet;
using Microsoft.Restier.Core;

namespace Microsoft.OData.Service.ApiAsAService.Controllers
{
    public class DynamicApiController : RestierController
    {
        public async Task<HttpResponseMessage> Get(CancellationToken cancellationToken)
        {
            // Get the data source from the request
            ODataPath path = Request.ODataProperties().Path;
            string dataSource = path.Segments[0].Identifier;
            
            //IEdmCollectionType collectionType = (IEdmCollectionType)path.EdmType;
            //IEdmEntityTypeReference entityType = collectionType.ElementType.AsEntity();

            Type dynamicType = dataSource=="Trippin" ? typeof(Models.TrippinModel) : typeof(Models.TrippinModel);
            base.SetApi(DynamicHelper.CreateDynamicApi(dynamicType, Request.GetRequestContainer()));

            return await base.GetResponse(cancellationToken);
        }
    }
}