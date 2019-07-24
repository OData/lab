namespace EdmObjectsGenerator
{
    using System;
    using System.Reflection;
    using System.Reflection.Emit;

    public class PropertyBuilderHelper
    {
        public static void BuildProperty(TypeBuilder typeBuilder,  string fieldName, Type fieldType)
        {
            FieldBuilder fieldBldr = typeBuilder.DefineField(fieldName,
                                                            fieldType,
                                                            FieldAttributes.Private);

           
            PropertyBuilder propBuilder = typeBuilder.DefineProperty(fieldName,
                                                             PropertyAttributes.HasDefault,
                                                             fieldType,
                                                             null);

           
            MethodAttributes getSetAttr =
                MethodAttributes.Public | MethodAttributes.SpecialName |
                    MethodAttributes.HideBySig;

            // Define the "get" accessor method for CustomerName.
            MethodBuilder propMethodBldr =
                typeBuilder.DefineMethod($"get_{fieldName}",
                                           getSetAttr,
                                           fieldType,
                                           Type.EmptyTypes);

            ILGenerator custNameGetIL = propMethodBldr.GetILGenerator();

            custNameGetIL.Emit(OpCodes.Ldarg_0);
            custNameGetIL.Emit(OpCodes.Ldfld, fieldBldr);
            custNameGetIL.Emit(OpCodes.Ret);

            // Define the "set" accessor method for CustomerName.
            MethodBuilder propSetMethodBldr =
                typeBuilder.DefineMethod($"set_{fieldName}",
                                           getSetAttr,
                                           null,
                                           new Type[] { fieldType });

            ILGenerator custNameSetIL = propSetMethodBldr.GetILGenerator();

            custNameSetIL.Emit(OpCodes.Ldarg_0);
            custNameSetIL.Emit(OpCodes.Ldarg_1);
            custNameSetIL.Emit(OpCodes.Stfld, fieldBldr);
            custNameSetIL.Emit(OpCodes.Ret);

           
            propBuilder.SetGetMethod(propMethodBldr);
            propBuilder.SetSetMethod(propSetMethodBldr);

            

        }
    }
}
