namespace MiniSharpCompiler
{
    using System;
    using LLVMSharp;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    public partial class LLVMIRGenerationVisitor
    {
        /// <summary>
        /// Variables, fields, method names
        /// </summary>
        public override void VisitIdentifierName(IdentifierNameSyntax node)
        {
            SyntaxNode syntaxNode = node.DeclaringSyntaxNode(this.semanticModel);

            if (syntaxNode != null)
            {
                LLVMValueRef operand;

                // if we call a method we've not yet seen ...
                if (!this.symbolTable.TryGetValue(syntaxNode, out operand))
                {
                    throw new NotImplementedException("methods and classes not implemented yet");
                }

                if (node.Parent is AssignmentExpressionSyntax)
                {
                    this.valueStack.Push(operand);
                }
                else
                {
                    this.Push(node, LLVM.BuildLoad(this.builder, operand, string.Empty));
                }
            }
        }
    }
}