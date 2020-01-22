// Copyright (c) Microsoft Corporation.  All rights reserved.
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
        private ICodeGenDescriptorFactory codeGenDescriptorFactory;
        public ODataConnectedServiceHandler(): this(new CodeGenDescriptorFactory())
        {
        }

        public ODataConnectedServiceHandler(ICodeGenDescriptorFactory codeGenDescriptorFactory)
            : base()
        {
            this.codeGenDescriptorFactory = codeGenDescriptorFactory;
        }


        public override async Task<AddServiceInstanceResult> AddServiceInstanceAsync(ConnectedServiceHandlerContext context, CancellationToken ct)
        {
            Project project = ProjectHelper.GetProjectFromHierarchy(context.ProjectHierarchy);
            ODataConnectedServiceInstance codeGenInstance = (ODataConnectedServiceInstance)context.ServiceInstance;

            var codeGenDescriptor = await GenerateCode(codeGenDescriptorFactory, codeGenInstance.MetadataTempFilePath, codeGenInstance.ServiceConfig.EdmxVersion, context, project);

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

            var codeGenDescriptor = await GenerateCode(codeGenDescriptorFactory, codeGenInstance.ServiceConfig.Endpoint, codeGenInstance.ServiceConfig.EdmxVersion, context, project);
            context.SetExtendedDesignerData<ServiceConfiguration>(codeGenInstance.ServiceConfig);
            return new UpdateServiceInstanceResult();
        }

        private static async Task<BaseCodeGenDescriptor> GenerateCode(ICodeGenDescriptorFactory codeGenDescriptorFactory, string metadataUri, Version edmxVersion, ConnectedServiceHandlerContext context, Project project)
        {
            BaseCodeGenDescriptor codeGenDescriptor = codeGenDescriptorFactory.Create(edmxVersion, metadataUri, context, project);
            await codeGenDescriptor.AddNugetPackages();
            await codeGenDescriptor.AddGeneratedClientCode();
            return codeGenDescriptor;
        }
    }
}
