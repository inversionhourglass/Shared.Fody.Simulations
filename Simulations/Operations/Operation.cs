using Mono.Cecil.Cil;
using Mono.Cecil;
using System.Collections.Generic;

namespace Fody.Simulations.Operations
{
    public abstract class Operation : ILoadable
    {
        public TypeSimulation Type => ModuleWeaver._simulations.Bool;

        public abstract OpCode TrueToken { get; }

        public abstract OpCode FalseToken { get; }

        public abstract SimulationModuleWeaver ModuleWeaver { get; }

        public IList<Instruction> Cast(TypeReference to) => Type.Cast(to);

        public abstract IList<Instruction> Load();
    }
}
