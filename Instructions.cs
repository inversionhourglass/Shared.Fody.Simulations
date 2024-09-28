using Mono.Cecil;
using Mono.Cecil.Cil;
using System.Collections;
using System.Collections.Generic;

namespace Fody
{
    public sealed class Instructions : IList<Instruction>
    {
        private readonly IList<Instruction> _instructions;

        public Instructions(Instruction instruction)
        {
            _instructions = [instruction];
        }

        public Instructions(IList<Instruction> instructions)
        {
            _instructions = instructions;
        }

        public Instruction this[int index]
        {
            get => _instructions[index];
            set => _instructions[index] = value;
        }

        public int Count => _instructions.Count;

        public bool IsReadOnly => _instructions.IsReadOnly;

        public void Add(Instruction item) => _instructions.Add(item);

        public void Clear() => _instructions.Clear();

        public bool Contains(Instruction item) => _instructions.Contains(item);

        public void CopyTo(Instruction[] array, int arrayIndex) => _instructions.CopyTo(array, arrayIndex);

        public IEnumerator<Instruction> GetEnumerator() => _instructions.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => _instructions.GetEnumerator();

        public int IndexOf(Instruction item) => _instructions.IndexOf(item);

        public void Insert(int index, Instruction item) => _instructions.Insert(index, item);

        public bool Remove(Instruction item) => _instructions.Remove(item);

        public void RemoveAt(int index) => _instructions.RemoveAt(index);

        public Instructions Clone()
        {
            var instructions = new List<Instruction>();
            foreach (Instruction item in this)
            {
                instructions.Add(item.Clone());
            }

            return new(instructions);
        }
    }
}
