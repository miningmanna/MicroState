using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;

namespace MicroState
{
    public static class StateHandleWrapper
    {
        private static object dictLock = new object();
        private static Dictionary<Type, Type> runtimeWrapperTypes = new Dictionary<Type, Type>();


        private static AssemblyBuilder asmBuilder;
        private static ModuleBuilder modBuilder;
        static StateHandleWrapper()
        {
            asmBuilder = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName(Guid.NewGuid().ToString()), AssemblyBuilderAccess.Run);
            modBuilder = asmBuilder.DefineDynamicModule("StateMachineWrappers");
        }

        /// <summary>
        /// Preemptively load a state base class for handle generation. This can be done to lower init time for the first state machine instance.
        /// </summary>
        /// <typeparam name="ST">The base class of the states</typeparam>
        /// <typeparam name="CT">The context class</typeparam>
        public static void Load<ST, CT>()
            where ST : State<CT>
        {
            lock(dictLock)
            {
                var tType = typeof(ST);
                if (tType.BaseType != typeof(State<CT>))
                    throw new ArgumentException($"The type is not a direct subtype of State<{typeof(CT).Name}>", nameof(ST));
                if (tType.GetInterfaces().Length != 0)
                    throw new ArgumentException($"The type must not implement any interfaces", nameof(ST));

                if (!runtimeWrapperTypes.ContainsKey(tType))
                    runtimeWrapperTypes[tType] = CreateWrapperType<ST, CT>();
            }
        }

        internal static ST Create<ST, CT>(StateMachine<ST, CT> stateMachine)
            where ST : State<CT>
        {
            lock (dictLock)
            {
                var tType = typeof(ST);
                if (tType.BaseType != typeof(State<CT>))
                    throw new ArgumentException($"The type is not a direct subtype of State<{typeof(CT).Name}>", nameof(ST));
                if (tType.GetInterfaces().Length != 0)
                    throw new ArgumentException($"The type must not implement any interfaces", nameof(ST));

                Type runtimeType = null;
                if (!runtimeWrapperTypes.ContainsKey(tType))
                {
                    runtimeType = CreateWrapperType<ST, CT>();
                    runtimeWrapperTypes[tType] = runtimeType;
                }
                else
                {
                    runtimeType = runtimeWrapperTypes[tType];
                }

                Action<Action<ST>> doLeaf = stateMachine.DoLeafsFirst;
                return (ST) runtimeType.GetConstructor(new Type[] { typeof(Action<Action<ST>>) }).Invoke(new object[] { doLeaf });
            }
        }

        private static Type CreateWrapperType<ST, CT>()
            where ST : State<CT>
        {
            var tType = typeof(ST);
            var smType = typeof(StateMachine<ST, CT>);
            var doLeafsType = typeof(Action<Action<ST>>);
            var typeBuilder = modBuilder.DefineType($"{tType.Name}_StateMachineWrapper", TypeAttributes.Public, tType);
            var nestedTypes = new List<TypeBuilder>();

            var doLeafsField = typeBuilder.DefineField("doFunc", doLeafsType, FieldAttributes.Private);

            {
                var constructorBuilder = typeBuilder.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, new Type[] { doLeafsType });
                var gen = constructorBuilder.GetILGenerator();

                // Store reference to doLeafs
                gen.Emit(OpCodes.Ldarg_0);
                gen.Emit(OpCodes.Ldarg_1);
                gen.Emit(OpCodes.Stfld, doLeafsField);
                gen.Emit(OpCodes.Ret);
            }

            var metAttribs = MethodAttributes.Public | MethodAttributes.ReuseSlot | MethodAttributes.Virtual | MethodAttributes.HideBySig;
            foreach (var method in tType.GetMethods())
            {
                var attribs = method.Attributes;
                if (!attribs.HasFlag(MethodAttributes.Virtual) || !attribs.HasFlag(MethodAttributes.Public))
                    continue;
                if (method.ReturnType != typeof(void))
                    continue;
                if (method.Name == "OnEnter")
                    continue;
                if (method.Name == "OnExit")
                    continue;

                var paramInfo = method.GetParameters();
                var paramTypes = paramInfo.Select(i => i.ParameterType).ToArray();

                var displayTypeBuilder = typeBuilder.DefineNestedType($"lambda_disp_{method.Name}");
                nestedTypes.Add(displayTypeBuilder);
                var displayTypeConstructorBuilder = displayTypeBuilder.DefineConstructor(MethodAttributes.Public, CallingConventions.HasThis, paramTypes);
                var displayTypeConstructorGen = displayTypeConstructorBuilder.GetILGenerator();

                var displayTypeMetBuilder = displayTypeBuilder.DefineMethod($"method", MethodAttributes.Public, CallingConventions.HasThis, null, new Type[] { typeof(ST) });
                var displayTypeMetGen = displayTypeMetBuilder.GetILGenerator();

                displayTypeMetGen.Emit(OpCodes.Ldarg_1);
                var paramFields = new FieldBuilder[paramInfo.Length];
                for (int i = 0; i < paramInfo.Length; i++)
                {
                    var info = paramInfo[i];
                    paramFields[i] = displayTypeBuilder.DefineField($"arg{i}", info.ParameterType, FieldAttributes.Private);

                    displayTypeConstructorGen.Emit(OpCodes.Ldarg_0);
                    displayTypeConstructorGen.Emit(OpCodes.Ldarg_S, (byte) i + 1);
                    displayTypeConstructorGen.Emit(OpCodes.Stfld, paramFields[i]);

                    displayTypeMetGen.Emit(OpCodes.Ldarg_0);
                    displayTypeMetGen.Emit(OpCodes.Ldfld, paramFields[i]);

                }
                displayTypeConstructorGen.Emit(OpCodes.Ret);

                displayTypeMetGen.Emit(OpCodes.Callvirt, method);
                displayTypeMetGen.Emit(OpCodes.Ret);

                var metBuild = typeBuilder.DefineMethod(method.Name, metAttribs, typeof(void), paramTypes);
                var gen = metBuild.GetILGenerator();

                // Get doLeafs
                gen.Emit(OpCodes.Ldarg_0);
                gen.Emit(OpCodes.Ldfld, doLeafsField);

                // Load parameters
                for (int i = 0; i < paramInfo.Length; i++)
                {
                    gen.Emit(OpCodes.Ldarg_S, (byte) i + 1);
                }

                gen.Emit(OpCodes.Newobj, displayTypeConstructorBuilder);

                gen.Emit(OpCodes.Ldftn, displayTypeMetBuilder);
                gen.Emit(OpCodes.Newobj, typeof(Action<ST>).GetConstructors()[0]);

                gen.Emit(OpCodes.Callvirt, doLeafsType.GetMethod("Invoke"));
                gen.Emit(OpCodes.Ret);
            }


            foreach (var nested in nestedTypes)
                nested.CreateTypeInfo();

            var type = typeBuilder.CreateTypeInfo().AsType();
            return type;
        }

    }
}
