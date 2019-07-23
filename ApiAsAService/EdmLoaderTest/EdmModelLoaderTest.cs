using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace EdmLoaderTest
{
    [TestClass]
    public class EdmModelLoaderTest
    {
        [TestMethod]
        public void TestEdmLoader()
        {
            var model = EdmLoader.EdmLoader.ReadModel("NW_Simple.xml");
            Assert.IsNotNull(model);
        }
    }
}
