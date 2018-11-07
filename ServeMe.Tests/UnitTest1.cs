namespace ServeMe.Tests
{
    using System.Linq;
    using System.Net;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using ServeMeLib;

    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void TestMethod()
        {
            
            string serverCsv = @"getSome,http://www.google.com,get,200";
            using (var serveMe = new ServeMe())
            {
                string url = serveMe.Start(null, serverCsv).First();
                HttpWebResponse result = (url + "/getSome").Get();
                var finalResult = result.ReadStringFromResponse().Trim().ToLower();
                Assert.IsTrue(finalResult.StartsWith("http"));
                Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
            }
        }

        [TestMethod]
        public void TestMethod1()
        {
            string serverCsv = @"getSome,{'ya':1},get,200";
            using (var serveMe = new ServeMe())
            {
                string url = serveMe.Start(null, serverCsv).First();
                HttpWebResponse result = (url + "/getSome").Get();
                Assert.AreEqual("{'ya':1}", result.ReadStringFromResponse());
                Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
            }
        }

        [TestMethod]
        public void TestMethod2()
        {
            string serverCsv = @"getSome,{'ya':2},get," + (int)HttpStatusCode.Accepted;
            using (var serveMe = new ServeMe())
            {
                string url = serveMe.Start(null, serverCsv).First();
                HttpWebResponse result = (url + "/getSome").Get();
                Assert.AreEqual("{'ya':2}", result.ReadStringFromResponse());
                Assert.AreEqual(HttpStatusCode.Accepted, result.StatusCode);
            }
        }

        [TestMethod]
        public void TestMethod3()
        {
            string serverCsv = @"getSome,{'ya':1},post,200";
            using (var serveMe = new ServeMe())
            {
                string url = serveMe.Start(null, serverCsv).First();
                HttpWebResponse result = (url + "/getSome").Post();
                Assert.AreEqual("{'ya':1}", result.ReadStringFromResponse());
                Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
            }
        }

        [TestMethod]
        public void TestMethod4()
        {
            string serverCsv = @"getSome,{'ya':2},post," + (int)HttpStatusCode.Accepted;
            using (var serveMe = new ServeMe())
            {
                string url = serveMe.Start(null, serverCsv).First();
                HttpWebResponse result = (url + "/getSome").Post();
                Assert.AreEqual("{'ya':2}", result.ReadStringFromResponse());
                Assert.AreEqual(HttpStatusCode.Accepted, result.StatusCode);
            }
        }
    }
}