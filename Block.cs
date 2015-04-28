namespace MiniSharpCompiler
{
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    public partial class LLVMIRGenerationVisitor
    {
        /// <summary>
        /// Block statement
        /// </summary>
        public override void VisitBlock(BlockSyntax node)
        {
            this.EnterScope();
            base.VisitBlock(node);

            // LLVM requires nulling of variable for gcroot
            //foreach (var symbol in currentSymbolTable)
            {
                // TODO: LLVM.BuildStore(this.builder, LLVM.ConstNull(symbol.Value.Item1), symbol.Value.Item2);
            }

            this.LeaveScope();
        }
    }
}