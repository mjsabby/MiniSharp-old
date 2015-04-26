using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using LLVMSharp;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MiniSharpCompiler
{
	public class Program2
	{
		public static int Prop { get; set; }
	}

	class Program
	{
	    static int f()
	    {
            int i = 0;
            while (i < 100)
            {
                if (i % 2 == 0)
                {
                    i = i + 2;
                    continue;
                }
                else
                {
                    i = i + 1;
                }
            }

            return i;
	    }

		private static void Main(string[] args)
		{
			// Make the module, which holds all the code.
			LLVMModuleRef module = LLVM.ModuleCreateWithName("my cool jit");
			LLVMBuilderRef builder = LLVM.CreateBuilder();

			LLVM.LinkInMCJIT();
			LLVM.InitializeX86TargetInfo();
			LLVM.InitializeX86Target();
			LLVM.InitializeX86TargetMC();
			LLVM.InitializeX86AsmPrinter();

			LLVMExecutionEngineRef engine;
			IntPtr errorMessage;
			if (LLVM.CreateExecutionEngineForModule(out engine, module, out errorMessage).Value == 1)
			{
				Console.WriteLine(Marshal.PtrToStringAnsi(errorMessage));
				LLVM.DisposeMessage(errorMessage);
				return;
			}

			var platform = System.Environment.OSVersion.Platform;
			if (platform == PlatformID.Win32NT) // On Windows, LLVM currently (3.6) does not support PE/COFF
			{
				LLVM.SetTarget(module, Marshal.PtrToStringAnsi(LLVM.GetDefaultTargetTriple()) + "-elf");
			}

			var options = new LLVMMCJITCompilerOptions();
			var optionsSize = (4*sizeof (int)) + IntPtr.Size; // LLVMMCJITCompilerOptions has 4 ints and a pointer

			LLVM.InitializeMCJITCompilerOptions(out options, optionsSize);
			IntPtr error;
			LLVM.CreateMCJITCompilerForModule(out engine, module, out options, optionsSize, out error);


			// Create a function pass manager for this engine
			LLVMPassManagerRef passManager = LLVM.CreateFunctionPassManagerForModule(module);

			// Set up the optimizer pipeline.  Start with registering info about how the
			// target lays out data structures.
			LLVM.AddTargetData(LLVM.GetExecutionEngineTargetData(engine), passManager);
			
			var tree = CSharpSyntaxTree.ParseText(@"
static int Main(int args)
        {
            int i = 0;
            int loopCounter = 0;
            while (i < 100)
            {
                if (i % 2 == 0)
                {
                    i = i + 1;
                    continue;
                    i = i - 1;
                }
                else
                {
                    i = i + 1;
                }

                loopCounter = loopCounter + 1;
            }

            return loopCounter;
        }");
			var tree2 = CSharpSyntaxTree.ParseText(@"

using System;
using System.Collections;

public enum MiniSharpSimpleType : ushort
	{
		Integral,
		FloatingPoint,
		Pointer
	}


	public class Program2
	{
		public static int Prop { get; set; }
	}

    class C
    {

public int Prop {get; set;}

int Fjj(int x) {

foobar:
int aaaw2a = 10;
int sdjkdjs = 10;

ushort s = 5;
		    switch (s)
		    {
			    case 1:
				    break;
case 2:
goto case 1;
		    }

int aaaa = 10;
switch(aaaa) {
case 0:
goto case 1;
case 1:
goto default;
default:
int ssss = 1;
break;
}

	if (x >= 0) goto x;
	x = -x;
	x: return x;
}


		public object xfoo()
		{
			int[] h = new int[10];
			++h[0];
++Program2.Prop;
var c = new C();
++c.Prop;
			uint q = 10;
			var ffffff = -q;
			float jf = -10.0;
			int jff = (int)-10.0;

var ddddd = !true;
int z = +10;
int l = ~10;
int ff = -10;

var k = -10.0F;
Hashtable h = new Hashtable();
h[1] = 2;
int[] arr = new int[10];
arr[1] = 10;
object x = default(int);
			return new {a = 10};
		}

static short bar()
{
return 2;
}

static short foo(Type t)
{
var a = 10;
return a;
}
        static int Main()
        {
var d = bar() + bar();
foo(typeof(string));
object x = null;
object aa = x ?? 0;
var ac = 10 as object;
short a = 10;
short b = 10;
			var c = a + b;
return 0;
        }
    }
");

			var sm = tree.GetRoot();
            var s = MetadataReference.CreateFromAssembly(typeof(object).Assembly);
			var s2 = MetadataReference.CreateFromAssembly(typeof(Hashtable).Assembly);
			var compilation = CSharpCompilation.Create("MyCompilation", new[] { tree }, new[] { s });
		    var model = compilation.GetSemanticModel(tree);

		    var d = model.Compilation.GetDiagnostics();

		    var symbolVisitor = new SymbolTableBuilder(model, builder);
            //symbolVisitor.Visit(sm);

		    var stack = new Stack<LLVMValueRef>();
            var v = new LLVMIRGenerationVisitor(model, module, builder, stack);

		    //var v = new Visitor(model);
            v.Visit(sm);
			LLVM.DumpModule(module);

		    LLVM.VerifyFunction(v.Function, LLVMVerifierFailureAction.LLVMAbortProcessAction);

			//int ddd = f();
			LLVM.AddCFGSimplificationPass(passManager);
			LLVM.AddInstructionCombiningPass(passManager);
			LLVM.AddBasicAliasAnalysisPass(passManager);
			LLVM.AddGVNPass(passManager);
			LLVM.AddPromoteMemoryToRegisterPass(passManager);
			LLVM.RunFunctionPassManager(passManager, v.Function);
			LLVM.RunFunctionPassManager(passManager, v.Function);
			LLVM.DumpModule(module);

          //  var addMethod = (Add)Marshal.GetDelegateForFunctionPointer(LLVM.GetPointerToGlobal(engine, v.Function), typeof(Add));
//		    var x = addMethod();
		}
	}

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate int Add();

    public class Visitor : CSharpSyntaxWalker
    {
        private SemanticModel model;

		private HashSet<object> switchDict = new HashSet<object>();

        public Visitor(SemanticModel model)
        {
            this.model = model;
        }

	    public override void VisitConditionalExpression(ConditionalExpressionSyntax node)
	    {
		    var ti = this.model.GetTypeInfo(node.WhenTrue);
			var ti2 = this.model.GetTypeInfo(node.WhenFalse);
		    var ttt = this.model.GetConversion(node.WhenTrue);
		    var ttt2 = this.model.GetConversion(node.WhenFalse);

		    var symIjf = this.model.GetSymbolInfo(node.WhenTrue);

			base.VisitConditionalExpression(node);
	    }

	    public override void VisitCastExpression(CastExpressionSyntax node)
	    {
		    var nodeType= this.model.GetTypeInfo(node);
		    var nodeEE = this.model.GetTypeInfo(node.Expression);
		    base.VisitCastExpression(node);
	    }

	    public override void VisitVariableDeclarator(VariableDeclaratorSyntax node)
        {
            base.VisitVariableDeclarator(node);
        }

        public override void VisitVariableDeclaration(VariableDeclarationSyntax node)
        {
            var vv = node.Variables[0];
            var dd = this.model.GetTypeInfo(vv.Initializer.Value);
            base.VisitVariableDeclaration(node);
        }

        public override void VisitConstructorDeclaration(ConstructorDeclarationSyntax node)
        {
            base.VisitConstructorDeclaration(node);
        }

	    public override void VisitIdentifierName(IdentifierNameSyntax node)
	    {
		    var dsd = this.model.GetTypeInfo(node);
			base.VisitIdentifierName(node);
	    }

	    public override void VisitReturnStatement(ReturnStatementSyntax node)
	    {
			var dsd = this.model.GetTypeInfo(node.Expression);
			base.VisitReturnStatement(node);
	    }
		
	    public override void VisitExpressionStatement(ExpressionStatementSyntax node)
	    {
		    base.VisitExpressionStatement(node);
	    }

	    public override void VisitLiteralExpression(LiteralExpressionSyntax node)
        {
            
            var token = node.Token;
            var kind = token.Kind();

	        var dsd = this.model.GetTypeInfo(node);
		    var xxx = node.Token.Value;
            
            base.VisitLiteralExpression(node);
        }

        public override void VisitIfStatement(IfStatementSyntax node)
        {
            this.Visit(node.Else);
            base.VisitIfStatement(node);
        }

        public override void VisitEqualsValueClause(EqualsValueClauseSyntax node)
        {
            base.VisitEqualsValueClause(node);
        }

        public override void VisitLocalDeclarationStatement(LocalDeclarationStatementSyntax node)
        {
            var variables = node.Declaration.Variables;
            if (node.Declaration.Type.IsVar)
            {
                var declInfo = model.GetTypeInfo(node.Declaration.Type);
            }
	        var ttt = this.model.GetTypeInfo(node.Declaration.Variables[0].Initializer.Value);

            base.VisitLocalDeclarationStatement(node);
        }

        public override void VisitDoStatement(DoStatementSyntax node)
        {
            base.VisitDoStatement(node);
        }

        public override void VisitWhileStatement(WhileStatementSyntax node)
        {
            base.VisitWhileStatement(node);
        }

        public override void VisitForStatement(ForStatementSyntax node)
        {
            base.VisitForStatement(node);
        }

        public override void VisitBlock(BlockSyntax node)
        {
            Console.WriteLine("");
            base.VisitBlock(node);
        }

        public override void VisitMethodDeclaration(MethodDeclarationSyntax node)
        {
            base.VisitMethodDeclaration(node);
        }

        public override void DefaultVisit(SyntaxNode node)
        {
            base.DefaultVisit(node);
        }

        public override void VisitAssignmentExpression(AssignmentExpressionSyntax node)
        {
            var lT = this.model.GetTypeInfo(node.Left);
            var lT2 = this.model.GetTypeInfo(node.Right);
            base.VisitAssignmentExpression(node);
        }

        public override void VisitPrefixUnaryExpression(PrefixUnaryExpressionSyntax node)
        {
	        var s = this.model.GetTypeInfo(node);
			var d = this.model.GetTypeInfo(node.Operand);
			base.VisitPrefixUnaryExpression(node);
        }

        public override void VisitPostfixUnaryExpression(PostfixUnaryExpressionSyntax node)
        {
            base.VisitPostfixUnaryExpression(node);
        }

	    public override void VisitObjectCreationExpression(ObjectCreationExpressionSyntax node)
	    {
			var info = model.GetTypeInfo(node.Type);
			base.VisitObjectCreationExpression(node);
	    }

	    public override void VisitInvocationExpression(InvocationExpressionSyntax node)
	    {
			var info = model.GetTypeInfo(node);
			base.VisitInvocationExpression(node);
	    }

	    public override void VisitGotoStatement(GotoStatementSyntax node)
	    {
		    if (node.CaseOrDefaultKeyword.IsKind(SyntaxKind.CaseKeyword))
		    {
                if (this.switchDict.Contains(this.model.GetConstantValue(node.Expression).Value))
			    {
					var dtt
				    = this.model.GetTypeInfo(node.Expression);
			    }
                foreach (var statement in ((SwitchSectionSyntax)node.Parent).Statements)
			    {
				    //statement.
			    }
            }
		    //v//ar typeInfo = this.model.GetTypeInfo(node.Expression);
		    var tt = this.model.GetTypeInfo(node);
		    base.VisitGotoStatement(node);
	    }

	    public override void VisitSwitchStatement(SwitchStatementSyntax node)
	    {
		    var expr = this.model.GetTypeInfo(node.Expression);
		    foreach (var section in node.Sections)
		    {
			    foreach (SwitchLabelSyntax label in section.Labels)
			    {
					//this.switchDict.Add(label);
					switch (label.Keyword.Kind())
				    {
					    case SyntaxKind.DefaultKeyword:
							var defaultLabel = (DefaultSwitchLabelSyntax)label;
						    break;
					    case SyntaxKind.CaseKeyword:

						    var caseLabel = (CaseSwitchLabelSyntax)label;
						    this.switchDict.Add(this.model.GetConstantValue(caseLabel.Value).Value);
						    break;
						default:
						    throw new Exception("Unreachable");
				    }

                    Console.WriteLine(label.Keyword.ToString());
			    }
		    }
		    base.VisitSwitchStatement(node);
	    }

	    public override void VisitSwitchSection(SwitchSectionSyntax node)
	    {
		    base.VisitSwitchSection(node);
	    }

	    public override void VisitLabeledStatement(LabeledStatementSyntax node)
	    {
            base.VisitLabeledStatement(node);
	    }

	    public override void VisitElementAccessExpression(ElementAccessExpressionSyntax node)
	    {
		    var f = model.GetTypeInfo(node);
		    var info= model.GetTypeInfo((node.Expression));
		    var info3 = model.GetTypeInfo(node.ArgumentList);
		    var x = node.ArgumentList?.Arguments[0].Expression;
            var info2 = model.GetTypeInfo(node.ArgumentList?.Arguments[0].Expression);
		    base.VisitElementAccessExpression(node);
	    }

	    public override void VisitDefaultExpression(DefaultExpressionSyntax node)
	    {
		    var a = model.GetTypeInfo(node);
		    var b = model.GetTypeInfo(node.Type);
		    base.VisitDefaultExpression(node);
	    }

	    public override void VisitBinaryExpression(BinaryExpressionSyntax node)
        {
            var info = model.GetTypeInfo(node.Left);
            var info2 = model.GetTypeInfo(node.Right);
		    var info3 = model.GetTypeInfo(node);
            base.VisitBinaryExpression(node);
        }
    }
}
