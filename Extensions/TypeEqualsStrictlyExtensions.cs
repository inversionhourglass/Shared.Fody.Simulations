using Fody;
using System.Runtime.CompilerServices;

namespace Mono.Cecil
{
    public static class TypeEqualsStrictlyExtensions
    {
        public static bool StrictIs(this TypeReference typeRef, string fullName)
        {
            if (typeRef is RequiredModifierType rmt)
            {
                typeRef = rmt.ElementType;
            }
            return typeRef.FullName == fullName;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool StrictIsVoid(this TypeReference typeRef)
        {
            return typeRef.StrictIs(Constants.TYPE_Void);
        }
    }
}
