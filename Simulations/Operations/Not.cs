using Mono.Cecil.Cil;
using System.Collections.Generic;

namespace Fody.Simulations.Operations
{
    public class Not(ILoadable value) : UnaryOperation(value)
    {
        public override IList<Instruction> Load()
        {
            return [.. Value.Load(), Instruction.Create(OpCodes.Not)];
        }
    }
}
