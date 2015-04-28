namespace MiniSharpCompiler
{
    using LLVMSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    public partial class LLVMIRGenerationVisitor
    {
        /// <summary>
        /// For every declaration, lookup the alloca that was allocated in the
        /// first basic block, and add it to the current symbol table.
        /// 
        /// For every variable initializer, build a store to its expression,
        /// and then push it on the value stack for equals binary expr to pick
        /// it up.
        /// </summary>
        public override void VisitVariableDeclaration(VariableDeclarationSyntax node)
        {
            var variables = node.Variables;
            foreach (VariableDeclaratorSyntax variable in variables)
            {
                LLVMValueRef alloca = this.symbolTable[variable];
                if (variable.Initializer != null)
                {
                    this.Push(node.Type, LLVM.BuildStore(this.builder, this.Pop(variable.Initializer.Value), alloca));
                }
            }
        }
    }
}