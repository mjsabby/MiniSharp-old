using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LLVMSharp;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MiniSharpCompiler
{
	public partial class LLVMIRGenerationVisitor
	{
		/// <summary>
		/// Non variable declaration assignments
		/// TODO: requires type conversion
		/// </summary>
		public override void VisitAssignmentExpression(AssignmentExpressionSyntax node)
		{
			if (node.Kind() != SyntaxKind.SimpleAssignmentExpression)
			{
				throw new Exception("only simple expressions are supported");
			}

			var currentSymbolTable = this.symbolTable.Peek().Locals;
			var id = (IdentifierNameSyntax)node.Left;
			var symbol = currentSymbolTable[id.Identifier.Text].Item2;

			this.valueStack.Push(LLVM.BuildStore(this.builder, this.Pop(node.Right), symbol));
		}
	}
}
