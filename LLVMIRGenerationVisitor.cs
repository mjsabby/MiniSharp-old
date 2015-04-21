namespace MiniSharpCompiler
{
    using System;
    using System.Collections.Generic;
	using System.Diagnostics;
	using LLVMSharp;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

	public partial class LLVMIRGenerationVisitor : CSharpSyntaxWalker
	{
        private static readonly LLVMBool False = new LLVMBool(0);

        private static readonly LLVMBool True = new LLVMBool(1);

		private readonly LLVMBuilderRef builder;

	    private readonly SemanticModel semanticModel;

	    private readonly Stack<LLVMValueRef> valueStack;

	    private readonly LLVMModuleRef module;

	    private readonly Stack<Environment> symbolTable = new Stack<Environment>();

        private readonly Stack<ControlFlowTarget> controlFlowStack = new Stack<ControlFlowTarget>();

	    private Dictionary<VariableDeclaratorSyntax, LLVMValueRef> allocaTable;

	    private LLVMValueRef function;

		public LLVMIRGenerationVisitor(SemanticModel model, LLVMModuleRef module, LLVMBuilderRef builder, Stack<LLVMValueRef> valueStack)
		{
		    this.semanticModel = model;
			this.builder = builder;
		    this.module = module;
		    this.valueStack = valueStack;
		}

        public LLVMValueRef Function { get { return this.function; } }

	    public override void VisitMethodDeclaration(MethodDeclarationSyntax node)
	    {
            var body = node.Body;
            if (body == null)
            {
                return;
            }

            List<Tuple<string, TypeInfo, LLVMTypeRef>> paramTuples = new List<Tuple<string, TypeInfo, LLVMTypeRef>>();

	        var parameterList = node.ParameterList;
	        if (parameterList != null)
	        {
                this.symbolTable.Push(new Environment());

	            var parameters = parameterList.Parameters;
	            foreach (var parameter in parameters)
	            {
	                var type = this.semanticModel.GetTypeInfo(parameter.Type);
                    paramTuples.Add(new Tuple<string, TypeInfo, LLVMTypeRef>(parameter.Identifier.Text, type, type.LLVMTypeRef()));
	            }
	        }

	        uint paramCount = (uint)paramTuples.Count;
            var paramTypesArr = new LLVMTypeRef[Math.Max(paramCount, 1)]; // always need 1 for deref
	        var returnType = this.semanticModel.GetTypeInfo(node.ReturnType);

	        for(int i = 0; i < paramCount; ++i)
	        {
	            paramTypesArr[i] = paramTuples[i].Item3;
	        }

            this.function = LLVM.AddFunction(this.module, node.Identifier.Text, LLVM.FunctionType(returnType.LLVMTypeRef(), out paramTypesArr[0], paramCount, new LLVMBool(0)));

            //LLVM.SetGC(this.function, "shadow-stack");
            LLVM.PositionBuilderAtEnd(this.builder, LLVM.AppendBasicBlock(this.function, "entry"));

            uint index = 0;
	        var currentSymbolTable = this.symbolTable.Peek().Locals;
            foreach (var tuple in paramTuples)
            {
                var llvmParam = LLVM.GetParam(this.function, index);
                var alloca = LLVM.BuildAlloca(this.builder, tuple.Item3, tuple.Item1);

                currentSymbolTable.Add(tuple.Item1, new Tuple<LLVMTypeRef, LLVMValueRef>(tuple.Item3, alloca));

                LLVM.BuildStore(this.builder, llvmParam, alloca);
                this.MarkGCRoot(llvmParam, tuple.Item2);
                index++;
            }

	        var allocaBuilder = new SymbolTableBuilder(this.semanticModel, this.builder);
            allocaBuilder.Visit(node);
	        this.allocaTable = allocaBuilder.SymbolTable;

	        foreach (var entry in this.allocaTable)
	        {
	            var parent = entry.Key.Parent as VariableDeclarationSyntax;
	            this.MarkGCRoot(entry.Value, this.semanticModel.GetTypeInfo(parent.Type));
	        }

	        this.Visit(body);

	        if (returnType.Type.SpecialType == SpecialType.System_Void)
	        {
	            LLVM.BuildRetVoid(this.builder);
	        }
	        else
	        {
	            LLVM.BuildRet(this.builder, LLVM.ConstNull(returnType.LLVMTypeRef()));
	        }
	    }

	    /// <summary>
        /// Block statement
        /// </summary>
	    public override void VisitBlock(BlockSyntax node)
	    {
	        this.EnterScope();
            base.VisitBlock(node);

            // LLVM requires nulling of variable for gcroot
            //foreach (var symbol in currentSymbolTable)
            {
                // LLVM.BuildStore(this.builder, LLVM.ConstNull(symbol.Value.Item1), symbol.Value.Item2);
            }

	        this.LeaveScope();
	    }

		/// <summary>
		/// For every declaration, lookup the alloca that was allocated in the
		/// first basic block, and add it to the current symbol table.
		/// 
		/// For every variable initializer, build a store to its expression,
		/// and then push it on the value stack for equals binary expr to pick
		/// it up.
		/// 
		/// TODO: needs type conversion
		/// </summary>
		public override void VisitVariableDeclaration(VariableDeclarationSyntax node)
	    {
            var currentSymbolTable = this.symbolTable.Peek().Locals;
	        var type = this.semanticModel.GetTypeInfo(node.Type);
	        var llvmType = type.LLVMTypeRef();

            int count = node.Variables.Count;
            for (int i = 0; i < count; ++i)
            {
                VariableDeclaratorSyntax variable = node.Variables[i];
                LLVMValueRef alloca = this.allocaTable[variable];
                currentSymbolTable.Add(variable.Identifier.Text, new Tuple<LLVMTypeRef, LLVMValueRef>(llvmType, alloca));

                if (variable.Initializer != null)
                {
                    this.valueStack.Push(LLVM.BuildStore(this.builder, this.Pop(variable.Initializer.Value), alloca));
                }
            }
	    }

        /// <summary>
        /// Variables, fields, method names
        /// </summary>
        /// <param name="node"></param>
	    public override void VisitIdentifierName(IdentifierNameSyntax node)
	    {
            var currentSymbolTable = this.symbolTable.Peek().Locals;
            var symbol = currentSymbolTable[node.Identifier.Text].Item2;
	        this.valueStack.Push(LLVM.BuildLoad(this.builder, symbol, string.Empty));
	    }

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
			var phi = LLVM.BuildPhi(this.builder, this.semanticModel.GetTypeInfo(node.WhenTrue).LLVMTypeRef(), "cond");
			LLVM.AddIncoming(phi, out trueValue, out condTrue, 1);
			LLVM.AddIncoming(phi, out falseValue, out condFalse, 1);

			this.valueStack.Push(phi);
		}

		/// <summary>
		/// Binary expression
		/// 
		/// TODO: Requires type conversion
		/// </summary>
		public override void VisitBinaryExpression(BinaryExpressionSyntax node)
		{
			var leftType = this.semanticModel.GetTypeInfo(node);
			var rightType = this.semanticModel.GetTypeInfo(node);
			SyntaxKind kind = node.Kind();
	        switch (kind)
	        {
	            case SyntaxKind.AddExpression:
	                this.AddExpression(node);
	                break;
	            case SyntaxKind.DivideExpression:
	                this.DivExpression(node);
	                break;
	            case SyntaxKind.ModuloExpression:
	                this.ModExpression(node);
	                break;
	            case SyntaxKind.MultiplyExpression:
	                this.MulExpression(node);
	                break;
	            case SyntaxKind.SubtractExpression:
	                this.SubExpression(node);
	                break;
	            case SyntaxKind.BitwiseAndExpression:
	                this.BitAndExpression(node);
	                break;
	            case SyntaxKind.BitwiseOrExpression:
	                this.BitOrExpression(node);
	                break;
	            case SyntaxKind.ExclusiveOrExpression:
	                this.XorExpression(node);
	                break;
	            case SyntaxKind.LogicalAndExpression:
					Debug.Assert(leftType.Type.SpecialType == SpecialType.System_Boolean);
					Debug.Assert(rightType.Type.SpecialType == SpecialType.System_Boolean);
					this.ConditionalAndExpression(node);
	                break;
	            case SyntaxKind.LogicalOrExpression:
					Debug.Assert(leftType.Type.SpecialType == SpecialType.System_Boolean);
					Debug.Assert(rightType.Type.SpecialType == SpecialType.System_Boolean);
					this.ConditionalOrExpression(node);
	                break;
	            case SyntaxKind.EqualsExpression:
                case SyntaxKind.NotEqualsExpression:
	            case SyntaxKind.LessThanExpression:
	            case SyntaxKind.LessThanOrEqualExpression:
	            case SyntaxKind.GreaterThanExpression:
	            case SyntaxKind.GreaterThanOrEqualExpression:
	                this.RelEqExpression(node);
	                break;
                default:
	                throw new Exception("Unreachable");
	        }
	    }


		/// <summary>
		/// TODO: needs type conversion
		/// </summary>
		/// <param name="node"></param>
		public override void VisitArgument(ArgumentSyntax node)
		{
			base.VisitArgument(node);
		}

		/// <summary>
		/// 
		/// TODO: requires type conversion
		/// </summary>
		/// <param name="node"></param>
		public override void VisitCastExpression(CastExpressionSyntax node)
		{
			base.VisitCastExpression(node);
		}

	    private void RelEqExpression(BinaryExpressionSyntax node)
	    {
	        var left = this.semanticModel.GetTypeInfo(node.Left);
	        var right = this.semanticModel.GetTypeInfo(node.Right);

	        var leftType = left.Type;
	        var rightType = right.Type;

	        if (!leftType.Equals(rightType))
	        {
	            throw new Exception("Type mismatch exception");
	        }

	        if (leftType.SpecialType == SpecialType.System_Double || leftType.SpecialType == SpecialType.System_Single)
            {
                throw new Exception("Single/Double relational expression");
            }

            this.valueStack.Push(LLVM.BuildICmp(this.builder, TypeSystem.IntPredicate(node.Kind()), this.Pop(node.Left), this.Pop(node.Right), "cmptmp"));
	    }

	    private void ConditionalAndExpression(BinaryExpressionSyntax node)
	    {
			LLVMBasicBlockRef incoming = LLVM.GetInsertBlock(this.builder);
			LLVMBasicBlockRef lorRhs = LLVM.AppendBasicBlock(this.function, "lor.rhs");
			LLVMBasicBlockRef lorEnd = LLVM.AppendBasicBlock(this.function, "lor.end");

			var lhs = this.Pop(node.Left);

			LLVM.BuildCondBr(this.builder, lhs, lorEnd, lorRhs);

			LLVM.PositionBuilderAtEnd(this.builder, lorRhs);
			var rhs = this.Pop(node.Right);
			LLVM.BuildBr(this.builder, lorEnd);

			LLVM.PositionBuilderAtEnd(this.builder, lorEnd);
			var phi = LLVM.BuildPhi(this.builder, LLVM.Int1Type(), "phi");
			var falseValue = LLVM.ConstInt(LLVM.Int1Type(), 1, False);

			LLVM.AddIncoming(phi, out falseValue, out incoming, 1);
			LLVM.AddIncoming(phi, out rhs, out lorRhs, 1);

			LLVM.PositionBuilderAtEnd(this.builder, lorEnd);

			this.valueStack.Push(phi);
		}

        private void ConditionalOrExpression(BinaryExpressionSyntax node)
        {
	        LLVMBasicBlockRef incoming = LLVM.GetInsertBlock(this.builder);
            LLVMBasicBlockRef lorRhs = LLVM.AppendBasicBlock(this.function, "lor.rhs");
            LLVMBasicBlockRef lorEnd = LLVM.AppendBasicBlock(this.function, "lor.end");
			
	        var lhs = this.Pop(node.Left);

	        LLVM.BuildCondBr(this.builder, lhs, lorEnd, lorRhs);

			LLVM.PositionBuilderAtEnd(this.builder, lorRhs);
	        var rhs = this.Pop(node.Right);
	        LLVM.BuildBr(this.builder, lorEnd);

			LLVM.PositionBuilderAtEnd(this.builder, lorEnd);
	        var phi = LLVM.BuildPhi(this.builder, LLVM.Int1Type(), "phi");
	        var trueValue = LLVM.ConstInt(LLVM.Int1Type(), 1, False);

			LLVM.AddIncoming(phi, out trueValue, out incoming, 1);
			LLVM.AddIncoming(phi, out rhs, out lorRhs, 1);

            LLVM.PositionBuilderAtEnd(this.builder, lorEnd);

            this.valueStack.Push(phi);
        }

	    private void NumericalExpressionCheck(BinaryExpressionSyntax node)
	    {
	        var left = this.semanticModel.GetTypeInfo(node.Left);
	        var right = this.semanticModel.GetTypeInfo(node.Right);

	        var leftType = left.Type;
	        var rightType = right.Type;

	        if (!leftType.Equals(rightType) && (leftType.SpecialType != SpecialType.System_Int16 || leftType.SpecialType != SpecialType.System_Int32 || leftType.SpecialType != SpecialType.System_Int64))
	        {
	            throw new Exception("Type mismatch exception");
	        }
	    }

	    private void AddExpression(BinaryExpressionSyntax node)
	    {
	        this.BinOp(node, LLVMOpcode.LLVMAdd, "add");
	    }

        private void SubExpression(BinaryExpressionSyntax node)
        {
            this.BinOp(node, LLVMOpcode.LLVMSub, "sub");
        }

        private void MulExpression(BinaryExpressionSyntax node)
        {
            this.BinOp(node, LLVMOpcode.LLVMMul, "mul");
        }

        private void DivExpression(BinaryExpressionSyntax node)
        {
            this.BinOp(node, LLVMOpcode.LLVMSDiv, "div");
        }

        private void ModExpression(BinaryExpressionSyntax node)
        {
            this.BinOp(node, LLVMOpcode.LLVMSRem, "rem");
        }

        private void BitAndExpression(BinaryExpressionSyntax node)
        {
            this.BinOp(node, LLVMOpcode.LLVMAnd, "and");
        }

        private void BitOrExpression(BinaryExpressionSyntax node)
        {
            this.BinOp(node, LLVMOpcode.LLVMOr, "or");
        }

        private void XorExpression(BinaryExpressionSyntax node)
        {
            this.BinOp(node, LLVMOpcode.LLVMXor, "xor");
        }

	    private void BinOp(BinaryExpressionSyntax node, LLVMOpcode opcode, string name)
	    {
	        this.NumericalExpressionCheck(node);
            this.valueStack.Push(LLVM.BuildBinOp(builder, opcode, this.Pop(node.Left), this.Pop(node.Right), name));
	    }

        // More information can be found: http://llvm.org/docs/GarbageCollection.html#llvm-ir-features
	    private void MarkGCRoot(LLVMValueRef value, TypeInfo type)
	    {
	        var specialType = type.Type.SpecialType;
	        if (type.Type.IsReferenceType || specialType == SpecialType.System_Array)
	        {
	            var args = new LLVMValueRef[2];
                args[0] = LLVM.BuildBitCast(this.builder, value, LLVM.PointerType(LLVM.PointerType(LLVM.Int8Type(), 0), 0), "gc");
                args[1] = LLVM.ConstNull(LLVM.PointerType(LLVM.Int8Type(), 0));
	            LLVM.BuildCall(this.builder, this.function, out args[0], 2, "llvm.gcroot");
	        }
	    }

	    private void EnterScope()
	    {
			var env = new Environment();
			Dictionary<string, Tuple<LLVMTypeRef, LLVMValueRef>> previousSymbolTable = null;
		    Dictionary<string, Tuple<LLVMTypeRef, LLVMValueRef>> currentSymbolTable = env.Locals;

			// previous scoped variables
			if (this.symbolTable.Count > 0)
            {
                previousSymbolTable = this.symbolTable.Peek().Locals;
            }

            // copy to current scope
            if (previousSymbolTable != null)
            {
                foreach (var v in previousSymbolTable)
                {
                    currentSymbolTable.Add(v.Key, v.Value);
                }
            }

            // push current scope
            this.symbolTable.Push(env);
	    }

	    private void LeaveScope()
	    {
	        this.symbolTable.Pop();
	    }

        private LLVMValueRef Pop(ExpressionSyntax node)
        {
            this.Visit(node);
            return this.valueStack.Pop().Convert(this.builder, this.semanticModel.GetTypeInfo(node));
        }

		private void Push(ExpressionSyntax node, LLVMValueRef value)
		{
			this.valueStack.Push(value.Convert(this.builder, this.semanticModel.GetTypeInfo(node)));
		}
		
	    private sealed class ControlFlowTarget
	    {
	        public ControlFlowTarget(LLVMBasicBlockRef condition, LLVMBasicBlockRef end)
	        {
	            this.Condition = condition;
	            this.End = end;
	        }

	        public LLVMBasicBlockRef Condition { get; private set; }

            public LLVMBasicBlockRef End { get; private set; }
	    }
	}
}