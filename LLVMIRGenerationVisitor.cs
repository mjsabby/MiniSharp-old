namespace MiniSharpCompiler
{
    using System;
    using System.Collections.Generic;
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
				++index;
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
                    this.Push(node.Type, LLVM.BuildStore(this.builder, this.Pop(variable.Initializer.Value), alloca));
                }
            }
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

		private LLVMValueRef OnlyPop(ExpressionSyntax node)
		{
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