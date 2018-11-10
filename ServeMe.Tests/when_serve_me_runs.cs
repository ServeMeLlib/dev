﻿namespace ServeMe.Tests
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using ServeMeLib;
    using System.Linq;
    using System.Net;

    [TestClass]
    public class when_serve_me_runs
    {
        [TestMethod]
        public void it_can_return_external_network_resource_with_get_and_ok_status_code()
        {
            string serverCsv = @"getSome,http://www.google.com,get,200";
            using (var serveMe = new ServeMe())
            {
                string url = serveMe.Start(null, serverCsv).First();
                HttpWebResponse result = (url + "/getSome").Get();
                string finalResult = result.ReadStringFromResponse().Trim().ToLower();
                Assert.IsTrue(finalResult.StartsWith("<!doc"));
                Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
            }
        }

        [TestMethod]
        public void it_can_return_json_string_provided_inline_with_get_and_ok_status_code()
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
        public void it_can_return_json_string_provided_inline_with_get_and_accepted_status_code()
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
        public void it_can_return_json_string_provided_inline_with_post_and_ok_status_code()
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
        public void it_can_return_json_string_provided_inline_with_post_and_accepted_status_code()
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

        [TestMethod]
        public void it_can_match_where_the_path_and_query_is_equal_to_the_route_provided_and_return_json_string_with_post()
        {
            string serverCsv = @"equalto /getSome,{'ya':2},post," + (int)HttpStatusCode.Accepted;
            using (var serveMe = new ServeMe())
            {
                string url = serveMe.Start(null, serverCsv).First();
                HttpWebResponse result = (url + "/getSome").Post();
                Assert.AreEqual("{'ya':2}", result.ReadStringFromResponse());
                Assert.AreEqual(HttpStatusCode.Accepted, result.StatusCode);
            }
        }

        [TestMethod]
        public void it_can_match_where_the_path_and_query_contains_the_route_provided_and_return_json_string_with_post()
        {
            string serverCsv = @"contains so,{'ya':2},post," + (int)HttpStatusCode.Accepted;
            using (var serveMe = new ServeMe())
            {
                string url = serveMe.Start(null, serverCsv).First();
                HttpWebResponse result = (url + "/getSome").Post();
                Assert.AreEqual("{'ya':2}", result.ReadStringFromResponse());
                Assert.AreEqual(HttpStatusCode.Accepted, result.StatusCode);
            }
        }

        [TestMethod]
        public void it_can_match_where_the_path_and_query_starts_with_the_route_provided_and_return_json_string_with_post()
        {
            string serverCsv = @"startswith /g,{'ya':2},post," + (int)HttpStatusCode.Accepted;
            using (var serveMe = new ServeMe())
            {
                string url = serveMe.Start(null, serverCsv).First();
                HttpWebResponse result = (url + "/getSome").Post();
                Assert.AreEqual("{'ya':2}", result.ReadStringFromResponse());
                Assert.AreEqual(HttpStatusCode.Accepted, result.StatusCode);
            }
        }

        [TestMethod]
        public void it_can_match_where_the_path_and_query_ends_with_the_route_provided_and_return_json_string_with_post()
        {
            string serverCsv = @"endswith me,{'ya':2},post," + (int)HttpStatusCode.Accepted;
            using (var serveMe = new ServeMe())
            {
                string url = serveMe.Start(null, serverCsv).First();
                HttpWebResponse result = (url + "/getSome").Post();
                Assert.AreEqual("{'ya':2}", result.ReadStringFromResponse());
                Assert.AreEqual(HttpStatusCode.Accepted, result.StatusCode);
            }
        }

        [TestMethod]
        public void it_can_match_where_the_path_and_query_contains_the_route_provided_and_return_json_string_with_post2()
        {
            string serverCsv = @"contains /g,{'ya':2},post," + (int)HttpStatusCode.Accepted;
            using (var serveMe = new ServeMe())
            {
                string url = serveMe.Start(null, serverCsv).First();
                HttpWebResponse result = (url + "/getSome").Post();
                Assert.AreEqual("{'ya':2}", result.ReadStringFromResponse());
                Assert.AreEqual(HttpStatusCode.Accepted, result.StatusCode);
            }
        }

        [TestMethod]
        public void it_can_match_where_the_path_and_query_contains_the_route_provided_and_return_json_string_with_post3()
        {
            string serverCsv = @"contains me,{'ya':2},post," + (int)HttpStatusCode.Accepted;
            using (var serveMe = new ServeMe())
            {
                string url = serveMe.Start(null, serverCsv).First();
                HttpWebResponse result = (url + "/getSome").Post();
                Assert.AreEqual("{'ya':2}", result.ReadStringFromResponse());
                Assert.AreEqual(HttpStatusCode.Accepted, result.StatusCode);
            }
        }

        [TestMethod]
        public void it_can_match_where_the_path_and_query_matches_with_regular_expression_the_route_provided_and_return_json_string_with_post()
        {
            string serverCsv = @"regex \/getSome,{'ya':2},post," + (int)HttpStatusCode.Accepted;
            using (var serveMe = new ServeMe())
            {
                string url = serveMe.Start(null, serverCsv).First();
                HttpWebResponse result = (url + "/getSome").Post();
                Assert.AreEqual("{'ya':2}", result.ReadStringFromResponse());
                Assert.AreEqual(HttpStatusCode.Accepted, result.StatusCode);
            }
        }

        [TestMethod]
        public void it_can_match_where_the_path_and_query_is_NOT_equal_to_the_route_provided_and_return_json_string_with_post()
        {
            string serverCsv = @"!equalto getSome,{'ya':2},post," + (int)HttpStatusCode.Accepted;
            using (var serveMe = new ServeMe())
            {
                string url = serveMe.Start(null, serverCsv).First();
                HttpWebResponse result = (url + "/getSome").Post();
                Assert.AreEqual("{'ya':2}", result.ReadStringFromResponse());
                Assert.AreEqual(HttpStatusCode.Accepted, result.StatusCode);
            }
        }

        [TestMethod]
        public void it_can_match_where_the_path_and_query_does_NOT_contain_the_route_provided_and_return_json_string_with_post()
        {
            string serverCsv = @"!contains us,{'ya':2},post," + (int)HttpStatusCode.Accepted;
            using (var serveMe = new ServeMe())
            {
                string url = serveMe.Start(null, serverCsv).First();
                HttpWebResponse result = (url + "/getSome").Post();
                Assert.AreEqual("{'ya':2}", result.ReadStringFromResponse());
                Assert.AreEqual(HttpStatusCode.Accepted, result.StatusCode);
            }
        }

        [TestMethod]
        public void it_can_match_where_the_path_and_query_does_NOT_start_with_the_route_provided_and_return_json_string_with_post()
        {
            string serverCsv = @"!startswith me,{'ya':2},post," + (int)HttpStatusCode.Accepted;
            using (var serveMe = new ServeMe())
            {
                string url = serveMe.Start(null, serverCsv).First();
                HttpWebResponse result = (url + "/getSome").Post();
                Assert.AreEqual("{'ya':2}", result.ReadStringFromResponse());
                Assert.AreEqual(HttpStatusCode.Accepted, result.StatusCode);
            }
        }

        [TestMethod]
        public void it_can_match_where_the_path_and_query_does_NOT_end_with_the_route_provided_and_return_json_string_with_post()
        {
            string serverCsv = @"!endswith /ge,{'ya':2},post," + (int)HttpStatusCode.Accepted;
            using (var serveMe = new ServeMe())
            {
                string url = serveMe.Start(null, serverCsv).First();
                HttpWebResponse result = (url + "/getSome").Post();
                Assert.AreEqual("{'ya':2}", result.ReadStringFromResponse());
                Assert.AreEqual(HttpStatusCode.Accepted, result.StatusCode);
            }
        }

        [TestMethod]
        public void it_can_match_where_the_path_and_query_does_NOT_matche_with_regular_expression_the_route_provided_and_return_json_string_with_post()
        {
            string serverCsv = @"!regex /getSome(d),{'ya':2},post," + (int)HttpStatusCode.Accepted;
            using (var serveMe = new ServeMe())
            {
                string url = serveMe.Start(null, serverCsv).First();
                HttpWebResponse result = (url + "/getSome").Post();
                Assert.AreEqual("{'ya':2}", result.ReadStringFromResponse());
                Assert.AreEqual(HttpStatusCode.Accepted, result.StatusCode);
            }
        }

        [TestMethod]
        public void it_can_match_where_the_path_and_query_matches_with_regular_expression_the_route_provided_and_return_json_string_with_post2()
        {
            string serverCsv = @"regex (.*)me,{'ya':2},post," + (int)HttpStatusCode.Accepted;
            using (var serveMe = new ServeMe())
            {
                string url = serveMe.Start(null, serverCsv).First();
                HttpWebResponse result = (url + "/getSome").Post();
                Assert.AreEqual("{'ya':2}", result.ReadStringFromResponse());
                Assert.AreEqual(HttpStatusCode.Accepted, result.StatusCode);
            }
        }

        [TestMethod]
        public void it_can_match_where_the_path_and_query_is_equal_to_the_route_provided_and_return_json_string_with_Get()
        {
            string serverCsv = @"equalto /getSome,{'ya':2},get," + (int)HttpStatusCode.Accepted;
            using (var serveMe = new ServeMe())
            {
                string url = serveMe.Start(null, serverCsv).First();
                HttpWebResponse result = (url + "/getSome").Get();
                Assert.AreEqual("{'ya':2}", result.ReadStringFromResponse());
                Assert.AreEqual(HttpStatusCode.Accepted, result.StatusCode);
            }
        }

        [TestMethod]
        public void it_can_match_where_the_path_and_query_contains_the_route_provided_and_return_json_string_with_Get()
        {
            string serverCsv = @"contains so,{'ya':2},get," + (int)HttpStatusCode.Accepted;
            using (var serveMe = new ServeMe())
            {
                string url = serveMe.Start(null, serverCsv).First();
                HttpWebResponse result = (url + "/getSome").Get();
                Assert.AreEqual("{'ya':2}", result.ReadStringFromResponse());
                Assert.AreEqual(HttpStatusCode.Accepted, result.StatusCode);
            }
        }

        [TestMethod]
        public void it_can_match_where_the_path_and_query_starts_with_the_route_provided_and_return_json_string_with_Get()
        {
            string serverCsv = @"startswith /g,{'ya':2},get," + (int)HttpStatusCode.Accepted;
            using (var serveMe = new ServeMe())
            {
                string url = serveMe.Start(null, serverCsv).First();
                HttpWebResponse result = (url + "/getSome").Get();
                Assert.AreEqual("{'ya':2}", result.ReadStringFromResponse());
                Assert.AreEqual(HttpStatusCode.Accepted, result.StatusCode);
            }
        }

        [TestMethod]
        public void it_can_match_where_the_path_and_query_ends_with_the_route_provided_and_return_json_string_with_Get()
        {
            string serverCsv = @"endswith me,{'ya':2},get," + (int)HttpStatusCode.Accepted;
            using (var serveMe = new ServeMe())
            {
                string url = serveMe.Start(null, serverCsv).First();
                HttpWebResponse result = (url + "/getSome").Get();
                Assert.AreEqual("{'ya':2}", result.ReadStringFromResponse());
                Assert.AreEqual(HttpStatusCode.Accepted, result.StatusCode);
            }
        }

        [TestMethod]
        public void it_can_match_where_the_path_and_query_contains_the_route_provided_and_return_json_string_with_get2()
        {
            string serverCsv = @"contains /g,{'ya':2},get," + (int)HttpStatusCode.Accepted;
            using (var serveMe = new ServeMe())
            {
                string url = serveMe.Start(null, serverCsv).First();
                HttpWebResponse result = (url + "/getSome").Get();
                Assert.AreEqual("{'ya':2}", result.ReadStringFromResponse());
                Assert.AreEqual(HttpStatusCode.Accepted, result.StatusCode);
            }
        }

        [TestMethod]
        public void it_can_match_where_the_path_and_query_contains_the_route_provided_and_return_json_string_with_get3()
        {
            string serverCsv = @"contains me,{'ya':2},get," + (int)HttpStatusCode.Accepted;
            using (var serveMe = new ServeMe())
            {
                string url = serveMe.Start(null, serverCsv).First();
                HttpWebResponse result = (url + "/getSome").Get();
                Assert.AreEqual("{'ya':2}", result.ReadStringFromResponse());
                Assert.AreEqual(HttpStatusCode.Accepted, result.StatusCode);
            }
        }

        [TestMethod]
        public void it_can_match_where_the_path_and_query_matches_with_regular_expression_the_route_provided_and_return_json_string_with_Get()
        {
            string serverCsv = @"regex \/getSome,{'ya':2},get," + (int)HttpStatusCode.Accepted;
            using (var serveMe = new ServeMe())
            {
                string url = serveMe.Start(null, serverCsv).First();
                HttpWebResponse result = (url + "/getSome").Get();
                Assert.AreEqual("{'ya':2}", result.ReadStringFromResponse());
                Assert.AreEqual(HttpStatusCode.Accepted, result.StatusCode);
            }
        }

        [TestMethod]
        public void it_can_match_where_the_path_and_query_is_NOT_equal_to_the_route_provided_and_return_json_string_with_Get()
        {
            string serverCsv = @"!equalto getSome,{'ya':2},get," + (int)HttpStatusCode.Accepted;
            using (var serveMe = new ServeMe())
            {
                string url = serveMe.Start(null, serverCsv).First();
                HttpWebResponse result = (url + "/getSome").Get();
                Assert.AreEqual("{'ya':2}", result.ReadStringFromResponse());
                Assert.AreEqual(HttpStatusCode.Accepted, result.StatusCode);
            }
        }

        [TestMethod]
        public void it_can_match_where_the_path_and_query_does_NOT_contain_the_route_provided_and_return_json_string_with_Get()
        {
            string serverCsv = @"!contains us,{'ya':2},get," + (int)HttpStatusCode.Accepted;
            using (var serveMe = new ServeMe())
            {
                string url = serveMe.Start(null, serverCsv).First();
                HttpWebResponse result = (url + "/getSome").Get();
                Assert.AreEqual("{'ya':2}", result.ReadStringFromResponse());
                Assert.AreEqual(HttpStatusCode.Accepted, result.StatusCode);
            }
        }

        [TestMethod]
        public void it_can_match_where_the_path_and_query_does_NOT_start_with_the_route_provided_and_return_json_string_with_Get()
        {
            string serverCsv = @"!startswith me,{'ya':2},get," + (int)HttpStatusCode.Accepted;
            using (var serveMe = new ServeMe())
            {
                string url = serveMe.Start(null, serverCsv).First();
                HttpWebResponse result = (url + "/getSome").Get();
                Assert.AreEqual("{'ya':2}", result.ReadStringFromResponse());
                Assert.AreEqual(HttpStatusCode.Accepted, result.StatusCode);
            }
        }

        [TestMethod]
        public void it_can_match_where_the_path_and_query_does_NOT_end_with_the_route_provided_and_return_json_string_with_Get()
        {
            string serverCsv = @"!endswith /ge,{'ya':2},get," + (int)HttpStatusCode.Accepted;
            using (var serveMe = new ServeMe())
            {
                string url = serveMe.Start(null, serverCsv).First();
                HttpWebResponse result = (url + "/getSome").Get();
                Assert.AreEqual("{'ya':2}", result.ReadStringFromResponse());
                Assert.AreEqual(HttpStatusCode.Accepted, result.StatusCode);
            }
        }

        [TestMethod]
        public void it_can_match_where_the_path_and_query_does_NOT_matche_with_regular_expression_the_route_provided_and_return_json_string_with_Get()
        {
            string serverCsv = @"!regex /getSome(d),{'ya':2},get," + (int)HttpStatusCode.Accepted;
            using (var serveMe = new ServeMe())
            {
                string url = serveMe.Start(null, serverCsv).First();
                HttpWebResponse result = (url + "/getSome").Get();
                Assert.AreEqual("{'ya':2}", result.ReadStringFromResponse());
                Assert.AreEqual(HttpStatusCode.Accepted, result.StatusCode);
            }
        }

        [TestMethod]
        public void it_can_match_where_the_path_and_query_matches_with_regular_expression_the_route_provided_and_return_json_string_with_get2()
        {
            string serverCsv = @"regex (.*)me,{'ya':2},get," + (int)HttpStatusCode.Accepted;
            using (var serveMe = new ServeMe())
            {
                string url = serveMe.Start(null, serverCsv).First();
                HttpWebResponse result = (url + "/getSome").Get();
                Assert.AreEqual("{'ya':2}", result.ReadStringFromResponse());
                Assert.AreEqual(HttpStatusCode.Accepted, result.StatusCode);
            }
        }
    }
}