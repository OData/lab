using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace EdmHelperTest
{
    [TestClass]
    public class Trippin_LoadTest
    {
        [TestMethod]
        public void TestEdmLoader()
        {
            var model = EdmLoader.EdmLoader.ReadModel("Trippin.xml");
            Assert.IsNotNull(model);
        }
    }
}
