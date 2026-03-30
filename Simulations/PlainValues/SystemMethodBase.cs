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
            if (methodDef.HasGenericParameters)
            {
                var getCurrentMethodRef = ModuleWeaver._tMethodBaseRef
                    .GetMethod(false, x => x.IsStatic && x.Name == "GetCurrentMethod" && x.Parameters.Count == 0 && x.ReturnType.FullName == ModuleWeaver._tMethodBaseRef.FullName)!
                    .ImportInto(ModuleWeaver);
                return [Instruction.Create(OpCodes.Call, getCurrentMethodRef)];
            }

            return [
                Instruction.Create(OpCodes.Ldtoken, methodDef),
                Instruction.Create(OpCodes.Ldtoken, methodDef.DeclaringType),
                Instruction.Create(OpCodes.Call, ModuleWeaver._mGetMethodFromHandleRef)
            ];
        }
    }
}
