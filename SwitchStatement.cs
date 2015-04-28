namespace MiniSharpCompiler
{
    using System;
    using System.Collections.Generic;
    using LLVMSharp;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    public partial class LLVMIRGenerationVisitor
    {
        public override void VisitSwitchStatement(SwitchStatementSyntax node)
        {
            var @default = LLVM.AppendBasicBlock(this.function, "sw.default");
            var epilog = LLVM.AppendBasicBlock(this.function, "sw.epilog");
            bool defaultEpilogWritten = false;

            this.controlFlowStack.Push(new ControlFlowTarget(epilog, epilog));

            var governingType = this.semanticModel.GetTypeInfo(node.Expression);
            var governingLLVMType = governingType.LLVMTypeRef();
            var @switch = LLVM.BuildSwitch(this.builder, this.Pop(node.Expression), @default, (uint)node.Sections.Count);
            this.currentSwitchStatement = new Dictionary<object, LLVMBasicBlockRef>();

            foreach (var section in node.Sections)
            {
                var sectionLabels = section.Labels;
                LLVMBasicBlockRef bb = @default;

                bool isDefault = false;
                foreach (SwitchLabelSyntax label in sectionLabels)
                {
                    if (label.Keyword.IsKind(SyntaxKind.DefaultKeyword))
                    {
                        defaultEpilogWritten = true;
                        isDefault = true;
                    }
                }

                if (!isDefault)
                {
                    bb = LLVM.AppendBasicBlock(this.function, "sw.bb");
                }

                foreach (SwitchLabelSyntax label in sectionLabels)
                {
                    switch (label.Keyword.Kind())
                    {
                        case SyntaxKind.DefaultKeyword:
                            this.currentSwitchStatement.Add(defaultHash, bb);
                            break;
                        case SyntaxKind.CaseKeyword:
                            ulong constantValue;
                            var caseLabel = (CaseSwitchLabelSyntax) label;
                            var typeInfo = this.semanticModel.GetTypeInfo(caseLabel.Value);
                            object constantValueObject = this.semanticModel.GetConstantValue(caseLabel.Value).Value;
                            switch (typeInfo.Type.SpecialType)
                            {
                                case SpecialType.System_Boolean:
                                    constantValue = (bool)constantValueObject ? 1U : 0;
                                    break;
                                case SpecialType.System_Byte:
                                case SpecialType.System_Char:
                                case SpecialType.System_UInt16:
                                case SpecialType.System_UInt32:
                                case SpecialType.System_UInt64:
                                    constantValue = (ulong)constantValueObject;
                                    break;
                                case SpecialType.System_SByte:
                                case SpecialType.System_Int16:
                                case SpecialType.System_Int32:
                                    constantValue = (ulong)((int)(constantValueObject));
                                    break;
                                case SpecialType.System_Int64:
                                    constantValue = (ulong)((long) constantValueObject);
                                    break;
                                case SpecialType.System_String:
                                    throw new NotImplementedException("Switch on string type is not implemented");
                                default:
                                    throw new Exception("Unreachable");
                            }

                            LLVM.AddCase(@switch, LLVM.ConstInt(governingLLVMType, constantValue, governingType.IsSignExtended()), bb);
                            this.currentSwitchStatement.Add(constantValueObject, bb);
                            break;
                        default:
                            throw new Exception("Unreachable");
                    }
                }

                LLVM.PositionBuilderAtEnd(this.builder, bb);

                foreach (var statement in section.Statements)
                {
                    this.Visit(statement);
                }

                LLVM.BuildBr(this.builder, epilog);
            }

            this.currentSwitchStatement = null;

            if (!defaultEpilogWritten)
            {
                LLVM.PositionBuilderAtEnd(this.builder, @default);
                LLVM.BuildBr(this.builder, epilog);
            }

            LLVM.PositionBuilderAtEnd(this.builder, epilog);
            this.controlFlowStack.Pop();
        }
    }
}