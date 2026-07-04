using Mono.Cecil;
using Mono.Cecil.Rocks;
using System.Collections.Generic;
using System.Linq;

namespace Fody
{
    public static class ImportExtensions
    {
        public static TypeReference Import(this BaseModuleWeaver moduleWeaver, TypeReference typeRef)
        {
            if (typeRef is ByReferenceType brt) return new ByReferenceType(Import(moduleWeaver, brt.ElementType));
            if (typeRef is GenericParameter) return typeRef;
            if (typeRef is ArrayType at)
            {
                var arrayType = new ArrayType(Import(moduleWeaver, at.ElementType), at.Rank);
                for (var i = 0; i < at.Rank; i++)
                {
                    arrayType.Dimensions[i] = at.Dimensions[i];
                }
                return arrayType;
            }

            var typeDef = typeRef.Resolve();
            typeDef = ResolveBestTypeDefinition(moduleWeaver, typeDef);

            var iTypeRef = typeDef.ImportInto(moduleWeaver.ModuleDefinition);
            if (typeRef is GenericInstanceType git)
            {
                var igas = new List<TypeReference>(git.GenericArguments.Count);
                foreach (var ga in git.GenericArguments)
                {
                    var iga = Import(moduleWeaver, ga);
                    igas.Add(iga);
                }
                iTypeRef = iTypeRef.MakeGenericInstanceType(igas.ToArray());
            }

            return iTypeRef;
        }

        /// <summary>
        /// 决定保留类型原始解析所在的 scope，还是将其重定向到目标模块引用链中找到的定义。
        /// </summary>
        /// <remarks>
        /// 当目标模块已经引用了类型的原始 scope 时，优先保留原始 scope。
        /// 这一点对 NuGet polyfill 包（如 System.Threading.Tasks.Extensions、Microsoft.Bcl.AsyncInterfaces、
        /// System.Memory 等）至关重要：如果将其类型重定向到 facade 程序集（如 netstandard.dll），
        /// Mono 在运行时可能无法正确解析类型转发链，从而抛出 MissingMethodException。
        /// </remarks>
        private static TypeDefinition ResolveBestTypeDefinition(BaseModuleWeaver moduleWeaver, TypeDefinition original)
        {
            // 目标模块自身定义的类型不需要重定向。
            if (original.Module == moduleWeaver.ModuleDefinition)
                return original;

            var originalScopeName = original.Scope?.Name;
            if (string.IsNullOrEmpty(originalScopeName))
                return original;

            var normalizedOriginalScope = NormalizeAssemblyName(originalScopeName);

            // 如果目标模块已经引用了原始程序集，则保留该 scope。
            var targetReferencesOriginalScope = moduleWeaver.ModuleDefinition.AssemblyReferences
                .Any(ar => NormalizeAssemblyName(ar.Name) == normalizedOriginalScope);
            if (targetReferencesOriginalScope)
                return original;

            if (!moduleWeaver.TryFindTypeDefinition(original.FullName, out var candidate))
                return original;

            // 不要重定向到纯 facade 程序集。facade 只包含类型转发，依赖运行时解析转发链，
            // 而 Mono 对此支持不完整。
            if (IsFacadeAssembly(candidate.Module.Assembly))
                return original;

            return candidate;
        }

        private static bool IsFacadeAssembly(AssemblyDefinition assembly)
        {
            if (assembly == null) return false;

            var module = assembly.MainModule;
            if (!module.HasExportedTypes) return false;

            // 纯 facade 程序集只包含导出的类型转发，没有真正的实现类型（只有必需的 <Module>）。
            foreach (var type in module.Types)
            {
                if (type.FullName == "<Module>") continue;
                return false;
            }

            return true;
        }

        private static string NormalizeAssemblyName(string? name)
        {
            if (name == null || name.Length == 0) return string.Empty;
            if (name.EndsWith(".dll", System.StringComparison.OrdinalIgnoreCase) ||
                name.EndsWith(".exe", System.StringComparison.OrdinalIgnoreCase))
            {
                return name.Substring(0, name.Length - 4);
            }
            return name;
        }

        public static MethodReference Import(this BaseModuleWeaver moduleWeaver, MethodReference methodRef)
        {
            var declaringTypeRef = Import(moduleWeaver, methodRef.DeclaringType);
            var returnTypeRef = Import(moduleWeaver, methodRef.ReturnType);

            var reference = new MethodReference(methodRef.Name, returnTypeRef, declaringTypeRef)
            {
                HasThis = methodRef.HasThis,
                ExplicitThis = methodRef.ExplicitThis,
                CallingConvention = methodRef.CallingConvention
            };

            foreach (var gp in methodRef.GenericParameters)
            {
                reference.GenericParameters.Add(new GenericParameter(gp.Name, reference));
            }

            foreach (var p in methodRef.Parameters)
            {
                reference.Parameters.Add(p.Clone(Import(moduleWeaver, p.ParameterType)));
            }

            return moduleWeaver.ModuleDefinition.ImportReference(reference);
        }
    }
}
