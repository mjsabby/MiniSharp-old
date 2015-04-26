namespace MiniSharpCompiler
{
    using LLVMSharp;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    public partial class LLVMIRGenerationVisitor
    {
        /// <summary>
        /// 7.14 Conditional operator
        /// 
        /// Type Conversion    :
        /// GC Root            : N/A
        /// Sign Extension     : N/A
        /// Stack Balance      : +1
        /// </summary>
        public override void VisitConditionalExpression(ConditionalExpressionSyntax node)
        {
            LLVMBasicBlockRef condTrue = LLVM.AppendBasicBlock(this.function, "cond.true");
            LLVMBasicBlockRef condFalse = LLVM.AppendBasicBlock(this.function, "cond.false");
            LLVMBasicBlockRef condEnd = LLVM.AppendBasicBlock(this.function, "cond.end");

            LLVM.BuildCondBr(this.builder, this.Pop(node.Condition), condTrue, condFalse);

            // true case
            LLVM.PositionBuilderAtEnd(this.builder, condTrue);
            this.EnterScope();
            var trueValue = this.Pop(node.WhenTrue);
            this.LeaveScope();
            LLVM.BuildBr(this.builder, condEnd);

            condTrue = LLVM.GetInsertBlock(this.builder);

            // false case
            LLVM.PositionBuilderAtEnd(this.builder, condFalse);
            this.EnterScope();
            var falseValue = this.Pop(node.WhenFalse);
            this.LeaveScope();
            LLVM.BuildBr(this.builder, condEnd);

            condFalse = LLVM.GetInsertBlock(this.builder);

            // end
            LLVM.PositionBuilderAtEnd(this.builder, condEnd);
            var phi = LLVM.BuildPhi(this.builder, this.semanticModel.GetTypeInfo(node).LLVMTypeRef(), "cond");
            LLVM.AddIncoming(phi, out trueValue, out condTrue, 1);
            LLVM.AddIncoming(phi, out falseValue, out condFalse, 1);

            this.Push(node, phi);
        }
    }
}