namespace MiniSharpCompiler
{
    using System;
    using System.Collections.Generic;
    using LLVMSharp;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal sealed class VariableDeclarationVisitor : CSharpSyntaxWalker
    {
        private readonly SemanticModel model;

        private readonly LLVMBuilderRef builder;

        public VariableDeclarationVisitor(SemanticModel model, LLVMBuilderRef builder)
        {
            if (model == null)
            {
                throw new ArgumentNullException(nameof(model));
            }

            this.model = model;
            this.builder = builder;
            this.Variables = new Dictionary<VariableDeclaratorSyntax, LLVMValueRef>();
        }

        public Dictionary<VariableDeclaratorSyntax, LLVMValueRef> Variables;

        public override void VisitVariableDeclaration(VariableDeclarationSyntax node)
        {
            var llvmType = this.model.GetTypeInfo(node.Type).LLVMTypeRef();
            var variables = node.Variables;

            foreach (var variable in variables)
            {
                var value = LLVM.BuildAlloca(this.builder, llvmType, variable.Identifier.Text);
                this.Variables.Add(variable, value);
            }
        }
    }
}