using Mono.Cecil.Cil;

namespace Fody.Simulations.Operations
{
    public abstract class BinaryOperation(ILoadable value1, ILoadable value2) : Operation
    {
        protected ILoadable Value1 => value1;
        protected ILoadable Value2 => value2;

        public override OpCode TrueToken => value1.TrueToken;

        public override OpCode FalseToken => value2.FalseToken;

        public override SimulationModuleWeaver ModuleWeaver => value1.ModuleWeaver ?? value2.ModuleWeaver;
    }
}
