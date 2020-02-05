﻿using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.VisualStudio.ConnectedServices;
using EnvDTE;
using Moq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.OData.ConnectedService;
using Microsoft.OData.ConnectedService.CodeGeneration;
using Microsoft.OData.ConnectedService.Models;
using Microsoft.OData.ConnectedService.Templates;
using ODataConnectedService.Tests.TestHelpers;

namespace ODataConnectedService.Tests.CodeGeneration
{
    [TestClass]
    public class V4CodeGenDescriptorTest
    {
        readonly static string TestProjectRootPath = Path.Combine(Directory.GetCurrentDirectory(), "TempODataConnectedServiceTest");
        readonly static string ServicesRootFolder = "ConnectedServicesRoot";
        readonly static string MetadataUri = "http://service/$metadata";

        [TestCleanup]
        public void CleanUp()
        {
            try
            {
                Directory.Delete(TestProjectRootPath, true);
            }
            catch (DirectoryNotFoundException) { }
        }

        [DataTestMethod]
        [DataRow(true, true, true, "Prefix", true)]
        [DataRow(false, false, false, null, false)]
        public void TestAddGeneratedClientCode_PassesServiceConfigOptionsToCodeGenerator(
            bool useDSC, bool ignoreUnexpected, bool enableNamingAlias,
            string namespacePrefix, bool makeTypesInternal)
        {
            var handlerHelper = new TestConnectedServiceHandlerHelper();
            var codeGenFactory = new TestODataT4CodeGeneratorFactory();

            var serviceConfig = new ServiceConfigurationV4()
            {
                UseDataServiceCollection = useDSC,
                IgnoreUnexpectedElementsAndAttributes = ignoreUnexpected,
                EnableNamingAlias = enableNamingAlias,
                NamespacePrefix = namespacePrefix,
                MakeTypesInternal = makeTypesInternal,
                IncludeT4File = false
            };
            var codeGenDescriptor = SetupCodeGenDescriptor(serviceConfig, "TestService", codeGenFactory, handlerHelper);
            codeGenDescriptor.AddGeneratedClientCode().Wait();

            var generator = codeGenFactory.LastCreatedInstance;
            Assert.AreEqual(useDSC, generator.UseDataServiceCollection);
            Assert.AreEqual(enableNamingAlias, generator.EnableNamingAlias);
            Assert.AreEqual(ignoreUnexpected, generator.IgnoreUnexpectedElementsAndAttributes);
            Assert.AreEqual(makeTypesInternal, generator.MakeTypesInternal);
            Assert.AreEqual(namespacePrefix, generator.NamespacePrefix);
            Assert.AreEqual(MetadataUri, generator.MetadataDocumentUri);
            Assert.AreEqual(ODataT4CodeGenerator.LanguageOption.CSharp, generator.TargetLanguage);
        }

        [TestMethod]
        public void TestAddGeneratedClientCode_GeneratesAndSavesCodeFile()
        {
            var serviceName = "MyService";
            ServiceConfiguration serviceConfig = new ServiceConfigurationV4()
            {
                MakeTypesInternal = true,
                UseDataServiceCollection = false,
                ServiceName = serviceName,
                GeneratedFileNamePrefix = "MyFile",
                IncludeT4File = false
            };
            var handlerHelper = new TestConnectedServiceHandlerHelper();
            var codeGenDescriptor = SetupCodeGenDescriptor(serviceConfig, serviceName,
                new TestODataT4CodeGeneratorFactory(), handlerHelper);
            codeGenDescriptor.AddGeneratedClientCode().Wait();
            using (var reader = new StreamReader(handlerHelper.AddedFileInputFileName))
            {
                var generatedCode = reader.ReadToEnd();
                Assert.AreEqual("Generated code", generatedCode);
                Assert.AreEqual(Path.Combine(TestProjectRootPath, ServicesRootFolder, serviceName, "MyFile.cs"),
                    handlerHelper.AddedFileTargetFilePath);
            }
        }

        static V4CodeGenDescriptor SetupCodeGenDescriptor(ServiceConfiguration serviceConfig, string serviceName, IODataT4CodeGeneratorFactory codeGenFactory, TestConnectedServiceHandlerHelper handlerHelper)
        {
            var referenceFolderPath = Path.Combine(TestProjectRootPath, ServicesRootFolder, serviceName);
            Directory.CreateDirectory(referenceFolderPath);
            Project project = CreateTestProject(TestProjectRootPath);
            var serviceInstance = new ODataConnectedServiceInstance()
            {
                ServiceConfig = serviceConfig,
                Name = serviceName
            };
            handlerHelper.ServicesRootFolder = ServicesRootFolder;
            ConnectedServiceHandlerContext context = new TestConnectedServiceHandlerContext(serviceInstance, handlerHelper);

            return new TestV4CodeGenDescriptor(MetadataUri, context, project, codeGenFactory);
        }

        static Project CreateTestProject(string projectPath)
        {
            var fullPathPropertyMock = new Mock<Property>();
            fullPathPropertyMock.SetupGet(p => p.Value).Returns(projectPath);
            var projectPropertiesMock = new Mock<Properties>();
            projectPropertiesMock.Setup(p => p.Item(It.Is<string>(s => s == "FullPath")))
                .Returns(fullPathPropertyMock.Object);
            var projectMock = new Mock<Project>();
            projectMock.SetupGet(p => p.Properties)
                .Returns(projectPropertiesMock.Object);
            return projectMock.Object;
        }
    }

    class TestV4CodeGenDescriptor: V4CodeGenDescriptor
    {
        public TestV4CodeGenDescriptor(string metadataUri, ConnectedServiceHandlerContext context, Project project, IODataT4CodeGeneratorFactory codeGenFactory)
            : base(metadataUri, context, project, codeGenFactory)
        {
        }
        protected override void Init() { }
    }

    class TestODataT4CodeGenerator: ODataT4CodeGenerator
    {
        public override string TransformText()
        {
            return "Generated code";
        }
    }

    class TestODataT4CodeGeneratorFactory : IODataT4CodeGeneratorFactory
    {
        public ODataT4CodeGenerator LastCreatedInstance { get; private set; }
        public ODataT4CodeGenerator Create()
        {
            var generator = new TestODataT4CodeGenerator();
            LastCreatedInstance = generator;
            return generator;
        }
    }
}
