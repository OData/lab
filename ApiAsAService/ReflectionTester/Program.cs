using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReflectionTester
{
    using System.Data.Entity;
    using System.IO;
    using System.Reflection;
    using System.Reflection.Emit;
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Xml;
    using Microsoft.OData.Edm;
    using Microsoft.OData.Edm.Csdl;
    using Microsoft.OData.Edm.Validation;

    class Program
    {
        static Dictionary<string, TypeBuilderInfo> _typeBuildersDict = new Dictionary<string, TypeBuilderInfo>();
        static Regex collectionRegex = new Regex(@"Collection\((.+)\)", RegexOptions.Compiled);

        public class TypeBuilderInfo
        {
            public bool IsDerived { get; set; }
            public TypeBuilder Builder { get; set; }
        }

        public static IEdmModel ReadModel(string fileName)
        {
            var edmxReaderSettings = new global::Microsoft.OData.Edm.Csdl.CsdlReaderSettings()
            {
                IgnoreUnexpectedAttributesAndElements = true
            };
            var references = global::System.Linq.Enumerable.Empty<global::Microsoft.OData.Edm.IEdmModel>();

            using (var reader = XmlReader.Create(fileName))
            {
                IEdmModel model;
                IEnumerable<EdmError> errors;
                if (CsdlReader.TryParse(reader, references, edmxReaderSettings, out model, out errors))
                {
                    return model;
                }
                return null;
            }
        }

        public static void BuildModules(IEdmModel model, ModuleBuilder moduleBuilder)
        {

            IList<string> baseProps = new List<string>();
            foreach (var modelSchemaElement in model.SchemaElements)
            {
                var one = model.FindDeclaredType(modelSchemaElement.FullName());

                if (one is IEdmStructuredType)
                {
                    if (one is IEdmEntityType)
                    {

                        var two = model.FindDirectlyDerivedTypes((IEdmStructuredType)one);
                        if (two.Any())
                        {
                            baseProps.Add(one.FullName());
                            Compile((IEdmStructuredType)one, moduleBuilder, one.FullName());
                        }
                    }
                    else
                    {
                        baseProps.Add(one.FullName());
                        Compile((IEdmStructuredType)one, moduleBuilder, one.FullName());

                    }

                }
            }

            foreach (var modelSchemaElement in model.SchemaElements)
            {
                if (!baseProps.Contains(modelSchemaElement.FullName()))
                {
                    var one = model.FindDeclaredType(modelSchemaElement.FullName());
                    if (one != null)
                    {
                        Compile((IEdmStructuredType)one, moduleBuilder, one.FullName());
                    }
                }
            }

            foreach (var modelSchemaElement in model.SchemaElements)
            {
                var one = model.FindDeclaredType(modelSchemaElement.FullName());
                if (one != null)
                {
                    Compile((IEdmStructuredType)one, moduleBuilder, one.FullName(), true);
                }

            }

            foreach (var typeBuilder in _typeBuildersDict)
            {
                if (typeBuilder.Value.IsDerived)
                {

                    var previouslyBuiltType = _typeBuildersDict[(typeBuilder.Value.Builder.BaseType.FullName)];
                    typeBuilder.Value.Builder.CreatePassThroughConstructors(previouslyBuiltType.Builder);
                }
                typeBuilder.Value.Builder.CreateType();
            }

            //generate the entities type
            var entitiesBuilder = moduleBuilder.DefineType("Entities", TypeAttributes.Class | TypeAttributes.Public, typeof(DbContext));
            var dbContextType = typeof(DbContext);
            entitiesBuilder.CreatePassThroughConstructors(dbContextType);


            foreach (var typeBuilderInfo in _typeBuildersDict)
            {
                Type listOf = typeof(DbSet<>);
                Type selfContained = listOf.MakeGenericType(typeBuilderInfo.Value.Builder);
                PropertyBuilderHelper.BuildProperty(entitiesBuilder, typeBuilderInfo.Key.Split('.')[1], selfContained);
            }
            entitiesBuilder.CreateType();


        }



        internal static void Compile(IEdmStructuredType type, ModuleBuilder moduleBuilder, string moduleName, bool navPass = false)
        {
            TypeBuilder typeBuilder = null;
            if (type.BaseType != null && !navPass)
            {
                var previouslyBuiltType = _typeBuildersDict[(type.BaseType.FullTypeName())];

                typeBuilder = moduleBuilder.DefineType(moduleName, TypeAttributes.Class | TypeAttributes.Public, previouslyBuiltType.Builder);
                _typeBuildersDict.Add(moduleName, new TypeBuilderInfo() { Builder = typeBuilder, IsDerived = true });


            }

            if (!navPass)
            {
                if (typeBuilder == null)
                {
                    typeBuilder = moduleBuilder.DefineType(moduleName, TypeAttributes.Class | TypeAttributes.Public);
                    _typeBuildersDict.Add(moduleName, new TypeBuilderInfo() { Builder = typeBuilder, IsDerived = false });
                }

                foreach (var property in type.DeclaredProperties)
                {
                    if (property.PropertyKind != EdmPropertyKind.Navigation)
                    {
                        GenerateProperty(property, typeBuilder, moduleBuilder);

                    }
                }

            }
            else
            {
                typeBuilder = _typeBuildersDict[moduleName].Builder;
                foreach (var property in type.DeclaredProperties)
                {
                    if (property.PropertyKind == EdmPropertyKind.Navigation)
                        GenerateProperty(property, typeBuilder, moduleBuilder);
                }
            }







        }

        internal static void GenerateProperty(IEdmProperty property, TypeBuilder typeBuilder, ModuleBuilder moduleBuilder)
        {
            var propertyName = property.Name;

            var emdPropType = property.Type.PrimitiveKind();
            var propertyType = GetPrimitiveClrType(emdPropType, false);
            if (propertyType == null)
            {
                if (property.Type.FullName().ToLower().Contains("geography"))
                {
                    return;
                }

                if (property.PropertyKind == EdmPropertyKind.Navigation)
                {
                    if (property.Type.FullName().StartsWith("Collection"))
                    {
                        var typeName = collectionRegex.Match(property.Type.FullName()).Groups[1].Value;
                        Type listOf = typeof(List<>);
                        Type selfContained = listOf.MakeGenericType(_typeBuildersDict[typeName].Builder);
                        propertyType = selfContained;
                    }
                    else
                    {
                        var navProptype = _typeBuildersDict[property.Type.FullName()];
                        propertyType = navProptype.Builder;
                    }


                }
                else
                {
                    var previouslyBuiltType = moduleBuilder.GetType(property.Type.FullName());
                    propertyType = previouslyBuiltType;
                }


            }
            PropertyBuilderHelper.BuildProperty(typeBuilder, propertyName, propertyType);
            //var field = typeBuilder.DefineField(propertyName, propertyType, FieldAttributes.Private);
            //var propertyBuilder = typeBuilder.DefineProperty(propertyName, PropertyAttributes.HasDefault, propertyType, null);

            //// Generate getter method

            //var getter = typeBuilder.DefineMethod("Get", MethodAttributes.Public, propertyType, Type.EmptyTypes);

            //var il = getter.GetILGenerator();

            //il.Emit(OpCodes.Ldarg_0);        // Push &quot;this&quot; on the stack
            //il.Emit(OpCodes.Ldfld, field);   // Load the field &quot;_Name&quot;
            //il.Emit(OpCodes.Ret);            // Return

            //propertyBuilder.SetGetMethod(getter);

            //// Generate setter method

            //var setter = typeBuilder.DefineMethod("Set", MethodAttributes.Public, null, new[] { propertyType });

            //il = setter.GetILGenerator();

            //il.Emit(OpCodes.Ldarg_0);        // Push &quot;this&quot; on the stack
            //il.Emit(OpCodes.Ldarg_1);        // Push &quot;value&quot; on the stack
            //il.Emit(OpCodes.Stfld, field);   // Set the field &quot;_Name&quot; to &quot;value&quot;
            //il.Emit(OpCodes.Ret);            // Return

            //propertyBuilder.SetSetMethod(setter);
        }

        /// <summary>
        /// Get Clr type
        /// </summary>
        /// <param name="typeKind">Edm Primitive Type Kind</param>
        /// <param name="isNullable">Nullable value</param>
        /// <returns>CLR type</returns>
        private static Type GetPrimitiveClrType(EdmPrimitiveTypeKind typeKind, bool isNullable)
        {
            switch (typeKind)
            {
                case EdmPrimitiveTypeKind.Binary:
                    return typeof(byte[]);
                case EdmPrimitiveTypeKind.Boolean:
                    return isNullable ? typeof(Boolean?) : typeof(Boolean);
                case EdmPrimitiveTypeKind.Byte:
                    return isNullable ? typeof(Byte?) : typeof(Byte);
                case EdmPrimitiveTypeKind.Date:
                    return isNullable ? typeof(Date?) : typeof(Date);
                case EdmPrimitiveTypeKind.DateTimeOffset:
                    return isNullable ? typeof(DateTimeOffset?) : typeof(DateTimeOffset);
                case EdmPrimitiveTypeKind.Decimal:
                    return isNullable ? typeof(Decimal?) : typeof(Decimal);
                case EdmPrimitiveTypeKind.Double:
                    return isNullable ? typeof(Double?) : typeof(Double);
                case EdmPrimitiveTypeKind.Guid:
                    return isNullable ? typeof(Guid?) : typeof(Guid);
                case EdmPrimitiveTypeKind.Int16:
                    return isNullable ? typeof(Int16?) : typeof(Int16);
                case EdmPrimitiveTypeKind.Int32:
                    return isNullable ? typeof(Int32?) : typeof(Int32);
                case EdmPrimitiveTypeKind.Int64:
                    return isNullable ? typeof(Int64?) : typeof(Int64);
                case EdmPrimitiveTypeKind.SByte:
                    return isNullable ? typeof(SByte?) : typeof(SByte);
                case EdmPrimitiveTypeKind.Single:
                    return isNullable ? typeof(Single?) : typeof(Single);
                case EdmPrimitiveTypeKind.Stream:
                    return typeof(Stream);
                case EdmPrimitiveTypeKind.String:
                    return typeof(String);
                case EdmPrimitiveTypeKind.Duration:
                    return isNullable ? typeof(TimeSpan?) : typeof(TimeSpan);
                case EdmPrimitiveTypeKind.TimeOfDay:
                    return isNullable ? typeof(TimeOfDay?) : typeof(TimeOfDay);
                default:
                    return null;
            }
        }

        static void Main(string[] args)
        {
            var model = ReadModel(@"C:\repos\fun\lab\ApiAsAService\EdmHelperTest\NW_Simple.xml");


            // create a dynamic assembly and module 
            AssemblyName assemblyName = new AssemblyName();
            assemblyName.Name = "HelloWorld";
            AssemblyBuilder assemblyBuilder = Thread.GetDomain().DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.RunAndSave);
            ModuleBuilder module;
            module = assemblyBuilder.DefineDynamicModule("HelloWorld.exe");
            BuildModules(model, module);

            assemblyBuilder.Save("HelloWorld.exe");
        }


    }
}
