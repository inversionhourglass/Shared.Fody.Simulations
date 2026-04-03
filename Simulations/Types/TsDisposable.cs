using Mono.Cecil;

namespace Fody.Simulations.Types;

public class TsDisposable(TypeReference typeRef, IHost? host, SimulationModuleWeaver moduleWeaver) : TypeSimulation(typeRef, host, moduleWeaver)
{
    public MethodSimulation M_Dispose => MethodSimulate(Constants.METHOD_Dispose, false);
}
