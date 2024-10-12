using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;

namespace Fody.Inspectors
{
    public class VisitStack
    {
        private readonly Stack<Instruction> _stack = [];
        private readonly HashSet<Instruction> _set = [];

        public bool IsEmpty => _stack.Count == 0 && _set.Count == 0;

        public bool TryAdd(Instruction instruction)
        {
            if (_set.Contains(instruction)) return false;

            _stack.Push(instruction);
            _set.Add(instruction);
            return true;
        }

        public void Clear()
        {
            _stack.Clear();
            _set.Clear();
        }

        public IDisposable Stash() => new Holder(this);

        private class Holder(VisitStack stack) : IDisposable
        {
            private readonly int _count = stack._stack.Count;

            public void Dispose()
            {
                while (stack._stack.Count > _count)
                {
                    var item = stack._stack.Pop();
                    stack._set.Remove(item);
                }
            }
        }
    }
}
