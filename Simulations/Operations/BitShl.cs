using Mono.Cecil.Cil;
using System.Collections.Generic;

namespace Fody.Simulations.Operations
{
    /// <summary>
    /// Shift left. (x &lt;&lt; y)
    /// </summary>
    public class BitShl(ILoadable value1, ILoadable value2) : BinaryOperation(value1, value2)
    {
        public override IList<Instruction> Load()
        {
            return [.. Value1.Load(), .. Value2.Load(), Instruction.Create(OpCodes.Shl)];
        }
    }
}
