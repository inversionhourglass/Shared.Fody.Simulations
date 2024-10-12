using Fody.Simulations;
using Mono.Cecil;
using Mono.Cecil.Cil;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using System.Reflection;
using System;
using System.Text;

namespace Fody
{
    public abstract class SimulationModuleWeaver : BaseModuleWeaver
    {
        protected readonly bool _testRun;
        protected bool _debugMode;

        protected internal TypeReference _tValueTypeRef;
        protected internal TypeReference _tObjectRef;
        protected internal TypeReference _tVoidRef;
        protected internal TypeReference _tBooleanRef;
        protected internal TypeReference _tInt32Ref;
        protected internal TypeReference _tTypeRef;
        protected internal TypeReference _tMethodBaseRef;

        protected internal MethodReference _mGetTypeFromHandleRef;
        protected internal MethodReference _mGetMethodFromHandleRef;

        protected internal GlobalSimulations _simulations;

#if DEBUG
        protected internal MethodReference _methodDebuggerBreakRef;
#endif

        public SimulationModuleWeaver() : this(false) { }

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public SimulationModuleWeaver(bool testRun)
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        {
            _testRun = testRun;
        }

        public override void Execute()
        {
            _debugMode = IsDebugMode();

            try
            {
                if (!Enabled()) return;

                LoadBasicReference();
                _simulations = new(this);
                
                ExecuteInternal();
            }
            catch (FodyAggregateWeavingException e)
            {
                if (_testRun) throw;

                foreach (var ex in e.Exceptions)
                {
                    WriteException(ex, e.MethodDef);
                }
            }
            catch (FodyWeavingException e)
            {
                if (_testRun) throw;

                WriteException(e);
            }

            void WriteException(FodyWeavingException e, MethodDefinition? methodDef = null)
            {
                if (e.MethodDef != null)
                {
                    methodDef = e.MethodDef;
                }

                if (e.Message != null)
                {
                    if (methodDef == null)
                    {
                        WriteError(e.Message);
                    }
                    else
                    {
                        WriteError(e.Message, methodDef);
                    }
                }
            }
        }

        protected abstract void ExecuteInternal();

        protected virtual bool Enabled() => true;

        protected virtual void LoadBasicReference()
        {
            _tValueTypeRef = FindAndImportType(typeof(ValueType).FullName);
            _tObjectRef = FindAndImportType(typeof(object).FullName);
            _tVoidRef = FindAndImportType(typeof(void).FullName);
            _tInt32Ref = FindAndImportType(typeof(int).FullName);
            _tBooleanRef = FindAndImportType(typeof(bool).FullName);
            _tTypeRef = FindAndImportType(typeof(Type).FullName);
            _tMethodBaseRef = FindAndImportType(typeof(MethodBase).FullName);

            _mGetTypeFromHandleRef = _tTypeRef.GetMethod(Constants.METHOD_GetTypeFromHandle, false).ImportInto(this);
            _mGetMethodFromHandleRef = _tMethodBaseRef.GetMethod(false, x => x.Name == Constants.METHOD_GetMethodFromHandle && x.Parameters.Count == 2)!.ImportInto(this);

#if DEBUG
            var debuggerTypeRef = this.Import(FindTypeDefinition(typeof(Debugger).FullName));
            _methodDebuggerBreakRef = this.Import(debuggerTypeRef.GetMethod(false, x => x.IsStatic && x.Name == "Break")!);
#endif
        }

        protected internal TypeReference FindAndImportType(string fullName)
        {
            return ModuleDefinition.ImportReference(FindTypeDefinition(fullName));
        }

        protected virtual string GetConfigValue(string defaultValue, params string[] configKeys)
        {
            if (Config == null) return defaultValue;

            foreach (var configKey in configKeys)
            {
                var configAttribute = Config.Attributes(configKey).SingleOrDefault();
                if (configAttribute != null) return configAttribute.Value;
            }

            return defaultValue;
        }

        public override IEnumerable<string> GetAssembliesForScanning()
        {
            yield return "netstandard";
            yield return "mscorlib";
            yield return "System";
            yield return "System.Runtime";
            yield return "System.Core";
        }

        protected virtual bool IsDebugMode()
        {
            var debuggableAttribute = ModuleDefinition.Assembly.CustomAttributes.SingleOrDefault(x => x.Is(Constants.TYPE_DebuggableAttribute));
            if (debuggableAttribute == null) return false;
            if (debuggableAttribute.ConstructorArguments.Count == 1 && debuggableAttribute.ConstructorArguments[0].Value is int modes) return (modes & 0x100) != 0;
            if (debuggableAttribute.ConstructorArguments.Count == 2 && debuggableAttribute.ConstructorArguments[1].Value is bool isJITOptimizerDisabled) return isJITOptimizerDisabled;

            return false;
        }

        protected virtual void Debugger(IList<Instruction> instructions, MethodDefinition methodDef, string methodName)
        {
#if DEBUG
            if (methodDef.Name == methodName) instructions.Add(Instruction.Create(OpCodes.Call, _methodDebuggerBreakRef));
#endif
        }
    }
}
