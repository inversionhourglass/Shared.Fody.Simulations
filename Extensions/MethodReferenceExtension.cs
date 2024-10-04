using Fody;
using Mono.Cecil.Cil;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using BindingFlags = System.Reflection.BindingFlags;

namespace Mono.Cecil
{
    public static class MethodReferenceExtension
    {
        private static readonly Lazy<Func<SequencePoint, InstructionOffset>> _SequencePointGetOffset = new(() =>
        {
            var parameter = Expression.Parameter(typeof(SequencePoint), "sequencePoint");
            var field = typeof(SequencePoint).GetField("offset", BindingFlags.NonPublic | BindingFlags.Instance);
            var fieldAccess = Expression.Field(parameter, field);
            var lambda = Expression.Lambda<Func<SequencePoint, InstructionOffset>>(fieldAccess, parameter);
            return lambda.Compile();
        });
        private static readonly Lazy<Func<InstructionOffset, Instruction>> _InstructionOffsetGetInstruction = new(() =>
        {
            var parameter = Expression.Parameter(typeof(InstructionOffset), "instructionOffset");
            var field = typeof(InstructionOffset).GetField("instruction", BindingFlags.NonPublic | BindingFlags.Instance);
            var fieldAccess = Expression.Field(parameter, field);
            var lambda = Expression.Lambda<Func<InstructionOffset, Instruction>>(fieldAccess, parameter);
            return lambda.Compile();
        });

        public static MethodDefinition ToDefinition(this MethodReference methodRef) => methodRef is MethodDefinition methodDef ? methodDef : methodRef.Resolve();

        public static VariableDefinition CreateVariable(this MethodBody body, TypeReference variableTypeReference)
        {
            var variable = new VariableDefinition(variableTypeReference);
            body.Variables.Add(variable);
            return variable;
        }

        public static MethodReference WithGenericDeclaringType(this MethodReference methodRef, TypeReference typeRef)
        {
            methodRef = methodRef.ImportInto(typeRef.Module);
            var typeDef = typeRef.Resolve();
            if (typeDef != null && typeDef != methodRef.DeclaringType.Resolve() && typeDef.IsInterface) return methodRef;

            var genericMethodRef = new MethodReference(methodRef.Name, methodRef.ReturnType, typeRef)
            {
                HasThis = methodRef.HasThis,
                ExplicitThis = methodRef.ExplicitThis,
                CallingConvention = methodRef.CallingConvention
            };
            foreach (var parameter in methodRef.Parameters)
            {
                genericMethodRef.Parameters.Add(new ParameterDefinition(parameter.ParameterType));
            }
            foreach (var parameter in methodRef.GenericParameters)
            {
                genericMethodRef.GenericParameters.Add(new GenericParameter(parameter.Name, genericMethodRef));
            }

            return genericMethodRef;
        }

        public static MethodReference WithGenerics(this MethodReference methodRef, params TypeReference[] genericTypeRefs)
        {
            if (genericTypeRefs.Length == 0) return methodRef;

            var genericInstanceMethod = new GenericInstanceMethod(methodRef.GetElementMethod());
            genericInstanceMethod.GenericArguments.Add(genericTypeRefs);

            return genericInstanceMethod;
        }

        public static TypeDefinition ResolveStateMachine(this MethodDefinition methodDef, string stateMachineAttributeName)
        {
            var stateMachineAttr = methodDef.CustomAttributes.Single(attr => attr.Is(stateMachineAttributeName));
            var obj = stateMachineAttr.ConstructorArguments[0].Value;
            return obj as TypeDefinition ?? ((TypeReference)obj).Resolve();
        }

        public static bool TryResolveStateMachine(this MethodDefinition methodDef, string stateMachineAttributeName, out TypeDefinition stateMachineTypeDef)
        {
            stateMachineTypeDef = null!;
            var stateMachineAttr = methodDef.CustomAttributes.SingleOrDefault(attr => attr.Is(stateMachineAttributeName));
            if (stateMachineAttr == null) return false;
            var obj = stateMachineAttr.ConstructorArguments[0].Value;
            stateMachineTypeDef = obj as TypeDefinition ?? ((TypeReference)obj).Resolve();
            return true;
        }

        public static bool TryResolveStateMachine(this MethodReference methodRef, out TypeDefinition? stateMachineTypeDef, out CustomAttribute stateMachineAttribute)
        {
            stateMachineTypeDef = null;
            stateMachineAttribute = methodRef.ToDefinition().CustomAttributes.FirstOrDefault(attr => attr.AttributeType.Inherit(Constants.TYPE_StateMachineAttribute));
            if (stateMachineAttribute == null) return false;
            stateMachineTypeDef = ((TypeReference)stateMachineAttribute.ConstructorArguments[0].Value).ToDefinition();
            return true;
        }

        public static void Clear(this MethodDefinition methodDef)
        {
            methodDef.Body.Instructions.Clear();
            methodDef.Body.Variables.Clear();
            methodDef.Body.ExceptionHandlers.Clear();
            methodDef.HardClearCustomDebugInformation();
            methodDef.DebugInformation.Clear();
        }

        public static void Clear(this MethodDebugInformation debugInformation)
        {
            debugInformation.CustomDebugInformations.Clear();
            debugInformation.SequencePoints.Clear();
            if (debugInformation.Scope != null)
            {
                debugInformation.Scope.Constants.Clear();
                debugInformation.Scope.Variables.Clear();
                debugInformation.Scope.Scopes.Clear();
                debugInformation.Scope.CustomDebugInformations.Clear();
            }
        }

        public static void HardClearCustomDebugInformation(this MethodDefinition methodDef)
        {
            methodDef.CustomDebugInformations.Clear();

            var module = methodDef.Module;
            var metadata = module.GetType().GetField("MetadataSystem", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(module);
            if (metadata == null) return;

            var customDebugInformations = (IDictionary)metadata.GetType().GetField("CustomDebugInformations", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(metadata);
            if (customDebugInformations == null) return;

            customDebugInformations.Remove(methodDef.MetadataToken);
        }

        public static Instruction? GetFirstInstruction(this MethodDefinition methodDef)
        {
            if (methodDef.IsConstructor && !methodDef.IsStatic)
            {
                var baseType = methodDef.DeclaringType.BaseType;
                foreach (var instruction in methodDef.Body.Instructions)
                {
                    if (instruction.OpCode.Code == Code.Call &&
                        instruction.Operand is MethodReference mr && mr.ToDefinition().IsConstructor &&
                        (mr.DeclaringType == methodDef.DeclaringType || mr.DeclaringType == baseType))
                    {
                        return instruction.Next;
                    }
                }
            }

            return methodDef.Body.Instructions.FirstOrDefault();
        }

        public static Instruction[] MergeReturnToLeave(this MethodDefinition methodDef)
        {
            Instruction[] returnBlock;
            VariableDefinition? vResult = null;
            if (methodDef.ReturnType.IsVoid())
            {
                returnBlock = [Instruction.Create(OpCodes.Ret)];
            }
            else
            {
                vResult = methodDef.Body.CreateVariable(methodDef.ReturnType);
                returnBlock = [Instruction.Create(OpCodes.Ldloc, vResult), Instruction.Create(OpCodes.Ret)];
            }

            var returns = new List<Instruction>();
            var instructions = methodDef.Body.Instructions;
            foreach (var instruction in instructions)
            {
                if (instruction.OpCode.Code == Code.Ret)
                {
                    returns.Add(instruction);
                }
            }
            var returnBlockStart = returnBlock[0];
            foreach (var ret in returns)
            {
                if (vResult == null)
                {
                    ret.Set(OpCodes.Leave, returnBlockStart);
                }
                else
                {
                    ret.Set(OpCodes.Stloc, vResult);
                    instructions.InsertAfter(ret, Instruction.Create(OpCodes.Leave, returnBlockStart));
                }
            }

            return returnBlock;
        }

        /// <summary>
        /// 获取方法的最外层异常处理器
        /// </summary>
        public static ExceptionHandler? GetOuterExceptionHandler(this MethodReference methodRef)
        {
            var methodDef = methodRef.ToDefinition();
            ExceptionHandler? earliestHandler = null;
            ExceptionHandler? latestHandler = null;

            foreach (var handler in methodDef.Body.ExceptionHandlers)
            {
                var earliestState = 0;
                var latestState = 0;
                if (earliestHandler == null || earliestHandler.TryStart.Offset > handler.TryStart.Offset)
                {
                    earliestHandler = handler;
                    earliestState = 1;
                }
                else if (earliestHandler.TryStart.Offset < handler.TryStart.Offset)
                {
                    earliestState = -1;
                }
                if (latestHandler == null || latestHandler.TryEnd.Offset < handler.TryEnd.Offset)
                {
                    latestHandler = handler;
                    latestState = 1;
                }
                else if (latestHandler.TryEnd.Offset > handler.TryEnd.Offset)
                {
                    latestState = -1;
                }
                if (earliestState == 0 && latestState == 1)
                {
                    earliestHandler = handler;
                }
                if (latestState == 0 && earliestState == 1)
                {
                    latestHandler = handler;
                }
            }

            return earliestHandler == latestHandler ? earliestHandler : null;
        }

        /// <summary>
        /// 获取方法的最外层异常处理器，如果找不到则创建一个。注意，针对<see cref="ExceptionHandlerType.Catch"/>，这里只简单的对异常进行了pop操作，
        /// 如果需要获取异常信息，需要自行修改<see cref="ExceptionHandler.HandlerStart"/>，同时还需要修改<see cref="ExceptionHandler.CatchType"/>
        /// </summary>
        /// <remarks>
        /// 所谓的最外层就是外面最多只允许return操作，其他多余的操作都会被判定为非最外层异常处理器
        /// </remarks>
        public static ExceptionHandler GetOrBuildOutermostExceptionHandler(this MethodReference methodRef, ExceptionHandlerType handlerType)
        {
            var methodDef = methodRef.ToDefinition();

            var outerHandler = methodDef.GetOuterExceptionHandler();
            if (outerHandler != null && outerHandler.HandlerType == handlerType && IsOuterMostExceptionHandler(methodDef, outerHandler))
            {
                if (outerHandler.HandlerStart.OpCode.Code == Code.Endfinally)
                {
                    var endFianlly = outerHandler.HandlerStart;
                    var nop = Instruction.Create(OpCodes.Nop);
                    methodDef.Body.Instructions.InsertBefore(endFianlly, nop);
                    outerHandler.TryEnd = nop;
                    outerHandler.HandlerStart = nop;
                }
                return outerHandler;
            }

            var returnBlock = methodDef.MergeReturnToLeave();
            var tryStart = methodDef.GetFirstInstruction();
            var tryEnd = handlerType == ExceptionHandlerType.Catch ? Instruction.Create(OpCodes.Pop) : Instruction.Create(OpCodes.Nop);
            var handlerEnd = returnBlock[0];

            var instructions = methodDef.Body.Instructions;
            var lastInstruction = instructions.Last();
            if (lastInstruction.OpCode.Code != Code.Leave && lastInstruction.OpCode.Code != Code.Leave_S || lastInstruction.Operand is not Instruction instruction || instruction != handlerEnd)
            {
                instructions.Add(Instruction.Create(OpCodes.Leave, handlerEnd));
            }
            instructions.Add(tryEnd);
            if (handlerType == ExceptionHandlerType.Finally)
            {
                instructions.Add(Instruction.Create(OpCodes.Endfinally));
            }
            instructions.Add(returnBlock);

            var handler = new ExceptionHandler(handlerType)
            {
                TryStart = tryStart,
                TryEnd = tryEnd,
                HandlerStart = tryEnd,
                HandlerEnd = handlerEnd
            };
            methodDef.Body.ExceptionHandlers.Add(handler);

            methodDef.UpdateDebugInfoLastSequencePoint();
            methodDef.SkipExceptionHandlerBlockSequencePoint(handler);

            return handler;
        }

        private static bool IsOuterMostExceptionHandler(MethodDefinition methodDef, ExceptionHandler exceptionHandler)
        {
            if (exceptionHandler.HandlerEnd == null) return true;

            var resultLoaded = methodDef.ReturnType.IsVoid();
            var returned = false;
            var instruction = exceptionHandler.HandlerEnd;
            do
            {
                var code = instruction.OpCode.Code;
                if (code == Code.Nop || code == Code.Break) continue;
                if (instruction.IsLdloc())
                {
                    if (resultLoaded) break;
                    resultLoaded = true;
                    continue;
                }
                if (code == Code.Ret && resultLoaded) returned = true;
            } while ((instruction = instruction.Next) != null);

            return returned;
        }

        public static void UpdateDebugInfoLastSequencePoint(this MethodDefinition methodDef)
        {
            if (methodDef.DebugInformation == null || methodDef.DebugInformation.SequencePoints.Count == 0) return;

            var sequencePoints = methodDef.DebugInformation.SequencePoints;
            var lastSequencePoint = sequencePoints.Last();
            var lastInstruction = methodDef.Body.Instructions.Last();
            if (lastSequencePoint.Offset == lastInstruction.Offset) return;
            sequencePoints[sequencePoints.Count - 1] = new(lastInstruction, lastSequencePoint.Document)
            {
                StartLine = lastSequencePoint.StartLine,
                StartColumn = lastSequencePoint.StartColumn,
                EndLine = lastSequencePoint.EndLine,
                EndColumn = lastSequencePoint.EndColumn
            };
        }

        public static void SkipExceptionHandlerBlockSequencePoint(this MethodDefinition methodDef, ExceptionHandler exceptionHandler)
        {
            if (methodDef.DebugInformation == null || methodDef.DebugInformation.SequencePoints.Count == 0) return;

            var sequencePoints = methodDef.DebugInformation.SequencePoints;
            var lastSequencePoint = sequencePoints.Last();

            var index = 0;
            var instructions = methodDef.Body.Instructions;
            SequencePoint? beforeThis = null;
            foreach (var point in sequencePoints)
            {
                var pointOffset = _SequencePointGetOffset.Value(point);
                var pointInstruction = _InstructionOffsetGetInstruction.Value(pointOffset);
                Instruction? instruction = null;
                while (index < instructions.Count && (instruction = instructions[index++]) != pointInstruction)
                {
                    if (instruction == exceptionHandler.TryEnd)
                    {
                        beforeThis = point;
                        break;
                    }
                }
                if (beforeThis != null || index >= instructions.Count) break;
                if (instruction == exceptionHandler.TryEnd) return;
            }
            var sequencePoint = new SequencePoint(exceptionHandler.TryEnd, lastSequencePoint.Document)
            {
                StartLine = 0xFEEFEE,
                EndLine = 0xFEEFEE,
                StartColumn = 0,
                EndColumn = 0
            };
            if (beforeThis == null)
            {
                sequencePoints.Add(sequencePoint);
            }
            else
            {
                sequencePoints.InsertBefore(beforeThis, sequencePoint);
            }
        }

        #region Import
        public static MethodReference ImportInto(this MethodReference methodRef, BaseModuleWeaver moduleWeaver)
        {
            return moduleWeaver.Import(methodRef);
        }

        public static MethodReference ImportInto(this MethodReference methodRef, ModuleDefinition moduleDef)
        {
            return moduleDef.ImportReference(methodRef);
        }
        #endregion Import
    }
}