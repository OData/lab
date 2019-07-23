using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace EdmHelperTest
{
    [TestClass]
    public class NWSimple_LoadTest
    {
        [TestMethod]
        public void TestEdmLoader()
        {
            var model = EdmLoader.EdmLoader.ReadModel("NW_Simple.xml");
            Assert.IsNotNull(model);
            EdmLoader.EdmLoader.GetComplexTypes(model, "NorthWind_Simple");
        }
    }
}
