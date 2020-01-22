using System;
using EnvDTE;
using Microsoft.VisualStudio.ConnectedServices;

namespace Microsoft.OData.ConnectedService.CodeGeneration
{
    interface ICodeGenDescriptorFactory
    {
        BaseCodeGenDescriptor Create(Version edmxVersion, string metadataUri, ConnectedServiceHandlerContext context, Project project);
    }
}
