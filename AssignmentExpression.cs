namespace MiniSharpCompiler
{
    using System;
    using LLVMSharp;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    public partial class LLVMIRGenerationVisitor
    {
        public override void VisitAssignmentExpression(AssignmentExpressionSyntax node)
        {
            SyntaxKind kind = node.Kind();
            this.Visit(node.Left);
            var lvalue = this.OnlyPop(node.Left);

            this.Visit(node.Right);
            var rvalue = this.OnlyPop(node.Right);

            this.valueStack.Push(rvalue);
            this.valueStack.Push(LLVM.BuildLoad(this.builder, lvalue, string.Empty));

            switch (kind)
            {
                case SyntaxKind.SimpleAssignmentExpression:
                    this.valueStack.Pop();
                    break;
                case SyntaxKind.AddAssignmentExpression:
                    this.AddExpression(node, node.Left, node.Right, visit: false);
                    break;
                case SyntaxKind.SubtractAssignmentExpression:
                    this.SubExpression(node, node.Left, node.Right, visit: false);
                    break;
                case SyntaxKind.MultiplyAssignmentExpression:
                    this.MulExpression(node, node.Left, node.Right, visit: false);
                    break;
                case SyntaxKind.DivideAssignmentExpression:
                    this.DivExpression(node, node.Left, node.Right, visit: false);
                    break;
                case SyntaxKind.ModuloAssignmentExpression:
                    this.ModExpression(node, node.Left, node.Right, visit: false);
                    break;
                case SyntaxKind.ExclusiveOrAssignmentExpression:
                    this.XorExpression(node, node.Left, node.Right, visit: false);
                    break;
                case SyntaxKind.AndAssignmentExpression:
                    this.BitAndExpression(node, node.Left, node.Right, visit: false);
                    break;
                case SyntaxKind.OrAssignmentExpression:
                    this.BitOrExpression(node, node.Left, node.Right, visit: false);
                    break;
                case SyntaxKind.LeftShiftAssignmentExpression:
                    this.ShlExpression(node, node.Left, node.Right, visit: false);
                    break;
                case SyntaxKind.RightShiftAssignmentExpression:
                    this.ShrExpression(node, node.Left, node.Right, visit: false);
                    break;
                default:
                    throw new Exception("Unreachable");
            }

            rvalue = this.OnlyPop(node.Right);

            switch (node.Left.Kind())
            {
                case SyntaxKind.IdentifierName:
                    LLVM.BuildStore(this.builder, rvalue, lvalue);
                    break;
                case SyntaxKind.ElementAccessExpression:
                    throw new NotImplementedException("ElementAccessExpression support is not yet implemented");
                case SyntaxKind.SimpleMemberAccessExpression:
                    throw new NotImplementedException("SimpleMemberAccessExpression support is not yet implemented");
                default:
                    throw new Exception("Unreachable");
            }
        }
    }
}