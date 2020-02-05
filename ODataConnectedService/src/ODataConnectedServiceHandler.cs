﻿// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using EnvDTE;
using Microsoft.OData.ConnectedService.CodeGeneration;
using Microsoft.OData.ConnectedService.Templates;
using Microsoft.OData.ConnectedService.Common;
using Microsoft.OData.ConnectedService.Models;
using Microsoft.VisualStudio.ConnectedServices;

namespace Microsoft.OData.ConnectedService
{
    [ConnectedServiceHandlerExport(Common.Constants.ProviderId, AppliesTo = "CSharp")]
    internal class ODataConnectedServiceHandler : ConnectedServiceHandler
    {
        public override async Task<AddServiceInstanceResult> AddServiceInstanceAsync(ConnectedServiceHandlerContext context, CancellationToken ct)
        {
            Project project = ProjectHelper.GetProjectFromHierarchy(context.ProjectHierarchy);
            ODataConnectedServiceInstance codeGenInstance = (ODataConnectedServiceInstance)context.ServiceInstance;

            var codeGenDescriptor = await GenerateCode(codeGenInstance.MetadataTempFilePath, codeGenInstance.ServiceConfig.EdmxVersion, context, project);

            context.SetExtendedDesignerData<ServiceConfiguration>(codeGenInstance.ServiceConfig);

            var result = new AddServiceInstanceResult(
                context.ServiceInstance.Name,
                new Uri(codeGenDescriptor.ClientDocUri));

            return result;
        }

        public override async Task<UpdateServiceInstanceResult> UpdateServiceInstanceAsync(ConnectedServiceHandlerContext context, CancellationToken ct)
        {
            Project project = ProjectHelper.GetProjectFromHierarchy(context.ProjectHierarchy);
            ODataConnectedServiceInstance codeGenInstance = (ODataConnectedServiceInstance)context.ServiceInstance;

            var codeGenDescriptor = await GenerateCode(codeGenInstance.ServiceConfig.Endpoint, codeGenInstance.ServiceConfig.EdmxVersion, context, project);
            context.SetExtendedDesignerData<ServiceConfiguration>(codeGenInstance.ServiceConfig);
            return new UpdateServiceInstanceResult();
        }

        private static async Task<BaseCodeGenDescriptor> GenerateCode(string metadataUri, Version edmxVersion, ConnectedServiceHandlerContext context, Project project)
        {
            BaseCodeGenDescriptor codeGenDescriptor;

            if (edmxVersion == Common.Constants.EdmxVersion1
                || edmxVersion == Common.Constants.EdmxVersion2
                || edmxVersion == Common.Constants.EdmxVersion3)
            {
                codeGenDescriptor = new V3CodeGenDescriptor(metadataUri, context, project);
            }
            else if (edmxVersion == Common.Constants.EdmxVersion4)
            {
                codeGenDescriptor = new V4CodeGenDescriptor(metadataUri, context, project, new ODataT4CodeGeneratorFactory());
            }
            else
            {
                throw new Exception(string.Format(CultureInfo.InvariantCulture, "Not supported Edmx Version {0}", edmxVersion.ToString()));
            }

            await codeGenDescriptor.AddNugetPackages();
            await codeGenDescriptor.AddGeneratedClientCode();
            return codeGenDescriptor;
        }
    }
}
