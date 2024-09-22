using Fody;
using System.Linq;

namespace Mono.Cecil
{
    public static class CustomAttributeExtensions
    {
        public static bool IsCompilerGenerated(this TypeReference typeRef)
        {
            return IsCompilerGenerated(typeRef.ToDefinition().CustomAttributes);
        }

        public static bool IsCompilerGenerated(this MethodReference methodRef)
        {
            return IsCompilerGenerated(methodRef.ToDefinition().CustomAttributes);
        }

        private static bool IsCompilerGenerated(Collections.Generic.Collection<CustomAttribute> customAttributes)
        {
            return customAttributes.Any(x => x.AttributeType.Is(Constants.TYPE_CompilerGeneratedAttribute) || x.AttributeType.Is(Constants.TYPE_Runtime_CompilerGeneratedAttribute));
        }
    }
}
