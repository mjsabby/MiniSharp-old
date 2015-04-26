namespace MiniSharpCompilerUnitTests
{
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class ConversionTests
    {
        [TestMethod]
        public void SimpleFloatAddTest()
        {
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
    }
}