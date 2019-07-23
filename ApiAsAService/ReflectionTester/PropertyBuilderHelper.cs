namespace ReflectionTester
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

            // The last argument of DefineProperty is null, because the
            // property has no parameters. (If you don't specify null, you must
            // specify an array of Type objects. For a parameterless property,
            // use an array with no elements: new Type[] {})
            PropertyBuilder propBuilder = typeBuilder.DefineProperty(fieldName,
                                                             PropertyAttributes.HasDefault,
                                                             fieldType,
                                                             null);

            // The property set and property get methods require a special
            // set of attributes.
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

            // Last, we must map the two methods created above to our PropertyBuilder to 
            // their corresponding behaviors, "get" and "set" respectively. 
            propBuilder.SetGetMethod(propMethodBldr);
            propBuilder.SetSetMethod(propSetMethodBldr);

            

        }
    }
}
