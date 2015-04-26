namespace MiniSharpCompiler
{
    using System;
    using System.Collections.Generic;
    using LLVMSharp;

    internal sealed class Environment
    {
        public Environment()
        {
            this.Locals = new Dictionary<string, Tuple<LLVMTypeRef, LLVMValueRef>>(StringComparer.OrdinalIgnoreCase);
            this.BasicBlocks = new Dictionary<string, LLVMBasicBlockRef>(StringComparer.OrdinalIgnoreCase);
        }

        public Dictionary<string, LLVMBasicBlockRef> BasicBlocks { get; private set; }

        public Dictionary<string, Tuple<LLVMTypeRef, LLVMValueRef>> Locals { get; private set; }
    }
}