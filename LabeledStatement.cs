namespace MiniSharpCompiler
{
    using LLVMSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    public partial class LLVMIRGenerationVisitor
    {
        public override void VisitLabeledStatement(LabeledStatementSyntax node)
        {
            var identifier = node.Identifier.Text;
            var basicBlocks = this.symbolTable.Peek().BasicBlocks;

            LLVMBasicBlockRef gotoBB;
            if (!basicBlocks.TryGetValue(identifier, out gotoBB))
            {
                gotoBB = LLVM.AppendBasicBlock(this.function, identifier);
                basicBlocks.Add(identifier, gotoBB);
            }

            LLVM.BuildBr(this.builder, gotoBB);

            LLVM.PositionBuilderAtEnd(this.builder, gotoBB);
            this.Visit(node.Statement);

            LLVMBasicBlockRef postGotoBB = LLVM.AppendBasicBlock(this.function, string.Concat("Post", identifier));
            LLVM.BuildBr(this.builder, postGotoBB);
            LLVM.PositionBuilderAtEnd(this.builder, postGotoBB);
        }
    }
}