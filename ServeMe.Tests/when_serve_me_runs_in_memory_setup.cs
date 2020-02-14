using System;

namespace ServeMe.Tests
{
    using System.IO;
    using System.Linq;
    using System.Net;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using ServeMeLib;

    [TestClass]
    public class when_serve_me_runs_in_memory_setup
    {
        public string DoSomething(string arg)
        {
            return "yo " + arg;
        }

        [TestMethod]
        public void execute_a_function_in_assembly()
        {
            string methodName = nameof(this.DoSomething);
            string arg = "w";

            string instruction = ServeMe.GetMethodExecutionInstruction(this.GetType(), methodName, arg);

            string serverCsv = @"getSome,assembly " + instruction + ",get";
            //getSome,assembly file:///D:/ServeMe.Tests/bin/Debug/ServeMe.Tests.DLL ServeMe.Tests.when_serve_me_runs DoSomething w,get
            using (var serveMe = new ServeMe())
            {
                string url = serveMe.Start().First();
                serveMe.AppendToInMemoryConfiguration(serverCsv);
                HttpWebResponse result = (url + "/getSome").Get();
                string finalResult = result.ReadStringFromResponse().Trim().ToLower();
                Assert.IsTrue(finalResult == "yo " + arg);
                Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
            }
        }

        [TestMethod]
        public void return_maprequestpathandquerytolink2()
        {
            string serverCsv = "equalto /search?q=hello,appendtolink http://www.google.com,get\napp log";
            using (var serveMe = new ServeMe())
            {
                string url = serveMe.Start().First();
                serveMe.AppendToInMemoryConfiguration(serverCsv);
                HttpWebResponse result = (url + "/search?q=hello").Get();
                string finalResult = result.ReadStringFromResponse().Trim().ToLower();
                Assert.IsTrue(finalResult.StartsWith("<!doc"));
                Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
            }
        }
        [TestMethod]
        public void return_link_as_json_variables()
        {
            string serverCsv = "app var, x=www.google.com; \n getSome,json http://{{x}},get";
            using (var serveMe = new ServeMe())
            {
                string url = serveMe.Start().First();
                serveMe.AppendToInMemoryConfiguration(serverCsv);
                HttpWebResponse result = (url + "/getSome").Get();
                string finalResult = result.ReadStringFromResponse().Trim().ToLower();
                Assert.IsTrue(finalResult == "http://www.google.com");
                Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
            }
        }
        [TestMethod]
        public void variablesTest()
        {
            string serverCsv = "app var , x=sample.js; y=2; z=3; \n contains /{{x}}, /sample/{{5}}";
            using (var serveMe = new ServeMe())
            {
                string url = serveMe.Start().First();
                serveMe.AppendToInMemoryConfiguration(serverCsv);
                HttpWebResponse result = (url + "/boo/loud/sample.js?q=hello").Get();
                string finalResult = result.ReadStringFromResponse().Trim().ToLower();
                Assert.IsTrue(finalResult.StartsWith("<!doc"));
                Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
            }
        }
        [TestMethod]
        public void return_maprequestpathandquerytolink3()
        {
            string serverCsv = "contains /sample.js, /sample/{{5}}";
            using (var serveMe = new ServeMe())
            {
                string url = serveMe.Start().First();
                serveMe.AppendToInMemoryConfiguration(serverCsv);
                HttpWebResponse result = (url + "/boo/loud/sample.js?q=hello").Get();
                string finalResult = result.ReadStringFromResponse().Trim().ToLower();
                Assert.IsTrue(finalResult.StartsWith("<!doc"));
                Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
            }
        }
        [TestMethod]
        public void return_maprequestpathandquerytolink()
        {
            string serverCsv = "contains /sample.js,appendtolink /sample/{{5}}";
            using (var serveMe = new ServeMe())
            {
                string url = serveMe.Start().First();
                serveMe.AppendToInMemoryConfiguration(serverCsv);
                HttpWebResponse result = (url + "/boo/loud/sample.js?q=hello").Get();
                string finalResult = result.ReadStringFromResponse().Trim().ToLower();
                Assert.IsTrue(finalResult.StartsWith("<!doc"));
                Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
            }
        }


        [TestMethod]
        public void fails_on_a_post()
        {
            try
            {
                string serverCsv = @"getSome,http://www.google.com,post";
                using (var serveMe = new ServeMe())
                {
                    string url = serveMe.Start().First();
                    serveMe.AppendToInMemoryConfiguration(serverCsv);
                    HttpWebResponse result = (url + "/getSome").Post();
                    string finalResult = result.ReadStringFromResponse().Trim().ToLower();
                    Assert.IsTrue(finalResult.StartsWith("<!doc"));
                    Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
                }
                Assert.Fail();
            }
            catch (Exception e)
            {
              
            }
        }
        [TestMethod]
        public void turns_arround_and_get_when_it_would_have_failed_on_a_post()
        {
                string serverCsv = @"getSome,http://www.google.com,post - get";
                using (var serveMe = new ServeMe())
                {
                    string url = serveMe.Start().First();
                    serveMe.AppendToInMemoryConfiguration(serverCsv);
                    HttpWebResponse result = (url + "/getSome").Post();
                    string finalResult = result.ReadStringFromResponse().Trim().ToLower();
                    Assert.IsTrue(finalResult.StartsWith("<!doc"));
                    Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
                }
        }
        [TestMethod]
        public void return_jsonp()
        {
            string serverCsv = @"getSome,http://www.google.com,get | jsonp";
            using (var serveMe = new ServeMe())
            {
                string url = serveMe.Start().First();
                serveMe.AppendToInMemoryConfiguration(serverCsv);
                HttpWebResponse result = (url + "/getSome?callback=booo").Get();
                string finalResult = result.ReadStringFromResponse().Trim().ToLower();
                Assert.IsTrue(finalResult.StartsWith("booo(<!doc"));
                Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
            }
        }
        [TestMethod]
        public void return_jsonp2()
        {
            string serverCsv = @"getSome,http://www.google.com,getjsonp";
            using (var serveMe = new ServeMe())
            {
                string url = serveMe.Start().First();
                serveMe.AppendToInMemoryConfiguration(serverCsv);
                HttpWebResponse result = (url + "/getSome?callback=booo").Get();
                string finalResult = result.ReadStringFromResponse().Trim().ToLower();
                Assert.IsTrue(finalResult.StartsWith("booo(<!doc"));
                Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
            }
        }
        [TestMethod]
        public void understand_query_parts()
        {
            string serverCsv = @"startswith / ,json {{0}}/{{scheme}}/{{3}}/{{4}}/{{5}}/{{file}}/{{6}}/{{query}}/{{extension}}/{{pathandquery}}/{{path}},get ";
            using (var serveMe = new ServeMe())
            {
                string url = serveMe.Start().First();
                serveMe.AppendToInMemoryConfiguration(serverCsv);
                HttpWebResponse result = (url + "/let/us/go.php?w=tree").Get();
                string finalResult = result.ReadStringFromResponse().Trim().ToLower();
                Assert.IsTrue(finalResult == "http/http/let/us/go.php/go.php/w=tree/w=tree/.php//let/us/go.php?w=tree//let/us/go.php");
                Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
            }
        }
        [TestMethod]
        public void understand_query_parts2()
        {
            string serverCsv = @"startswith / ,{{scheme}}://{{3}}.{{4}}.{{5}},get ";
            using (var serveMe = new ServeMe())
            {
                string url = serveMe.Start().First();
                serveMe.AppendToInMemoryConfiguration(serverCsv);
                HttpWebResponse result = (url + "/www/google/com/us/go.php?w=tree").Get();
                string finalResult = result.ReadStringFromResponse().Trim().ToLower();
                Assert.IsTrue(finalResult.StartsWith("<!doc"));
                Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
            }
        }
        [TestMethod]
        public void understand_query_parts3_on_steriods()
        {
            string serverCsv = @"{{6}} / ,{{scheme}}://{{3}}.{{4}}.{{5}} , {{7}} ";
            using (var serveMe = new ServeMe())
            {
                string url = serveMe.Start().First();
                serveMe.AppendToInMemoryConfiguration(serverCsv);
                HttpWebResponse result = (url + "/www/google/com/startswith/get/us/go.php?w=tree").Get();
                string finalResult = result.ReadStringFromResponse().Trim().ToLower();
                Assert.IsTrue(finalResult.StartsWith("<!doc"));
                Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
            }
        }
        [TestMethod]
        public void return_link_as_json()
        {
            string serverCsv = @"getSome,json http://www.google.com,get";
            using (var serveMe = new ServeMe())
            {
                string url = serveMe.Start().First();
                serveMe.AppendToInMemoryConfiguration(serverCsv);
                HttpWebResponse result = (url + "/getSome").Get();
                string finalResult = result.ReadStringFromResponse().Trim().ToLower();
                Assert.IsTrue(finalResult == "http://www.google.com");
                Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
            }
        }

        [TestMethod]
        public void it_can_connect_with_cookie_auth()
        {
            string serverCsv = @"getSome,http://www.google.com auth cookie MYCOOKIE MYOHMYOHMY,get";
            using (var serveMe = new ServeMe())
            {
                string url = serveMe.Start().First();
                serveMe.AppendToInMemoryConfiguration(serverCsv);
                HttpWebResponse result = (url + "/getSome").Get();
                string finalResult = result.ReadStringFromResponse().Trim().ToLower();
                Assert.IsTrue(finalResult.StartsWith("<!doc"));
                Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
            }
        }

        [TestMethod]
        public void it_can_connect_with_basic_auth()
        {
            string serverCsv = @"getSome,http://www.google.com auth basic Sam@me.com password%*!,get";
            using (var serveMe = new ServeMe())
            {
                string url = serveMe.Start().First();
                serveMe.AppendToInMemoryConfiguration(serverCsv);
                HttpWebResponse result = (url + "/getSome").Get();
                string finalResult = result.ReadStringFromResponse().Trim().ToLower();
                Assert.IsTrue(finalResult.StartsWith("<!doc"));
                Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
            }
        }

        [TestMethod]
        public void it_can_return_external_network_resource_with_get_and_ok_status_code()
        {
            string serverCsv = @"getSome,http://www.google.com,get,200";
            using (var serveMe = new ServeMe())
            {
                string url = serveMe.Start().First();
                serveMe.AppendToInMemoryConfiguration(serverCsv);
                HttpWebResponse result = (url + "/getSome").Get();
                string finalResult = result.ReadStringFromResponse().Trim().ToLower();
                Assert.IsTrue(finalResult.StartsWith("<!doc"));
                Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
            }
        }

        [TestMethod]
        public void it_can_write_response_to_file()
        {
            //use saveasserved to save with same file name as served
            string serverCsv = @"getSome,http://www.google.com,get,200,save data.json find replace";
            using (var serveMe = new ServeMe())
            {
                if (File.Exists("data.json"))
                    File.Delete("data.json");
                Assert.IsFalse(File.Exists("data.json"));

                string url = serveMe.Start().First();
                serveMe.AppendToInMemoryConfiguration(serverCsv);
                HttpWebResponse result = (url + "/getSome").Get();
                string finalResult = result.ReadStringFromResponse().Trim().ToLower();
                Assert.IsTrue(finalResult.StartsWith("<!doc"));
                Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
                Assert.IsTrue(File.Exists("data.json"));
                Assert.IsTrue(File.ReadAllText("data.json").StartsWith("<!doc"));
                if (File.Exists("data.json"))
                    File.Delete("data.json");
                Assert.IsFalse(File.Exists("data.json"));
            }
        }

        [TestMethod]
        public void it_can_load_settings_from_another_file()
        {
            string serverCsv = @"app LoadSettingsFromFile,settings.txt";
            using (var serveMe = new ServeMe())
            {
                string url = serveMe.Start(serverCsv, fileExists: fn => true, readAllTextFromFile: fn => fn == "settings.txt" ? "getSome,{'ya':1},get,200" : "").First();
                HttpWebResponse result = (url + "/getSome").Get();
                Assert.AreEqual("{'ya':1}", result.ReadStringFromResponse());
                Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
            }
        }

        [TestMethod]
        public void it_can_load_settings_from_another_file_with_logging()
        {
            string serverCsv = "app LoadSettingsFromFile,settings.txt";
            using (var serveMe = new ServeMe())
            {
                string url = serveMe.Start(serverCsv, fileExists: fn => true, readAllTextFromFile: fn => fn == "settings.txt" ? "getSome,{'ya':1},get,200\napp log" : "").First();
                HttpWebResponse result = (url + "/getSome").Get();
                Assert.AreEqual("{'ya':1}", result.ReadStringFromResponse());
                Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
            }
        }

        [TestMethod]
        public void it_can_return_json_string_provided_inline_with_get_and_ok_status_code()
        {
            string serverCsv = @"getSome,{'ya':1},get,200";
            using (var serveMe = new ServeMe())
            {
                string url = serveMe.Start().First();
                serveMe.AppendToInMemoryConfiguration(serverCsv);
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
                string url = serveMe.Start().First();
                serveMe.AppendToInMemoryConfiguration(serverCsv);
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
                string url = serveMe.Start().First();
                serveMe.AppendToInMemoryConfiguration(serverCsv);
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
                string url = serveMe.Start().First();
                serveMe.AppendToInMemoryConfiguration(serverCsv);
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
                string url = serveMe.Start().First();
                serveMe.AppendToInMemoryConfiguration(serverCsv);
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
                string url = serveMe.Start().First();
                serveMe.AppendToInMemoryConfiguration(serverCsv);
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
                string url = serveMe.Start().First();
                serveMe.AppendToInMemoryConfiguration(serverCsv);
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
                string url = serveMe.Start().First();
                serveMe.AppendToInMemoryConfiguration(serverCsv);
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
                string url = serveMe.Start().First();
                serveMe.AppendToInMemoryConfiguration(serverCsv);
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
                string url = serveMe.Start().First();
                serveMe.AppendToInMemoryConfiguration(serverCsv);
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
                string url = serveMe.Start().First();
                serveMe.AppendToInMemoryConfiguration(serverCsv);
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
                string url = serveMe.Start().First();
                serveMe.AppendToInMemoryConfiguration(serverCsv);
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
                string url = serveMe.Start().First();
                serveMe.AppendToInMemoryConfiguration(serverCsv);
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
                string url = serveMe.Start().First();
                serveMe.AppendToInMemoryConfiguration(serverCsv);
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
                string url = serveMe.Start().First();
                serveMe.AppendToInMemoryConfiguration(serverCsv);
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
                string url = serveMe.Start().First();
                serveMe.AppendToInMemoryConfiguration(serverCsv);
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
                string url = serveMe.Start().First();
                serveMe.AppendToInMemoryConfiguration(serverCsv);
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
                string url = serveMe.Start().First();
                serveMe.AppendToInMemoryConfiguration(serverCsv);
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
                string url = serveMe.Start().First();
                serveMe.AppendToInMemoryConfiguration(serverCsv);
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
                string url = serveMe.Start().First();
                serveMe.AppendToInMemoryConfiguration(serverCsv);
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
                string url = serveMe.Start().First();
                serveMe.AppendToInMemoryConfiguration(serverCsv);
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
                string url = serveMe.Start().First();
                serveMe.AppendToInMemoryConfiguration(serverCsv);
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
                string url = serveMe.Start().First();
                serveMe.AppendToInMemoryConfiguration(serverCsv);
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
                string url = serveMe.Start().First();
                serveMe.AppendToInMemoryConfiguration(serverCsv);
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
                string url = serveMe.Start().First();
                serveMe.AppendToInMemoryConfiguration(serverCsv);
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
                string url = serveMe.Start().First();
                serveMe.AppendToInMemoryConfiguration(serverCsv);
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
                string url = serveMe.Start().First();
                serveMe.AppendToInMemoryConfiguration(serverCsv);
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
                string url = serveMe.Start().First();
                serveMe.AppendToInMemoryConfiguration(serverCsv);
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
                string url = serveMe.Start().First();
                serveMe.AppendToInMemoryConfiguration(serverCsv);
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
                string url = serveMe.Start().First();
                serveMe.AppendToInMemoryConfiguration(serverCsv);
                HttpWebResponse result = (url + "/getSome").Get();
                Assert.AreEqual("{'ya':2}", result.ReadStringFromResponse());
                Assert.AreEqual(HttpStatusCode.Accepted, result.StatusCode);
            }
        }
    }
}