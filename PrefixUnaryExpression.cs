﻿namespace MiniSharpCompiler
{
    using System;
    using LLVMSharp;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    public partial class LLVMIRGenerationVisitor
    {
        /// <summary>
        /// 7.7
        /// </summary>
        public override void VisitPrefixUnaryExpression(PrefixUnaryExpressionSyntax node)
        {
            SyntaxKind kind = node.Kind();
            TypeInfo typeInfo = this.semanticModel.GetTypeInfo(node.Operand);
            LLVMValueRef operand;

            switch (kind)
            {
                // 7.7.1
                case SyntaxKind.UnaryPlusExpression: // var a = +10;
                    switch (typeInfo.ConvertedType.SpecialType)
                    {
                        case SpecialType.System_Int32:
                        case SpecialType.System_UInt32:
                        case SpecialType.System_Int64:
                        case SpecialType.System_UInt64:
                        case SpecialType.System_Single:
                        case SpecialType.System_Double:
                        case SpecialType.System_Decimal:
                            operand = this.Pop(node.Operand);
                            break;
                        default:
                            throw new NotImplementedException("UnaryPlusExpression operator overloading is not yet implemented");
                    }
                    break;
                // 7.7.2
                case SyntaxKind.UnaryMinusExpression: // var a = -10;
                    switch (typeInfo.ConvertedType.SpecialType)
                    {
                        case SpecialType.System_Int32:
                        case SpecialType.System_Int64:
                            operand = LLVM.BuildNeg(this.builder, this.Pop(node.Operand), "neg");
                            break;
                        case SpecialType.System_Single:
                        case SpecialType.System_Double:
                        case SpecialType.System_Decimal:
                            operand = LLVM.BuildFNeg(this.builder, this.Pop(node.Operand), "fneg");
                            break;
                        case SpecialType.System_UInt32:
                        case SpecialType.System_UInt64:
                            if (node.Operand.IsKind(SyntaxKind.NumericLiteralExpression))
                            {
                                operand = LLVM.BuildNeg(this.builder, this.Pop(node.Operand), "neg");
                                break;
                            }
                            // TODO: UnaryMinusExpression rules from 7.7.2 should hard code int/long.MinValue here
                            throw new Exception("UnaryMinusExpression for uint32 and uint64 is only applicable for numerical literals");
                        default:
                            throw new NotImplementedException("UnaryMinusExpression operator overloading not yet implemented");
                    }
                    break;
                // 7.7.3
                case SyntaxKind.LogicalNotExpression: // var a = !true;
                    switch (typeInfo.ConvertedType.SpecialType)
                    {
                        case SpecialType.System_Boolean:
                            operand = LLVM.BuildNot(this.builder, this.Pop(node.Operand), "lnot");
                            break;
                        default:
                            throw new NotImplementedException("LogicalNotExpression operator overloading not yet implemented");
                    }
                    break;
                // 7.7.4
                case SyntaxKind.BitwiseNotExpression: // var a = ~10;
                    switch (typeInfo.ConvertedType.SpecialType)
                    {
                        case SpecialType.System_Int32:
                        case SpecialType.System_UInt32:
                        case SpecialType.System_Int64:
                        case SpecialType.System_UInt64:
                            operand = LLVM.BuildNot(this.builder, this.Pop(node.Operand), "not");
                            break;
                        default:
                            throw new NotImplementedException("BitwiseNotExpression operator overloading is not implemented");
                    }
                    break;
                case SyntaxKind.PreIncrementExpression:
                case SyntaxKind.PreDecrementExpression:
                {
                    LLVMOpcode opcode;
                    LLVMValueRef opValue;
                    LLVMValueRef operand2;

                    switch (typeInfo.ConvertedType.SpecialType)
                    {
                        case SpecialType.System_SByte:
                        case SpecialType.System_Byte:
                        case SpecialType.System_Int16:
                        case SpecialType.System_UInt16:
                        case SpecialType.System_Int32:
                        case SpecialType.System_UInt32:
                        case SpecialType.System_Int64:
                        case SpecialType.System_UInt64:
                        case SpecialType.System_Char:
                            opcode = kind == SyntaxKind.PreIncrementExpression ? LLVMOpcode.LLVMAdd : LLVMOpcode.LLVMSub;
                            opValue = LLVM.ConstInt(typeInfo.LLVMTypeRef(), 1, typeInfo.IsSignExtended());
                            break;
                        case SpecialType.System_Single:
                        case SpecialType.System_Double:
                        case SpecialType.System_Decimal:
                            opcode = kind == SyntaxKind.PreIncrementExpression ? LLVMOpcode.LLVMFAdd : LLVMOpcode.LLVMFSub;
                            opValue = LLVM.ConstReal(typeInfo.LLVMTypeRef(), 1);
                            break;
                        default:
                            throw new NotImplementedException("PreIncrementExpression/PreDecrementExpression operator overloading is not yet implemented");
                    }

                    switch (node.Operand.Kind())
                    {
                        case SyntaxKind.IdentifierName:
                            SyntaxNode syntaxNode = node.Operand.DeclaringSyntaxNode(this.semanticModel);
                            operand2 = this.symbolTable[syntaxNode];
                            break;
                        case SyntaxKind.SimpleMemberAccessExpression:
                            throw new NotImplementedException("PreIncrementExpression/PreDecrementExpression for SimpleMemberAccessExpression is not implemented");
                        case SyntaxKind.ElementAccessExpression:
                            throw new NotImplementedException("PreIncrementExpression/PreDecrementExpression for ElementAccessExpression is not implemented");
                        default:
                            throw new Exception("Unreachable");
                    }

                    var inc = LLVM.BuildBinOp(this.builder, opcode, this.Pop(node.Operand), opValue, "preop");
                    LLVM.BuildStore(this.builder, inc, operand2);
                    operand = inc;
                    break;
                }
                default:
                    throw new Exception("Unreachable");
            }

            this.Push(node, operand);
        }
    }
}