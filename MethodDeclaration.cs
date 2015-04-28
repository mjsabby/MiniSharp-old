namespace MiniSharpCompiler
{
    using System;
    using LLVMSharp;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    public partial class LLVMIRGenerationVisitor
    {
        public override void VisitMethodDeclaration(MethodDeclarationSyntax node)
        {
            var parameters = node.ParameterList.Parameters;
            var paramCount = (uint)parameters.Count;

            LLVMTypeRef[] paramTypesArr = new LLVMTypeRef[Math.Max(paramCount, 1)]; // always need 1 for deref
            {
                uint index = 0;
                foreach (var parameter in parameters)
                {
                    paramTypesArr[index] = this.semanticModel.GetTypeInfo(parameter.Type).LLVMTypeRef();
                }
            }

            var returnType = this.semanticModel.GetTypeInfo(node.ReturnType).LLVMTypeRef();
            var functionType = LLVM.FunctionType(returnType, out paramTypesArr[0], paramCount, False);
            this.function = LLVM.AddFunction(this.module, node.Identifier.Text, functionType);
            this.symbolTable.Add(node, this.function); // add to symbol table

            var body = node.Body;

            // extern methods
            if (body == null)
            {
                return;
            }

            LLVM.PositionBuilderAtEnd(this.builder, LLVM.AppendBasicBlock(this.function, "entry"));

            {
                uint index = 0;
                foreach (var parameter in parameters)
                {
                    var llvmParam = LLVM.GetParam(this.function, index);
                    var alloca = LLVM.BuildAlloca(this.builder, paramTypesArr[index], parameter.Identifier.Text);
                    LLVM.BuildStore(this.builder, llvmParam, alloca);
                    this.symbolTable.Add(parameter, llvmParam);
                    // TODO: this.MarkGCRoot(llvmParam, tuple.Item2);
                }
            }

            var localVariableVisitor = new VariableDeclarationVisitor(this.semanticModel, this.builder);
            localVariableVisitor.Visit(node);
            var variables = localVariableVisitor.Variables;

            foreach (var entry in variables)
            {
                this.symbolTable.Add(entry.Key, entry.Value);
                // TODO: this.MarkGCRoot(entry.Value, this.semanticModel.GetTypeInfo(parent.Type));
            }

            this.Visit(body);

            if (this.semanticModel.GetTypeInfo(node.ReturnType).Type.SpecialType == SpecialType.System_Void)
            {
                LLVM.BuildRetVoid(this.builder);
            }
            else
            {
                LLVM.BuildRet(this.builder, LLVM.ConstNull(returnType));
            }
        }
    }
}