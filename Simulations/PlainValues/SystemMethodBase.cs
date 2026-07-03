using Mono.Cecil;
using Mono.Cecil.Cil;
using System.Collections.Generic;
using System.Diagnostics;

namespace Fody.Simulations.PlainValues
{
    [DebuggerDisplay("{methodDef}")]
    public class SystemMethodBase(MethodDefinition methodDef, SimulationModuleWeaver moduleWeaver) : PlainValueSimulation(moduleWeaver)
    {
        public override TypeSimulation Type => ModuleWeaver._simulations.MethodBase;

        public override IList<Instruction> Load()
        {
            // 使用 ldtoken 直接加载目标方法的 token，不依赖运行时当前执行上下文。
            // 对于 async/iterator 状态机，这段 IL 会被织入到 MoveNext 中，如果使用
            // MethodBase.GetCurrentMethod() 会得到 MoveNext 而非原始方法。
            return [
                Instruction.Create(OpCodes.Ldtoken, methodDef),
                Instruction.Create(OpCodes.Ldtoken, methodDef.DeclaringType),
                Instruction.Create(OpCodes.Call, ModuleWeaver._mGetMethodFromHandleRef)
            ];
        }
    }
}
