using Mono.Cecil;
using Mono.Cecil.Cil;
using System.Collections.Generic;
using System.Diagnostics;

namespace Fody.Simulations.PlainValues
{
    [DebuggerDisplay("{methodDef}")]
    public class SystemMethodBase(MethodDefinition methodDef, TypeDefinition? stateMachineTypeDef, SimulationModuleWeaver moduleWeaver) : PlainValueSimulation(moduleWeaver)
    {
        public override TypeSimulation Type => ModuleWeaver._simulations.MethodBase;

        public override IList<Instruction> Load()
        {
            var methodRef = ResolveMethodReference(methodDef, stateMachineTypeDef);

            // 使用 ldtoken 直接加载目标方法的 token，不依赖运行时当前执行上下文。
            // 对于 async/iterator 状态机，这段 IL 会被织入到 MoveNext 中，如果使用
            // MethodBase.GetCurrentMethod() 会得到 MoveNext 而非原始方法。
            return [
                Instruction.Create(OpCodes.Ldtoken, methodRef),
                Instruction.Create(OpCodes.Ldtoken, methodRef.DeclaringType),
                Instruction.Create(OpCodes.Call, ModuleWeaver._mGetMethodFromHandleRef)
            ];
        }

        /// <summary>
        /// 解析用于 ldtoken 的方法引用。
        /// </summary>
        /// <remarks>
        /// 在 async/iterator 状态机的 MoveNext 中，如果直接对原始泛型方法定义使用 ldtoken，
        /// Mono JIT 会因为无法建立方法级泛型参数实例而断言失败（method-to-ir.c:2467）。
        /// 因此需要将方法级泛型参数映射到状态机类型级泛型参数，构造一个已实例化的方法 token。
        /// </remarks>
        private static MethodReference ResolveMethodReference(MethodDefinition methodDef, TypeDefinition? stateMachineTypeDef)
        {
            if (!methodDef.HasGenericParameters)
                return methodDef;

            // 不在状态机中时，直接使用方法定义（非状态机内的泛型方法不存在泛型上下文冲突）。
            if (stateMachineTypeDef == null)
                return methodDef;

            if (!stateMachineTypeDef.HasGenericParameters ||
                stateMachineTypeDef.GenericParameters.Count < methodDef.GenericParameters.Count)
            {
                throw new FodyWeavingException(
                    $"Cannot construct an instantiated token for generic method {methodDef} in state machine {stateMachineTypeDef}: " +
                    $"the state machine type does not have enough generic parameters.");
            }

            var instantiated = new GenericInstanceMethod(methodDef);
            foreach (var methodGp in methodDef.GenericParameters)
            {
                var typeGp = stateMachineTypeDef.GenericParameters[methodGp.Position];
                instantiated.GenericArguments.Add(typeGp);
            }

            return instantiated;
        }
    }
}
