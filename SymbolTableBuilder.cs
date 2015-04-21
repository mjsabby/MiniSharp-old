namespace MiniSharpCompiler
{
    using System;
    using System.Collections.Generic;
    using LLVMSharp;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    /// <summary>
    /// Symbol Table Builder
    /// </summary>
    public class SymbolTableBuilder : CSharpSyntaxWalker
    {
        private readonly SemanticModel model;

        private readonly LLVMBuilderRef builder;

        public SymbolTableBuilder(SemanticModel model, LLVMBuilderRef builder)
        {
            if (model == null)
            {
                throw new ArgumentNullException("model");
            }

            this.model = model;
            this.builder = builder;
            this.SymbolTable = new Dictionary<VariableDeclaratorSyntax, LLVMValueRef>();
        }

        public Dictionary<VariableDeclaratorSyntax, LLVMValueRef> SymbolTable;

        public override void VisitVariableDeclaration(VariableDeclarationSyntax node)
        {
            var type = this.model.GetTypeInfo(node.Type);
            var llvmType = type.LLVMTypeRef();

            foreach (var variable in node.Variables)
            {
                var value = LLVM.BuildAlloca(this.builder, llvmType, variable.Identifier.Text);
                this.SymbolTable.Add(variable, value);
            }
        }
    }
}