namespace MiniSharpCompiler
{
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    public partial class LLVMIRGenerationVisitor
    {
        /// <summary>
        /// 7.7.6
        /// TODO: implicit, explicit conversion support
        /// </summary>
        /// <param name="node"></param>
        public override void VisitCastExpression(CastExpressionSyntax node)
        {
            var fromType = this.semanticModel.GetTypeInfo(node.Expression).ConvertedType;
            var toType = this.semanticModel.GetTypeInfo(node.Type).ConvertedType;
            this.Push(node, this.Pop(node.Expression).Convert(this.builder, fromType, toType));
        }
    }
}