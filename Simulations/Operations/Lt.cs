using Mono.Cecil.Cil;
using System.Collections.Generic;

namespace Fody.Simulations.Operations
{
    public class Lt(ILoadable value1, ILoadable value2) : BinaryOperation(value1, value2), ICondition
    {
        public override OpCode TrueToken => OpCodes.Blt;

        public override OpCode FalseToken => OpCodes.Bge;

        public override IList<Instruction> Load()
        {
            return [.. Value1.Load(), .. Value2.Load(), Instruction.Create(OpCodes.Clt)];
        }

        public IList<Instruction> LoadCondition()
        {
            return [.. Value1.Load(), .. Value2.Load()];
        }
    }
}
