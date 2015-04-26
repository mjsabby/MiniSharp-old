namespace MiniSharpCompilerUnitTests
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.InteropServices;
    using LLVMSharp;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using MiniSharpCompiler;

    [TestClass]
    public class ControlFlowTests
    {
        [TestMethod]
        public void SimpleWhileLoopTest()
        {
            var tree = CSharpSyntaxTree.ParseText(@"
    class C
    {
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
        }
    }
");

            var data = Initialize(tree);
            var v = new LLVMIRGenerationVisitor(data.model, data.module, data.builder, new Stack<LLVMValueRef>());
            v.Visit(tree.GetRoot());
			LLVM.DumpModule(data.module);
	        LLVM.WriteBitcodeToFile(data.module, @"C:\a.bc");
	        LLVM.VerifyFunction(v.Function, LLVMVerifierFailureAction.LLVMAbortProcessAction);
			var addMethod = (Add)Marshal.GetDelegateForFunctionPointer(LLVM.GetPointerToGlobal(data.engine, v.Function), typeof(Add));
            Assert.IsTrue(addMethod() == 50);
        }

        [TestMethod]
        public void SimpleDoWhileLoopTest()
        {
            var tree = CSharpSyntaxTree.ParseText(@"
    class C
    {
        static int Main()
        {
            int i = 0;
            int loopCounter = 0;
            do
            {
                continue;
                loopCounter = loopCounter + 1;
            } while (i > 100);

            return loopCounter;
        }
    }
");
            var data = Initialize(tree);
            var v = new LLVMIRGenerationVisitor(data.model, data.module, data.builder, new Stack<LLVMValueRef>());
            v.Visit(tree.GetRoot());
            LLVM.DumpModule(data.module);
			LLVM.VerifyFunction(v.Function, LLVMVerifierFailureAction.LLVMAbortProcessAction);
			var addMethod = (Add)Marshal.GetDelegateForFunctionPointer(LLVM.GetPointerToGlobal(data.engine, v.Function), typeof(Add));
            Assert.IsTrue(addMethod() == 0);
        }

        [TestMethod]
        public void SimpleDoWhileLoopTest2()
        {
            var tree = CSharpSyntaxTree.ParseText(@"
    class C
    {
        static int Main()
        {
            int i = 0;
            int loopCounter = 0;
            do
            {
                loopCounter = loopCounter + 1;
                i = i + 2;
            } while (i < 99);


            return loopCounter;
        }
    }
");
            var data = Initialize(tree);
            var v = new LLVMIRGenerationVisitor(data.model, data.module, data.builder, new Stack<LLVMValueRef>());
            v.Visit(tree.GetRoot());
            LLVM.DumpModule(data.module);
			LLVM.VerifyFunction(v.Function, LLVMVerifierFailureAction.LLVMAbortProcessAction);
			var addMethod = (Add)Marshal.GetDelegateForFunctionPointer(LLVM.GetPointerToGlobal(data.engine, v.Function), typeof(Add));
            Assert.IsTrue(addMethod() == 50);
        }

        [TestMethod]
        public void SimpleDoWhileLoopTest3()
        {
            var tree = CSharpSyntaxTree.ParseText(@"
    class C
    {
        static int Main()
        {
            int i = 0;
            int loopCounter = 0;
            do
            {
                loopCounter = loopCounter + 1;
                i = i + 2;
            } while (i < 100);


            return loopCounter;
        }
    }
");
            var data = Initialize(tree);
            var v = new LLVMIRGenerationVisitor(data.model, data.module, data.builder, new Stack<LLVMValueRef>());
            v.Visit(tree.GetRoot());
            LLVM.DumpModule(data.module);
			LLVM.VerifyFunction(v.Function, LLVMVerifierFailureAction.LLVMAbortProcessAction);
			var addMethod = (Add)Marshal.GetDelegateForFunctionPointer(LLVM.GetPointerToGlobal(data.engine, v.Function), typeof(Add));
            var result = addMethod();
            Assert.IsTrue(result == 50);
        }

        [TestMethod]
        public void PreIncrementTest()
        {
            var tree = CSharpSyntaxTree.ParseText(@"
public class C
{
    private static int Main()
    {
        int i;
        int loopCounter = 0;
        do
        {
            i = ++loopCounter;
        } while (i < 100);

        return loopCounter;
    }
}");
            var data = Initialize(tree);
            var v = new LLVMIRGenerationVisitor(data.model, data.module, data.builder, new Stack<LLVMValueRef>());
            v.Visit(tree.GetRoot());
            LLVM.DumpModule(data.module);
			LLVM.VerifyFunction(v.Function, LLVMVerifierFailureAction.LLVMAbortProcessAction);
			var addMethod = (Add)Marshal.GetDelegateForFunctionPointer(LLVM.GetPointerToGlobal(data.engine, v.Function), typeof(Add));
            var result = addMethod();
            Assert.IsTrue(result == 100);
        }

        [TestMethod]
        public void PostIncrementTest()
        {
            var tree = CSharpSyntaxTree.ParseText(@"
public class C
{
    private static int Main()
    {
        int i;
        int loopCounter = 0;
        do
        {
            i = loopCounter++;
        } while (i < 100);

        return loopCounter;
    }
}");
            var data = Initialize(tree);
            var v = new LLVMIRGenerationVisitor(data.model, data.module, data.builder, new Stack<LLVMValueRef>());
            v.Visit(tree.GetRoot());
            LLVM.DumpModule(data.module);
			LLVM.VerifyFunction(v.Function, LLVMVerifierFailureAction.LLVMAbortProcessAction);
			var addMethod = (Add)Marshal.GetDelegateForFunctionPointer(LLVM.GetPointerToGlobal(data.engine, v.Function), typeof(Add));
            var result = addMethod();
            Assert.IsTrue(result == 101);
        }

		[TestMethod]
		public void PostIncrementTest2()
		{
			int i = 1;
			i = i++ * i;
			var tree = CSharpSyntaxTree.ParseText(@"
public class C
{
    private static int Main()
    {
        int i = -1;
        i = i++;

        return i;
    }
}");
			var data = Initialize(tree);
			var v = new LLVMIRGenerationVisitor(data.model, data.module, data.builder, new Stack<LLVMValueRef>());
			v.Visit(tree.GetRoot());
			LLVM.DumpModule(data.module);
			LLVM.VerifyFunction(v.Function, LLVMVerifierFailureAction.LLVMAbortProcessAction);
			var addMethod = (Add)Marshal.GetDelegateForFunctionPointer(LLVM.GetPointerToGlobal(data.engine, v.Function), typeof(Add));
			var result = addMethod();
			Assert.IsTrue(result == -1);
		}

		[TestMethod]
		public void PostIncrementTest3()
		{

			var tree = CSharpSyntaxTree.ParseText(@"
public class C
{
    private static int Main()
    {
			int i = 1;
			i = i++ * i++;

        return i;
    }
}");
			var data = Initialize(tree);
			var v = new LLVMIRGenerationVisitor(data.model, data.module, data.builder, new Stack<LLVMValueRef>());
			v.Visit(tree.GetRoot());
			LLVM.DumpModule(data.module);
			LLVM.VerifyFunction(v.Function, LLVMVerifierFailureAction.LLVMAbortProcessAction);
			var addMethod = (Add)Marshal.GetDelegateForFunctionPointer(LLVM.GetPointerToGlobal(data.engine, v.Function), typeof(Add));
			var result = addMethod();
			Assert.IsTrue(result == 2);
		}

		[TestMethod]
		public void PostIncrementTest4()
		{

			var tree = CSharpSyntaxTree.ParseText(@"
public class C
{
    private static int Main()
    {
			int i = 0x7fffffff;
			i = i++ * i++;

        return i;
    }
}");
			var data = Initialize(tree);
			var v = new LLVMIRGenerationVisitor(data.model, data.module, data.builder, new Stack<LLVMValueRef>());
			v.Visit(tree.GetRoot());
			LLVM.DumpModule(data.module);
			LLVM.VerifyFunction(v.Function, LLVMVerifierFailureAction.LLVMAbortProcessAction);
			var addMethod = (Add)Marshal.GetDelegateForFunctionPointer(LLVM.GetPointerToGlobal(data.engine, v.Function), typeof(Add));
			var result = addMethod();
			Assert.IsTrue(result == -2147483648);
		}

		[TestMethod]
		public void PostIncrementTest5()
		{

			var tree = CSharpSyntaxTree.ParseText(@"
public class C
{
    private static int Main()
    {
			int i = 0x7fffffff -1;
			i = i++ + ++i;

        return i;
    }
}");
			var data = Initialize(tree);
			var v = new LLVMIRGenerationVisitor(data.model, data.module, data.builder, new Stack<LLVMValueRef>());
			v.Visit(tree.GetRoot());
			LLVM.DumpModule(data.module);
			LLVM.VerifyFunction(v.Function, LLVMVerifierFailureAction.LLVMAbortProcessAction);
			var addMethod = (Add)Marshal.GetDelegateForFunctionPointer(LLVM.GetPointerToGlobal(data.engine, v.Function), typeof(Add));
			var result = addMethod();
			Assert.IsTrue(result == -2);
		}

		[TestMethod]
        public void ForLoopTest()
        {
            var tree = CSharpSyntaxTree.ParseText(@"
public class C
{
    public static int Main()
    {
        int loopCounter = 0;
        for (int i = 2; i < 100; ++i)
        {
            ++loopCounter;
        }
            
        return loopCounter;
    }
}");
            var data = Initialize(tree);
            var v = new LLVMIRGenerationVisitor(data.model, data.module, data.builder, new Stack<LLVMValueRef>());
            v.Visit(tree.GetRoot());
            LLVM.DumpModule(data.module);
			LLVM.VerifyFunction(v.Function, LLVMVerifierFailureAction.LLVMAbortProcessAction);
			var addMethod = (Add)Marshal.GetDelegateForFunctionPointer(LLVM.GetPointerToGlobal(data.engine, v.Function), typeof(Add));
            var result = addMethod();
            Assert.IsTrue(result == 98);
        }

		[TestMethod]
		public void TernaryOperatorTest()
		{
			var tree = CSharpSyntaxTree.ParseText(@"
public class C
{
    public static int Main()
    {
            bool a = false;
            int b = 2;
            bool c = true;
            int d = 4;
            int e = 5;
            int f = a ? b : c ? d : e;
            int cond = d > 5 ? 1 : f;
            return cond;
    }
}");
			var data = Initialize(tree);
			var v = new LLVMIRGenerationVisitor(data.model, data.module, data.builder, new Stack<LLVMValueRef>());
			v.Visit(tree.GetRoot());
			LLVM.DumpModule(data.module);
			LLVM.VerifyFunction(v.Function, LLVMVerifierFailureAction.LLVMAbortProcessAction);
			var addMethod = (Add)Marshal.GetDelegateForFunctionPointer(LLVM.GetPointerToGlobal(data.engine, v.Function), typeof(Add));
			var result = addMethod();
			Assert.IsTrue(result == 4);
		}

		[TestMethod]
		public void SimpleGotoTest()
		{
			var tree = CSharpSyntaxTree.ParseText(@"
    class C
    {
        static int Main(int args)
        {
            int i = 1;
goto foo;
        
foo:
            return i;
        }
    }
");

			var data = Initialize(tree);
			var v = new LLVMIRGenerationVisitor(data.model, data.module, data.builder, new Stack<LLVMValueRef>());
			v.Visit(tree.GetRoot());
			LLVM.VerifyFunction(v.Function, LLVMVerifierFailureAction.LLVMAbortProcessAction);
			var addMethod = (Add)Marshal.GetDelegateForFunctionPointer(LLVM.GetPointerToGlobal(data.engine, v.Function), typeof(Add));
			Assert.IsTrue(addMethod() == 1);
		}

		[TestMethod]
		public void SimpleGotoTest2()
		{
			var tree = CSharpSyntaxTree.ParseText(@"
    class C
    {
        static int Main(int args)
        {
            int i = 1;
goto foo;
        i++;
foo:
            return i;
        }
    }
");

			var data = Initialize(tree);
			var v = new LLVMIRGenerationVisitor(data.model, data.module, data.builder, new Stack<LLVMValueRef>());
			v.Visit(tree.GetRoot());
			LLVM.VerifyFunction(v.Function, LLVMVerifierFailureAction.LLVMAbortProcessAction);
			var addMethod = (Add)Marshal.GetDelegateForFunctionPointer(LLVM.GetPointerToGlobal(data.engine, v.Function), typeof(Add));
			Assert.IsTrue(addMethod() == 1);
		}

		[TestMethod]
		public void SimpleGotoTest3()
		{
			var tree = CSharpSyntaxTree.ParseText(@"
    class C
    {
        static int Main(int args)
        {
            int i = 1;
goto foo;
foo:
i++;
            return i;
        }
    }
");

			var data = Initialize(tree);
			var v = new LLVMIRGenerationVisitor(data.model, data.module, data.builder, new Stack<LLVMValueRef>());
			v.Visit(tree.GetRoot());
			LLVM.VerifyFunction(v.Function, LLVMVerifierFailureAction.LLVMAbortProcessAction);
			var addMethod = (Add)Marshal.GetDelegateForFunctionPointer(LLVM.GetPointerToGlobal(data.engine, v.Function), typeof(Add));
			Assert.IsTrue(addMethod() == 2);
		}

		[TestMethod]
		public void SimpleGotoTest4()
		{
			var tree = CSharpSyntaxTree.ParseText(@"
	 class C
	 {
            public static int Main(int args)
            {
                int j = -1;
                int i = 1 + j*2;
                goto foo;
                bar:
                j = 10;
                return i + j;
            foo:
                i++;
                j = i == 2 ? i++ : i--;
                goto bar;
                return i;
            }
		}
");

			var data = Initialize(tree);
			var v = new LLVMIRGenerationVisitor(data.model, data.module, data.builder, new Stack<LLVMValueRef>());
			v.Visit(tree.GetRoot());
			LLVM.VerifyFunction(v.Function, LLVMVerifierFailureAction.LLVMAbortProcessAction);
			var addMethod = (Add)Marshal.GetDelegateForFunctionPointer(LLVM.GetPointerToGlobal(data.engine, v.Function), typeof(Add));
			Assert.IsTrue(addMethod() == 9);
		}

		[TestMethod]
		public void SimpleSwitchTest()
		{
			var tree = CSharpSyntaxTree.ParseText(@"
            public static int Main(int args)
            {
                int ret = 1;
                switch (true)
                {
                default:
                    ret = 2;
                    break;
                }
                return (ret);
            }
");

			var data = Initialize(tree);
			var v = new LLVMIRGenerationVisitor(data.model, data.module, data.builder, new Stack<LLVMValueRef>());
			v.Visit(tree.GetRoot());
			LLVM.VerifyFunction(v.Function, LLVMVerifierFailureAction.LLVMAbortProcessAction);
			var addMethod = (Add)Marshal.GetDelegateForFunctionPointer(LLVM.GetPointerToGlobal(data.engine, v.Function), typeof(Add));
			Assert.IsTrue(addMethod() == 2);
		}

		[TestMethod]
		public void SimpleSwitchTest2()
		{
			var tree = CSharpSyntaxTree.ParseText(@"
            public static int Main(int args)
            {
                int ret =2;
                switch (1+1)
                {
                default:
                    ret--;
                    break;
                }
                return (ret);
            }
");

			var data = Initialize(tree);
			var v = new LLVMIRGenerationVisitor(data.model, data.module, data.builder, new Stack<LLVMValueRef>());
			v.Visit(tree.GetRoot());
			LLVM.VerifyFunction(v.Function, LLVMVerifierFailureAction.LLVMAbortProcessAction);
			var addMethod = (Add)Marshal.GetDelegateForFunctionPointer(LLVM.GetPointerToGlobal(data.engine, v.Function), typeof(Add));
			Assert.IsTrue(addMethod() == 1);
		}

		[TestMethod]
		public void SimpleSwitchTest3()
		{
			var tree = CSharpSyntaxTree.ParseText(@"
public class Test {
            public static int Main(int args)
            {
const int kValue = 23;
                int ret = 1;
		int value = 23;
		switch (value) {
		case kValue:
			ret = 0;
			break;
		default:
			ret = 1;
       	    break;
		}
                return (ret);
            }
}
");

			var data = Initialize(tree);
			var v = new LLVMIRGenerationVisitor(data.model, data.module, data.builder, new Stack<LLVMValueRef>());
			v.Visit(tree.GetRoot());
			LLVM.VerifyFunction(v.Function, LLVMVerifierFailureAction.LLVMAbortProcessAction);
			var addMethod = (Add)Marshal.GetDelegateForFunctionPointer(LLVM.GetPointerToGlobal(data.engine, v.Function), typeof(Add));
			Assert.IsTrue(addMethod() == 0);
		}

		[TestMethod]
		public void DefaultExpressionInLabel()
		{
			var tree = CSharpSyntaxTree.ParseText(@"
public class Test {
            public static int Main(int a)
            {
switch (0)
        {
            case default(int): 
return -11;
        }
            }
         }
");

			var data = Initialize(tree);
			var v = new LLVMIRGenerationVisitor(data.model, data.module, data.builder, new Stack<LLVMValueRef>());
			v.Visit(tree.GetRoot());
			LLVM.VerifyFunction(v.Function, LLVMVerifierFailureAction.LLVMAbortProcessAction);
			var addMethod = (Add)Marshal.GetDelegateForFunctionPointer(LLVM.GetPointerToGlobal(data.engine, v.Function), typeof(Add));
			Assert.IsTrue(addMethod() == -11);
		}

		[TestMethod]
		public void SwitchWith_NoMatchingCaseLabel_And_NoDefaultLabel()
		{
			var tree = CSharpSyntaxTree.ParseText(@"
public class Test {
            public static int Main(int a)
            {
int i = 5;
    switch (i)
    {
      case 1:
      case 2:
      case 3:
        return 1;
      case 1001:
      case 1002:
      case 1003:
        return 2;
    }
    return 5;
            }
         }
");

			var data = Initialize(tree);
			var v = new LLVMIRGenerationVisitor(data.model, data.module, data.builder, new Stack<LLVMValueRef>());
			v.Visit(tree.GetRoot());
			LLVM.VerifyFunction(v.Function, LLVMVerifierFailureAction.LLVMAbortProcessAction);
			var addMethod = (Add)Marshal.GetDelegateForFunctionPointer(LLVM.GetPointerToGlobal(data.engine, v.Function), typeof(Add));
			Assert.IsTrue(addMethod() == 5);
		}

		[TestMethod]
		public void ByteTypeSwitchArgumentExpression()
		{
			var tree = CSharpSyntaxTree.ParseText(@"
public class Test {
            public static int Main(int a)
            {
int ret = 2;
		byte b = 2;
		switch (b) {
		case 1:
		case 2:
			ret--;
			break;
		case 3:
			break;
		default:
			break;
		}
		switch (b) {
		case 1:
		case 3:
			break;
		default:
			ret--;
       	    break;
		}

return ret;
            }
         }
");

			var data = Initialize(tree);
			var v = new LLVMIRGenerationVisitor(data.model, data.module, data.builder, new Stack<LLVMValueRef>());
			v.Visit(tree.GetRoot());
			LLVM.VerifyFunction(v.Function, LLVMVerifierFailureAction.LLVMAbortProcessAction);
			var addMethod = (Add)Marshal.GetDelegateForFunctionPointer(LLVM.GetPointerToGlobal(data.engine, v.Function), typeof(Add));
			Assert.IsTrue(addMethod() == 0);
		}

		[TestMethod]
		public void SByteTypeSwitchArgumentExpression()
		{
			var tree = CSharpSyntaxTree.ParseText(@"
public class Test {
            public static int Main(int a)
            {
int ret = 2;
		sbyte b = -2;
		switch (b) {
		case -1:
		case -2:
			ret--;
			break;
		case -3:
			break;
		default:
			break;
		}
		switch (b) {
		case -1:
		case -3:
			break;
		default:
			ret--;
       	    break;
		}

return ret;
            }
         }
");

			var data = Initialize(tree);
			var v = new LLVMIRGenerationVisitor(data.model, data.module, data.builder, new Stack<LLVMValueRef>());
			v.Visit(tree.GetRoot());
			LLVM.VerifyFunction(v.Function, LLVMVerifierFailureAction.LLVMAbortProcessAction);
			var addMethod = (Add)Marshal.GetDelegateForFunctionPointer(LLVM.GetPointerToGlobal(data.engine, v.Function), typeof(Add));
			Assert.IsTrue(addMethod() == 0);
		}

		[TestMethod]
		public void LongTypeSwitchArgumentExpression1()
		{
			var tree = CSharpSyntaxTree.ParseText(@"
public class Test {
            public static int Main(int a)
            {
int ret = 2;
		long b = -9223372036854775808L;
		switch (b) {
		case -1:
		case -9223372036854775808L:
			ret--;
			break;
		case -3:
			break;
		default:
			break;
		}
		switch (b) {
		case -1:
		case -3:
			break;
		default:
			ret--;
       	    break;
		}

return ret;
            }
         }
");

			var data = Initialize(tree);
			var v = new LLVMIRGenerationVisitor(data.model, data.module, data.builder, new Stack<LLVMValueRef>());
			v.Visit(tree.GetRoot());
			LLVM.DumpModule(data.module);
			LLVM.VerifyFunction(v.Function, LLVMVerifierFailureAction.LLVMAbortProcessAction);
			var addMethod = (Add)Marshal.GetDelegateForFunctionPointer(LLVM.GetPointerToGlobal(data.engine, v.Function), typeof(Add));
			Assert.IsTrue(addMethod() == 0);
		}

		[TestMethod]
		public void LongTypeSwitchArgumentExpression2()
		{
			var tree = CSharpSyntaxTree.ParseText(@"
public class Test {
            public static int Main(int a)
            {
int ret = 2;
		long b = -9223372036854775808L;
		switch (b) {
		case -1:
		case -2:
			ret--;
			break;
		case -3:
			break;
		default:
			break;
		}
		switch (b) {
		case -1:
		case -3:
			break;
		default:
			ret--;
       	    break;
		}

return ret;
            }
         }
");

			var data = Initialize(tree);
			var v = new LLVMIRGenerationVisitor(data.model, data.module, data.builder, new Stack<LLVMValueRef>());
			v.Visit(tree.GetRoot());
			LLVM.VerifyFunction(v.Function, LLVMVerifierFailureAction.LLVMAbortProcessAction);
			var addMethod = (Add)Marshal.GetDelegateForFunctionPointer(LLVM.GetPointerToGlobal(data.engine, v.Function), typeof(Add));
			Assert.IsTrue(addMethod() == 1);
		}

		[TestMethod]
		public void LongTypeSwitchArgumentExpression3()
		{
			var tree = CSharpSyntaxTree.ParseText(@"
public class Test {
            public static int Main(int a)
            {
long b = 42;
int ret = 2;
		switch (b) {
		case 1:
            ret++;
            break;            
		case 2:
			ret--;
			break;
		case 3:
			break;
		case 4:
			ret = ret + 7;
            break;
		default:
			ret = ret + 2;
            break;
		}
		return ret;
            }
         }
");

			var data = Initialize(tree);
			var v = new LLVMIRGenerationVisitor(data.model, data.module, data.builder, new Stack<LLVMValueRef>());
			v.Visit(tree.GetRoot());
			LLVM.VerifyFunction(v.Function, LLVMVerifierFailureAction.LLVMAbortProcessAction);
			var addMethod = (Add)Marshal.GetDelegateForFunctionPointer(LLVM.GetPointerToGlobal(data.engine, v.Function), typeof(Add));
			Assert.IsTrue(addMethod() == 4);
		}

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int Add();

        #region private
        private class InitData
        {
            public LLVMExecutionEngineRef engine;

            public LLVMModuleRef module;

            public SemanticModel model;

            public LLVMBuilderRef builder;

			public LLVMPassManagerRef passmanager;
		}

        private static InitData Initialize(SyntaxTree tree)
        {
            LLVMModuleRef module = LLVM.ModuleCreateWithName("test jit");
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
                throw new Exception("Dispose error");
            }

            var platform = Environment.OSVersion.Platform;
            if (platform == PlatformID.Win32NT) // On Windows, LLVM currently (3.6) does not support PE/COFF
            {
                LLVM.SetTarget(module, Marshal.PtrToStringAnsi(LLVM.GetDefaultTargetTriple()) + "-elf");
            }

            var options = new LLVMMCJITCompilerOptions();
            var optionsSize = (4 * sizeof(int)) + IntPtr.Size; // LLVMMCJITCompilerOptions has 4 ints and a pointer

            LLVM.InitializeMCJITCompilerOptions(out options, optionsSize);
            IntPtr error;
            LLVM.CreateMCJITCompilerForModule(out engine, module, out options, optionsSize, out error);

            // Create a function pass manager for this engine
            LLVMPassManagerRef passManager = LLVM.CreateFunctionPassManagerForModule(module);

            // Set up the optimizer pipeline.  Start with registering info about how the
            // target lays out data structures.
            LLVM.AddTargetData(LLVM.GetExecutionEngineTargetData(engine), passManager);

			LLVM.AddGVNPass(passManager);

			LLVMBool f = LLVM.InitializeFunctionPassManager(passManager);


			var s = MetadataReference.CreateFromAssembly(typeof(object).Assembly);
            var compilation = CSharpCompilation.Create("MyCompilation", new[] { tree }, new[] { s });
            var model = compilation.GetSemanticModel(tree);

            var initData = new InitData();
            initData.builder = builder;
            initData.engine = engine;
            initData.model = model;
            initData.module = module;

            return initData;
        }
        #endregion
    }
}
