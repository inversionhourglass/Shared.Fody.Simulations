using Mono.Cecil.Cil;
using System.Collections.Generic;

namespace Fody.Simulations.Operations
{
    public interface ICondition : IAssertable
    {
        IList<Instruction> LoadCondition();
    }
}
