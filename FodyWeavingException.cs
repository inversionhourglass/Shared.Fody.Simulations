using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.Text;

namespace Fody
{
    public class FodyWeavingException : Exception
    {
        public FodyWeavingException() : base() { }

        public FodyWeavingException(string message) : this(message, null, null) { }

        public FodyWeavingException(string message, MethodDefinition methodDef) : this(message, null, methodDef)
        {
        }

        public FodyWeavingException(string message, Instruction instruction) : this(message, instruction, null)
        {

        }

        public FodyWeavingException(string message, Instruction? instruction, MethodDefinition? methodDef)
        {
            Instruction = instruction;
            MethodDef = methodDef;

            var builder = new StringBuilder();
            if (methodDef != null)
            {
                builder.Append($"[{methodDef}] ");
            }
            if (instruction != null)
            {
                builder.Append($"[{instruction}] ");
            }
            builder.Append(message);

            Message = builder.ToString();
        }

        public override string? Message { get; }

        public virtual Instruction? Instruction { get; set; }

        public virtual MethodDefinition? MethodDef { get; set; }
    }
}
