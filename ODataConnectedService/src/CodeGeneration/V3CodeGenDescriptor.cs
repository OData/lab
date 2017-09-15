// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Data.Services.Design;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using EnvDTE;
using Microsoft.OData.ConnectedService.Common;
using Microsoft.VisualStudio.ConnectedServices;

namespace Microsoft.OData.ConnectedService.CodeGeneration
{
    internal class V3CodeGenDescriptor : BaseCodeGenDescriptor
    {
        const string loadServiceModelAssignment = "this.Format.LoadServiceModel = GeneratedEdmModel.GetInstance;";
        const string onContextCreatedCall = "this.OnContextCreated();";
        const string dscPattern = @"private (.+) _(.+) = new global::System.Data.Services.Client.DataServiceCollection\<(.+)\>\(null, global::System.Data.Services.Client.TrackingMode.None\);";
        const string collectionPattern = @"private (.+) _(.+) = new global::System.Collections.ObjectModel.Collection\<(.+)\>\(\);";

        const string dynamicModelLoader =
        @"/// <summary>
        /// Runtime IEdmModel loader class. Responsible for loading, parsing and caching $metadata documents.
        /// Used in place of the static GeneratedEdmModel from this.Format.LoadServiceModel().
        /// </summary>
        private static class RuntimeEdmModel
        {
            [global::System.CodeDom.Compiler.GeneratedCodeAttribute(""System.Data.Services.Design"", ""1.0.0"")]
            private static global::System.Collections.Generic.Dictionary<string, global::Microsoft.Data.Edm.IEdmModel> models = 
                new global::System.Collections.Generic.Dictionary<string, global::Microsoft.Data.Edm.IEdmModel>(global::System.StringComparer.OrdinalIgnoreCase);

            [global::System.CodeDom.Compiler.GeneratedCodeAttribute(""System.Data.Services.Design"", ""1.0.0"")]
            private static readonly object modelsCacheLock = new object();

            [global::System.CodeDom.Compiler.GeneratedCodeAttribute(""System.Data.Services.Design"", ""1.0.0"")]
            public static global::Microsoft.Data.Edm.IEdmModel LoadModel(global::System.Data.Services.Client.DataServiceContext context)
            {
                var metadataUri = context.GetMetadataUri();
                global::Microsoft.Data.Edm.IEdmModel model = null;
                if (!models.TryGetValue(metadataUri.AbsoluteUri, out model))
                {
                    lock (modelsCacheLock)
                    {
                        if (!models.TryGetValue(metadataUri.AbsoluteUri, out model))
                        {
                            var request = (global::System.Net.HttpWebRequest)global::System.Net.WebRequest.Create(metadataUri);
                            request.Credentials = context.Credentials;

                            using (var response = request.EndGetResponse(request.BeginGetResponse(null, null)))
                            using (var stream = response.GetResponseStream())
                            using (var reader = global::System.Xml.XmlReader.Create(stream))
                            {
                                model = global::Microsoft.Data.Edm.Csdl.EdmxReader.Parse(reader);
                                models.Add(metadataUri.AbsoluteUri, model);
                            }
                        }
                    }
                }
                return model;
            }
        }";

        const string resolveBackingFieldAssignment = "this.ResolveBackingField = ResolveCodeGenBackingField;\r\n\t\t\t";
        const string resolveBackingFieldImplementation = @"
        private static global::System.Reflection.FieldInfo FindBackingField(global::System.Reflection.PropertyInfo property, string prefix)
        {
            global::System.Reflection.FieldInfo backingField = null;
#if NO_NETFX
            // global::System.Reflection.BindingFlags is only available on full .Net Framework. 
            // Developers can add NO_NETFX to Conditional compilation symbols and still use lazy collection initialization.
            backingField = global::System.Linq.Queryable.FirstOrDefault(
                global::System.Linq.Queryable.Where(
                    global::System.Linq.Queryable.AsQueryable(global::System.Reflection.RuntimeReflectionExtensions.GetRuntimeFields(property.DeclaringType)),
                    p => p.Name == prefix + property.Name && !p.IsStatic && !p.IsPublic && p.FieldType == property.PropertyType));
#else
            backingField = property.DeclaringType.GetField(prefix + property.Name, global::System.Reflection.BindingFlags.NonPublic | global::System.Reflection.BindingFlags.Instance);
            if (backingField != null && backingField.FieldType != property.PropertyType)
            {
                backingField = null;
            }
#endif
            return backingField;
        }
        /// <summary>
        /// Tries to find the backing field for property.
        /// This is needed when using lazy initialized collection properties to avoid WCF DataServiesClient triggering construction.
        /// The backing field name is matched by name using the naming convention by the code generation process.
        /// </summary>
        /// <param name=""property"">The property to find the backing field for.</param>
        /// <returns>The property backing FieldInfo or null.</returns>
        private static global::System.Reflection.FieldInfo ResolveCodeGenBackingField(global::System.Reflection.PropertyInfo property)
        {
            if (property == null)
                return null;

            global::System.Reflection.FieldInfo backingField = FindBackingField(property, ""__"");
            if (backingField == null)
                backingField = FindBackingField(property, ""_"");

            return backingField;
        }
" + "\t\t";

        const string collectionReplacer =
        @"private $1 __$2 = null;

        [global::System.CodeDom.Compiler.GeneratedCodeAttribute(""System.Data.Services.Design"", ""1.0.0"")]
        private $1 _$2
        {
            get
            {
                if (__$2 == null && !__$2Initialized)
                    $2 = new $1(null, global::System.Data.Services.Client.TrackingMode.None);
                return __$2;
            }
            set
            {
                __$2 = value;
                __$2Initialized = true;
            }
        }

        [global::System.CodeDom.Compiler.GeneratedCodeAttribute(""System.Data.Services.Design"", ""1.0.0"")]
        bool __$2Initialized;
";

        public V3CodeGenDescriptor(string metadataUri, ConnectedServiceHandlerContext context, Project project)
            : base(metadataUri, context, project)
        {
            this.ClientNuGetPackageName = Common.Constants.V3ClientNuGetPackage;
            this.ClientDocUri = Common.Constants.V3DocUri;
        }

        public async override Task AddNugetPackages()
        {
            await this.Context.Logger.WriteMessageAsync(LoggerMessageCategory.Information, "Adding Nuget Packages");

            var wcfDSInstallLocation = CodeGeneratorUtils.GetWCFDSInstallLocation();
            var packageSource = Path.Combine(wcfDSInstallLocation, @"bin\NuGet");
            if (Directory.Exists(packageSource))
            {
                var files = Directory.EnumerateFiles(packageSource, "*.nupkg").ToList();
                foreach (var nugetPackage in Common.Constants.V3NuGetPackages)
                {
                    if (!files.Any(f => Regex.IsMatch(f, nugetPackage + @"(.\d){2,4}.nupkg")))
                    {
                        packageSource = Common.Constants.NuGetOnlineRepository;
                    }
                }
            }
            else
            {
                packageSource = Common.Constants.NuGetOnlineRepository;
            }

            if (!PackageInstallerServices.IsPackageInstalled(this.Project, this.ClientNuGetPackageName))
            {
                Version packageVersion = null;
                PackageInstaller.InstallPackage(Common.Constants.NuGetOnlineRepository, this.Project, this.ClientNuGetPackageName, packageVersion, false);
            }
        }

        public async override Task AddGeneratedClientCode()
        {
            await this.Context.Logger.WriteMessageAsync(LoggerMessageCategory.Information, "Generating Client Proxy ...");

            EntityClassGenerator generator = new EntityClassGenerator(LanguageOption.GenerateCSharpCode);
            generator.UseDataServiceCollection = this.ServiceConfiguration.UseDataServiceCollection;
            generator.Version = DataServiceCodeVersion.V3;

            XmlReaderSettings settings = new XmlReaderSettings()
            {
                XmlResolver = new XmlUrlResolver()
                {
                    Credentials = System.Net.CredentialCache.DefaultNetworkCredentials
                }
            };

            using (XmlReader reader = XmlReader.Create(this.MetadataUri, settings))
            {
                string tempFile = Path.GetTempFileName();

                using (StreamWriter writer = File.CreateText(tempFile))
                {
                    var errors = generator.GenerateCode(reader, writer, this.ServiceConfiguration.NamespacePrefix);
                    await writer.FlushAsync();
                    if (errors != null && errors.Count() > 0)
                    {
                        foreach (var err in errors)
                        {
                            await this.Context.Logger.WriteMessageAsync(LoggerMessageCategory.Warning, err.Message);
                        }
                    }
                }

                if (this.ServiceConfiguration.LazyInitializedEntityCollections || this.ServiceConfiguration.UseRuntimeModel)
                {
                    await this.Context.Logger.WriteMessageAsync(LoggerMessageCategory.Information, "Cleaning proxy...");
                    string original = null;
                    using (var textReader = File.OpenText(tempFile))
                    {
                        original = await textReader.ReadToEndAsync();
                    }
                    string modified = null;

                    if (this.ServiceConfiguration.LazyInitializedEntityCollections)
                    {
                        string pattern = null;
                        if (generator.UseDataServiceCollection)
                        {
                            await this.Context.Logger.WriteMessageAsync(LoggerMessageCategory.Information, "Proxy - injecting lazy initialization of DataServiceCollections");
                            pattern = dscPattern;
                        }
                        else
                        {
                            await this.Context.Logger.WriteMessageAsync(LoggerMessageCategory.Information, "Proxy - injecting lazy initialization of ObservableCollections");
                            pattern = collectionPattern;
                        }
                        modified = Regex.Replace(original, pattern, collectionReplacer);
                        int classStart = modified.IndexOf("private abstract class GeneratedEdmModel");
                        if (classStart >= 0)
                        {
                            modified = string.Concat(
                                modified.Substring(0, classStart),
                                resolveBackingFieldImplementation,
                                modified.Substring(classStart));
                            int onContextCreatedCallIndex = modified.IndexOf(onContextCreatedCall);
                            if (onContextCreatedCallIndex >= 0)
                            {
                                modified = string.Concat(
                                    modified.Substring(0, onContextCreatedCallIndex),
                                    resolveBackingFieldAssignment,
                                    modified.Substring(onContextCreatedCallIndex));
                            }
                        }
                    }
                    else
                    {
                        modified = original;
                    }

                    if (this.ServiceConfiguration.UseRuntimeModel)
                    {
                        await this.Context.Logger.WriteMessageAsync(LoggerMessageCategory.Information, "Proxy - removing static model");
                        int loadServiceModelAssignmentIndex = modified.IndexOf(loadServiceModelAssignment);
                        if (loadServiceModelAssignmentIndex >= 0)
                        {
                            modified = string.Concat(
                                modified.Substring(0, loadServiceModelAssignmentIndex), "this.Format.LoadServiceModel = () => RuntimeEdmModel.LoadModel(this);", 
                                modified.Substring(loadServiceModelAssignmentIndex + loadServiceModelAssignment.Length));
                            int classStart = modified.IndexOf("private abstract class GeneratedEdmModel");
                            int classEnd = modified.IndexOf("return global::System.Xml.XmlReader.Create(new global::System.IO.StringReader(edmxToParse));");
                            if (classStart >= 0 && classEnd > 0)
                            {
                                classEnd = modified.IndexOf('}', classEnd + 1);
                                classEnd = modified.IndexOf('}', classEnd + 1) + 1;
                                modified = string.Concat(modified.Substring(0, classStart), dynamicModelLoader, modified.Substring(classEnd));
                            }
                        }
                    }
                    await this.Context.Logger.WriteMessageAsync(LoggerMessageCategory.Information, "Proxy generation done. Writing output files...");
                    using (StreamWriter writer = File.CreateText(tempFile))
                    {
                        await writer.WriteAsync(modified);
                        await writer.FlushAsync();
                    }
                }

                string outputFile = Path.Combine(GetReferenceFileFolder(), this.GeneratedFileNamePrefix + ".cs");
                await this.Context.HandlerHelper.AddFileAsync(tempFile, outputFile);
            }
        }
    }
}
