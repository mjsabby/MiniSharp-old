namespace MiniSharpCompiler
{
    using System.Collections.Generic;
    using LLVMSharp;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    public partial class LLVMIRGenerationVisitor : CSharpSyntaxWalker
    {
        private static readonly LLVMBool False = new LLVMBool(0);

        private static readonly LLVMBool True = new LLVMBool(1);

        private static readonly object defaultHash = new object(); // global state

        private readonly LLVMBuilderRef builder;

        private readonly SemanticModel semanticModel;

        private readonly Stack<LLVMValueRef> valueStack;

        private readonly LLVMModuleRef module;

        private readonly Dictionary<SyntaxNode, LLVMValueRef> symbolTable = new Dictionary<SyntaxNode, LLVMValueRef>();

        private readonly Stack<Dictionary<string, LLVMBasicBlockRef>> labels = new Stack<Dictionary<string, LLVMBasicBlockRef>>();
        
        private readonly Stack<ControlFlowTarget> controlFlowStack = new Stack<ControlFlowTarget>();

        private Dictionary<object, LLVMBasicBlockRef> currentSwitchStatement;

        private LLVMValueRef function;

        public LLVMIRGenerationVisitor(SemanticModel model, LLVMModuleRef module, LLVMBuilderRef builder, Stack<LLVMValueRef> valueStack)
        {
            this.semanticModel = model;
            this.builder = builder;
            this.module = module;
            this.valueStack = valueStack;
        }

        public LLVMValueRef Function => this.function;

        // More information can be found: http://llvm.org/docs/GarbageCollection.html#llvm-ir-features
        private void MarkGCRoot(LLVMValueRef value, TypeInfo type)
        {
            var specialType = type.Type.SpecialType;
            if (type.Type.IsReferenceType || specialType == SpecialType.System_Array)
            {
                var args = new LLVMValueRef[2];
                args[0] = LLVM.BuildBitCast(this.builder, value, LLVM.PointerType(LLVM.PointerType(LLVM.Int8Type(), 0), 0), "gc");
                args[1] = LLVM.ConstNull(LLVM.PointerType(LLVM.Int8Type(), 0));
                LLVM.BuildCall(this.builder, this.function, out args[0], 2, "llvm.gcroot");
            }
        }

        private void EnterScope()
        {
            Dictionary<string, LLVMBasicBlockRef> prev = null;
            Dictionary<string, LLVMBasicBlockRef> curr = new Dictionary<string, LLVMBasicBlockRef>();

            // previous scoped labels
            if (this.labels.Count > 0)
            {
                prev = this.labels.Peek();
            }

            // copy to current label set
            if (prev != null)
            {
                foreach (var v in prev)
                {
                    curr.Add(v.Key, v.Value);
                }
            }

            // push current scope
            this.labels.Push(curr);
        }

        private void LeaveScope()
        {
            this.labels.Pop();
        }

        private LLVMValueRef Pop(ExpressionSyntax node)
        {
            this.Visit(node);
            return this.valueStack.Pop().Convert(this.builder, this.semanticModel.GetTypeInfo(node));
        }

        private LLVMValueRef OnlyPop(ExpressionSyntax node)
        {
            return this.valueStack.Pop().Convert(this.builder, this.semanticModel.GetTypeInfo(node));
        }

        private void Push(ExpressionSyntax node, LLVMValueRef value)
        {
            this.valueStack.Push(value.Convert(this.builder, this.semanticModel.GetTypeInfo(node)));
        }

        private sealed class ControlFlowTarget
        {
            public ControlFlowTarget(LLVMBasicBlockRef condition, LLVMBasicBlockRef end)
            {
                this.Condition = condition;
                this.End = end;
            }

            public LLVMBasicBlockRef Condition { get; }

            public LLVMBasicBlockRef End { get; }
        }
    }
}