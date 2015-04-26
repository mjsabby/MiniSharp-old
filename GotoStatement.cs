namespace MiniSharpCompiler
{
    using System;
    using LLVMSharp;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    public partial class LLVMIRGenerationVisitor
    {
        public override void VisitGotoStatement(GotoStatementSyntax node)
        {
            LLVMBasicBlockRef gotoBB;
            switch (node.CaseOrDefaultKeyword.Kind())
            {
                case SyntaxKind.DefaultKeyword:
                    gotoBB = this.currentSwitchStatement[defaultHash];
                    break;
                case SyntaxKind.CaseKeyword:
                    var constantValueObject = this.semanticModel.GetConstantValue(node.Expression).Value;
                    gotoBB = this.currentSwitchStatement[constantValueObject];
                    break;
                case SyntaxKind.None:
                    var identifier = ((IdentifierNameSyntax) node.Expression).Identifier.Text;
                    var basicBlocks = this.symbolTable.Peek().BasicBlocks;
                    if (!basicBlocks.TryGetValue(identifier, out gotoBB))
                    {
                        gotoBB = LLVM.AppendBasicBlock(this.function, identifier);
                        basicBlocks.Add(identifier, gotoBB);
                    }
                    break;
                default:
                    throw new Exception("Unreachable");
            }

            LLVM.BuildBr(this.builder, gotoBB);
            LLVM.PositionBuilderAtEnd(this.builder, LLVM.AppendBasicBlock(this.function, "UnreachableGoto"));
        }
    }
}