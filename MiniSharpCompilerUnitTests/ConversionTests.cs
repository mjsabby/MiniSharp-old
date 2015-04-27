namespace MiniSharpCompilerUnitTests
{
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class ConversionTests
    {
        static float F()
        {
            int a = (int)10.0F;
            int b = 10 + a;
            return (float)b;
        }

        [TestMethod]
        public void SimpleFloatAddTest()
        {
            int a = (int) 10.0F;
            int b = 10 + a;
            var tree = CSharpSyntaxTree.ParseText(@"
    class C
    {
        static double F()
        {
			int i = 1;
            float f = 10.0F + i;
            return f;
        }
    }
");

            var d = TestSupport.DM(tree)();
            Assert.IsTrue(d == 11.0);
        }

        [TestMethod]
        public void SimpleExplicitConversionTest()
        {
            int a = (int)10.0F;
            int b = 10 + a;
            var tree = CSharpSyntaxTree.ParseText(@"
    class C
    {
        static double F()
        {
            int a = (int)10.0F;
            int b = 10 + a;
            return (float)b;
        }
    }
");

            var d = TestSupport.DM(tree)();
            Assert.IsTrue(d == 20.0);
        }
    }
}