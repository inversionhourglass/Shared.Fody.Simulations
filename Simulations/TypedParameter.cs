﻿using Mono.Cecil;
using Mono.Cecil.Cil;
using System.Collections.Generic;
using System.Diagnostics;

namespace Fody.Simulations
{
    [DebuggerDisplay("Type = {Type}, Parameter = {parameter}")]
    public class TypedParameter(TypeSimulation type, IParameterSimulation parameter) : IParameterSimulation
    {
        public TypeSimulation Type => type;

        public OpCode TrueToken => parameter.TrueToken;

        public OpCode FalseToken => parameter.FalseToken;

        public SimulationModuleWeaver ModuleWeaver => type.ModuleWeaver;

        public IList<Instruction> Cast(TypeReference to) => parameter.Cast(to);

        public IList<Instruction> Load() => parameter.Load();

        public IList<Instruction> LoadAddress(MethodSimulation? method) => parameter.LoadAddress(method);

        public IList<Instruction> PrepareLoadAddress(MethodSimulation? method) => parameter.PrepareLoadAddress(method);
    }

    public static class TypedParameterExtensions
    {
        public static TypedParameter Typed(this IParameterSimulation parameter, TypeSimulation type)
        {
            return new(type, parameter);
        }
    }
}
