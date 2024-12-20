﻿using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.Linq;
using static Mono.Cecil.Cil.Instruction;

namespace Fody.Simulations
{
    public class ArgumentSimulation(ParameterDefinition parameterDef, SimulationModuleWeaver moduleWeaver) : Simulation(moduleWeaver), ILoadable, IParameterSimulation, IAssignable
    {
        public ParameterDefinition Def => parameterDef;

        public virtual TypeSimulation Type { get; } = parameterDef.ParameterType is ByReferenceType refType ? refType.ElementType.Simulate(moduleWeaver) : parameterDef.ParameterType.Simulate(moduleWeaver);

        public virtual bool IsByReference => parameterDef.ParameterType.IsByReference;

        public OpCode TrueToken => Type.TrueToken;

        public OpCode FalseToken => Type.FalseToken;

        public virtual IList<Instruction> Assign(Func<IAssignable, IList<Instruction>> valueFactory)
        {
            if (IsByReference)
            {
                return [Create(OpCodes.Ldarg, parameterDef), .. valueFactory(this), Type.Ref.Stind()];
            }
            return [.. valueFactory(this), Create(OpCodes.Starg, parameterDef)];
        }

        public virtual IList<Instruction> AssignDefault(TypeSimulation type)
        {
            var argTypeRef = Type.Ref;
            if (argTypeRef.IsValueType || argTypeRef.IsGenericParameter)
            {
                return [Create(IsByReference ? OpCodes.Ldarg : OpCodes.Ldarga, parameterDef), Create(OpCodes.Initobj, argTypeRef)];
            }
            if (IsByReference)
            {
                return [Create(OpCodes.Ldarg, parameterDef), Create(OpCodes.Ldnull), argTypeRef.Stind()];
            }
            return [Create(OpCodes.Ldnull), Create(OpCodes.Starg, parameterDef)];
        }

        public virtual IList<Instruction> Load()
        {
            if (!IsByReference)
            {
                return [Create(OpCodes.Ldarg, parameterDef)];
            }
            return [Create(OpCodes.Ldarg, parameterDef), Type.Ref.Ldind()];
        }

        public virtual IList<Instruction> Cast(TypeReference to)
        {
            return Type.Cast(to);
        }

        public virtual IList<Instruction> PrepareLoadAddress(MethodSimulation? method)
        {
            return [];
        }

        public virtual IList<Instruction> LoadAddress(MethodSimulation? method)
        {
            return [Create(OpCodes.Ldarg, parameterDef)];
        }
    }

    public static class ArgumentSimulationExtensions
    {
        public static ArgumentSimulation Simulate(this ParameterDefinition parameterDef, SimulationModuleWeaver moduleWeaver)
        {
            return new(parameterDef, moduleWeaver);
        }

        public static IList<Instruction> AssignDefault(this ArgumentSimulation arg) => arg.AssignDefault(arg.Type);

        public static bool IsRefStruct(this ArgumentSimulation arg)
        {
            var def = arg.Def.ParameterType.ToDefinition();
            return def != null && def.CustomAttributes.Any(y => y.Is(Constants.TYPE_IsByRefLikeAttribute));
        }

        public static FakeArgumentSimulation BeFake(this ArgumentSimulation arg, ILoadable loadable) => new(loadable, arg.Def, arg.ModuleWeaver);
    }
}
