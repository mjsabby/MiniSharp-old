namespace MiniSharpCompilerUnitTests
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.InteropServices;
    using LLVMSharp;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using MiniSharpCompiler;

    public class TestSupport
    {
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int A();

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate double D();

        public static A AM(SyntaxTree tree)
        {
            var data = Initialize(tree);
            var v = new LLVMIRGenerationVisitor(data.model, data.module, data.builder, new Stack<LLVMValueRef>());
            v.Visit(tree.GetRoot());
            LLVM.VerifyFunction(v.Function, LLVMVerifierFailureAction.LLVMAbortProcessAction);
            return (A)Marshal.GetDelegateForFunctionPointer(LLVM.GetPointerToGlobal(data.engine, v.Function), typeof(A));
        }

        public static D DM(SyntaxTree tree)
        {
            var data = Initialize(tree);
            var v = new LLVMIRGenerationVisitor(data.model, data.module, data.builder, new Stack<LLVMValueRef>());
            v.Visit(tree.GetRoot());
            LLVM.VerifyFunction(v.Function, LLVMVerifierFailureAction.LLVMAbortProcessAction);
            return (D)Marshal.GetDelegateForFunctionPointer(LLVM.GetPointerToGlobal(data.engine, v.Function), typeof(D));
        }

        #region private

        private class InitData
        {
            public LLVMExecutionEngineRef engine;

            public LLVMModuleRef module;

            public SemanticModel model;

            public LLVMBuilderRef builder;
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
                LLVM.SetTarget(module, string.Concat(Marshal.PtrToStringAnsi(LLVM.GetDefaultTargetTriple()), "-elf"));
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

            LLVM.AddGVNPass(passManager);

            LLVMBool f = LLVM.InitializeFunctionPassManager(passManager);


            var s = MetadataReference.CreateFromAssembly(typeof (object).Assembly);
            var compilation = CSharpCompilation.Create("MyCompilation", new[] {tree}, new[] {s});
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