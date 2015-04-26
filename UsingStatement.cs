namespace MiniSharpCompiler
{
    using System;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    public partial class LLVMIRGenerationVisitor
    {
        public override void VisitUsingStatement(UsingStatementSyntax node)
        {
            throw new Exception("MiniSharp does not support using statements");
        }
    }
}