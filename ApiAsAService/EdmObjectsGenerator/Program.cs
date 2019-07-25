namespace EdmObjectsGenerator
{
    using System;
    using System.Collections.Generic;
    using System.Data.Entity;
    using System.Data.Entity.Infrastructure;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Reflection.Emit;
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Xml;
    using Microsoft.OData.Edm;
    using Microsoft.OData.Edm.Csdl;
    using Microsoft.OData.Edm.Validation;

    public class Program
    {
        static Dictionary<string, TypeBuilderInfo> _typeBuildersDict = new Dictionary<string, TypeBuilderInfo>();
        static Regex collectionRegex = new Regex(@"Collection\((.+)\)", RegexOptions.Compiled);
        static Queue<TypeBuilderInfo> _builderQueue = new Queue<TypeBuilderInfo>();
        public class TypeBuilderInfo
        {
            public bool IsDerived { get; set; }
            public bool IsStructured { get; set; }
            public TypeInfo Builder { get; set; }
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
            //first create the basic types for the enums
            foreach (var modelSchemaElement in model.SchemaElements)
            {
                var declaredType = model.FindDeclaredType(modelSchemaElement.FullName());
                if (declaredType == null) continue;
                if (declaredType is IEdmEnumType)
                {
                    CreateType((IEdmEnumType)declaredType, moduleBuilder, declaredType.FullName());
                }
                
            }
            //next create the basic types for the types
            foreach (var modelSchemaElement in model.SchemaElements)
            {

                var declaredType = model.FindDeclaredType(modelSchemaElement.FullName());
                if (declaredType == null) continue;
                if (!(declaredType is IEdmEnumType))
                {
                    CreateType((IEdmStructuredType)declaredType, moduleBuilder, declaredType.FullName());
                }
                else
                {
                    Compile((IEdmEnumType)declaredType, moduleBuilder, declaredType.FullName());

                }
            }

            //go through and add all elements and their properties but not nav properties
            foreach (var modelSchemaElement in model.SchemaElements)
            {

                var one = model.FindDeclaredType(modelSchemaElement.FullName());
                if (one != null && !(modelSchemaElement is IEdmEnumType))
                {
                    Compile((IEdmStructuredType)one, moduleBuilder, one.FullName());
                }

            }

            //finally add the nav properties
            foreach (var modelSchemaElement in model.SchemaElements)
            {
                if ((modelSchemaElement is IEdmEnumType))
                {
                    continue;
                }
                var one = model.FindDeclaredType(modelSchemaElement.FullName());
                if (one != null)
                {
                    Compile((IEdmStructuredType)one, moduleBuilder, one.FullName(), true);
                }

            }
            //now go through the queue and create the types in dependency order
            while (_builderQueue.Count != 0)
            {
                var typeBuilder = _builderQueue.Dequeue();
                if (typeBuilder.Builder is TypeBuilder)
                {
                    ((TypeBuilder)typeBuilder.Builder).CreateType();

                }
                if (typeBuilder.Builder is EnumBuilder)
                {
                    ((EnumBuilder)typeBuilder.Builder).CreateType();

                }
            }
            

            //generate the entities type
            var entitiesBuilder = moduleBuilder.DefineType("Entities", TypeAttributes.Class | TypeAttributes.Public, typeof(DbContext));
            var dbContextType = typeof(DbContext);
            entitiesBuilder.CreatePassThroughConstructors(dbContextType);


            foreach (var typeBuilderInfo in _typeBuildersDict)
            {
                if (!typeBuilderInfo.Value.IsStructured)
                {
                    Type listOf = typeof(DbSet<>);
                    Type selfContained = listOf.MakeGenericType(typeBuilderInfo.Value.Builder);
                    PropertyBuilderHelper.BuildProperty(entitiesBuilder, typeBuilderInfo.Value.Builder.Name, selfContained);
                }
            }

           
            // create the Main(string[] args) method
            MethodBuilder methodbuilder = entitiesBuilder.DefineMethod("OnModelCreating", MethodAttributes.Public
                                                                                          | MethodAttributes.HideBySig
                                                                                          | MethodAttributes.CheckAccessOnOverride
                                                                                          | MethodAttributes.Virtual,
                                                                                          typeof(void), new Type[] { typeof(DbModelBuilder) });

            // generate the IL for the Main method
            ILGenerator ilGenerator = methodbuilder.GetILGenerator();
            ilGenerator.ThrowException(typeof(UnintentionalCodeFirstException));
            ilGenerator.Emit(OpCodes.Ret);
            entitiesBuilder.CreateType();


        }

        internal static TypeBuilder CreateType(IEdmStructuredType targetType, ModuleBuilder moduleBuilder, string moduleName)
        {
            if (_typeBuildersDict.ContainsKey(moduleName))
            {
                return (TypeBuilder)_typeBuildersDict[moduleName].Builder;
            }
            if (targetType.BaseType != null)
            {
                TypeBuilder previouslyBuiltType = null;
                if (!_typeBuildersDict.ContainsKey(moduleName))
                {
                    previouslyBuiltType = CreateType(targetType.BaseType, moduleBuilder, targetType.BaseType.FullTypeName());

                }

                var typeBuilder = moduleBuilder.DefineType(moduleName, TypeAttributes.Class | TypeAttributes.Public, previouslyBuiltType);
                var typeBuilderInfo = new TypeBuilderInfo() {Builder = typeBuilder, IsDerived = true};
                _typeBuildersDict.Add(moduleName, typeBuilderInfo);
                _builderQueue.Enqueue(typeBuilderInfo);
                return typeBuilder;

            }
            else
            {
                var typeBuilder = moduleBuilder.DefineType(moduleName, TypeAttributes.Class | TypeAttributes.Public);
                var builderInfo = new TypeBuilderInfo() {Builder = typeBuilder, IsDerived = false};
                _typeBuildersDict.Add(moduleName, builderInfo);
                _builderQueue.Enqueue(builderInfo);

                return typeBuilder;
            }

        }



        internal static void Compile(IEdmStructuredType type, ModuleBuilder moduleBuilder, string moduleName, bool navPass = false)
        {
            TypeBuilder typeBuilder = null;
            if (type.BaseType != null && !navPass)
            {
                typeBuilder = CreateType(type, moduleBuilder, moduleName);

            }

            if (!navPass)
            {
                if (typeBuilder == null)
                {
                    typeBuilder = CreateType(type, moduleBuilder, moduleName);
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
                typeBuilder = (TypeBuilder)_typeBuildersDict[moduleName].Builder;
                foreach (var property in type.DeclaredProperties)
                {
                    if (property.PropertyKind == EdmPropertyKind.Navigation)
                        GenerateProperty(property, typeBuilder, moduleBuilder);
                }
            }

        }

        internal static EnumBuilder CreateType(IEdmEnumType targetType, ModuleBuilder moduleBuilder, string moduleName)
        {
            if (_typeBuildersDict.ContainsKey(moduleName))
            {
                return (EnumBuilder)_typeBuildersDict[moduleName].Builder;
            }

            EnumBuilder typeBuilder = moduleBuilder.DefineEnum(moduleName, TypeAttributes.Public, typeof(int));
            var builderInfo = new TypeBuilderInfo() {Builder = typeBuilder, IsDerived = false};
            _typeBuildersDict.Add(moduleName, builderInfo);
            _builderQueue.Enqueue(builderInfo);
            return typeBuilder;


        }

        internal static void Compile(IEdmEnumType type, ModuleBuilder moduleBuilder, string moduleName)
        {
            var typeBuilder = CreateType(type, moduleBuilder, moduleName);
            foreach (var enumMember in type.Members)
            {
                GenerateEnum(enumMember, typeBuilder, moduleBuilder);

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
                        var baseType = _typeBuildersDict.ContainsKey(typeName) ? _typeBuildersDict[typeName].Builder : typeof(string);

                        var selfContained = listOf.MakeGenericType(baseType);
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
                    if (property.Type.FullName().StartsWith("Collection"))
                    {
                        var typeName = collectionRegex.Match(property.Type.FullName()).Groups[1].Value;
                        Type listOf = typeof(List<>);
                        var baseType = _typeBuildersDict.ContainsKey(typeName) ? _typeBuildersDict[typeName].Builder : typeof(string);
                        var selfContained = listOf.MakeGenericType(baseType);



                        propertyType = selfContained;
                    }
                    else
                    {
                        var previouslyBuiltType = _typeBuildersDict[property.Type.FullName()];

                        propertyType = previouslyBuiltType.Builder;
                    }

                }
            }
            if (property.Type.IsNullable && Nullable.GetUnderlyingType(propertyType) != null)
            {
                Type nullableOf = typeof(Nullable<>);
                Type selfContained = nullableOf.MakeGenericType(propertyType);
                propertyType = selfContained;
            }
            PropertyBuilderHelper.BuildProperty(typeBuilder, propertyName, propertyType);
        }

        internal static void GenerateEnum(IEdmEnumMember member, EnumBuilder enumBuilder, ModuleBuilder moduleBuilder)
        {
            var memberName = member.Name;

            var memberValue = Convert.ToInt32(member.Value.Value);
            enumBuilder.DefineLiteral(memberName, memberValue);
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

        public static Type GenerateDbContext(string csdlFile)
        {
            var model = ReadModel(csdlFile);
            var name = csdlFile.Split('\\').Last();

            // create a dynamic assembly and module 
            AssemblyName assemblyName = new AssemblyName();
            assemblyName.Name = name;
            AssemblyBuilder assemblyBuilder = Thread.GetDomain().DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
            ModuleBuilder module;
            module = assemblyBuilder.DefineDynamicModule($"{assemblyName.Name}.dll");
            BuildModules(model, module);
            Assembly assembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.GetName().Name == name);
            Type entitiesType = assembly.GetTypes().FirstOrDefault(t => t.Name == "Entities");
            return entitiesType;
        }

        static void Main(string[] args)
        {
            //TODO: grab edm path and assembly name from cmdline args
            var model = ReadModel(@"C:\repos\fun\lab\ApiAsAService\Trippin.xml");
            var name = "Trippin2";

            // create a dynamic assembly and module 
            AssemblyName assemblyName = new AssemblyName();
            assemblyName.Name = name;
            AssemblyBuilder assemblyBuilder = Thread.GetDomain().DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.RunAndSave);
            ModuleBuilder module;
            module = assemblyBuilder.DefineDynamicModule($"{assemblyName.Name}.dll");
            BuildModules(model, module);

            assemblyBuilder.Save($"{assemblyName.Name}.dll");
        }
    }
}
