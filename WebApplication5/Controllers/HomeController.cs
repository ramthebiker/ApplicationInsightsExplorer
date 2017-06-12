using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Web;
using System.Web.Mvc;
using WebApplication5.Models;

namespace WebApplication5.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Metrics()
        {
            if (Session["ID"] != null)
            {
                return View();
            }
            else
            {
                return RedirectToAction("Login");
            }
        }

        public ActionResult Events(string abc)
        {
            if (Session["ID"] != null)
            {
               // string response = GetTelemetry()
                return View();
            }
            else
            {
                return RedirectToAction("Login");
            }
        }

        [HttpPost]
        [MultipleButton(Name = "action", Argument = "GetEventLogs")]
        public ActionResult GetEventLogs(string eventsID)
        {
           string response = GetTelemetry("events", eventsID, string.Empty);
            ViewBag.Response = response;
            return View("Events");
        }

        [HttpPost]
        [MultipleButton(Name = "action", Argument = "DownloadEventLogs")]
        public void DownloadEventLogs(string eventsID)
        {           
                string response = GetTelemetry("events", eventsID, string.Empty);
                ViewBag.Response = response;

                StringBuilder sb = new StringBuilder();
                string output = response;
                sb.Append(output);
                sb.Append("\r\n");
                string text = sb.ToString();

                Response.Clear();
                Response.ClearHeaders();
                Response.AddHeader("Content-Length", text.Length.ToString());
                Response.ContentType = "text/plain";

                Response.AppendHeader("content-disposition", "attachment;filename=\"AppInsightsEventLogs-" + DateTime.Now.ToString() + ".txt\"");
                using (StreamWriter writer = new StreamWriter(Response.OutputStream, Encoding.UTF8))
                {
                    writer.Write(text);
                }
                Response.End();
            
            
        }

        [HttpPost]
        [MultipleButton(Name = "action", Argument = "GetMetricsLogs")]
        public ActionResult GetMetricsLogs(string metricsID, string timespan, string interval)
        {
            string query = "timespan=" + timespan + "&" + "interval=" + interval;
            string response = GetTelemetry("metrics", metricsID, query);
            ViewBag.Response = response;
            return View("Metrics");
        }

        [HttpPost]
        [MultipleButton(Name = "action", Argument = "DownloadMetricsLogs")]
        public void DownloadMetricsLogs(string metricsID, string timespan, string interval)
        {
            string query = "timespan=" + timespan + "&" + "interval=" + interval;
            string response = GetTelemetry("metrics", metricsID, query);
            ViewBag.Response = response;

            StringBuilder sb = new StringBuilder();
            string output = response;
            sb.Append(output);
            sb.Append("\r\n");
            string text = sb.ToString();

            Response.Clear();
            Response.ClearHeaders();
            Response.AddHeader("Content-Length", text.Length.ToString());
            Response.ContentType = "text/plain";
            Response.AppendHeader("content-disposition", "attachment;filename=\"AppInsightsMetricsLogs-" + DateTime.Now.ToString() + ".txt\"");
            using (StreamWriter writer = new StreamWriter(Response.OutputStream, Encoding.UTF8))
            {
                writer.Write(text);
            }
            Response.End();
        }

        [HttpPost]
        [MultipleButton(Name = "action", Argument = "GetQueryLogs")]
        public ActionResult GetQueryLogs(string timespan, string query)
        {
            string request = "timespan=" + timespan + "&" + "query=" + query;
            string response = GetTelemetry("query", string.Empty, request);
            ViewBag.Response = response;
            return View("Query");
        }

        [HttpPost]
        [MultipleButton(Name = "action", Argument = "DownloadQueryLogs")]
        public void DownloadQueryLogs(string timespan, string query)
        {
            string request = "timespan=" + timespan + "&" + "query=" + query;
            string response = GetTelemetry("query", string.Empty, request);
            ViewBag.Response = response;

            StringBuilder sb = new StringBuilder();
            string output = response;
            sb.Append(output);
            sb.Append("\r\n");
            string text = sb.ToString();

            Response.Clear();
            Response.ClearHeaders();
            Response.AddHeader("Content-Length", text.Length.ToString());
            Response.ContentType = "text/plain";
            Response.AppendHeader("content-disposition", "attachment;filename=\"AppInsightsQueryLogs-" + DateTime.Now.ToString() + ".txt\"");
            using (StreamWriter writer = new StreamWriter(Response.OutputStream, Encoding.UTF8))
            {
                writer.Write(text);
            }
            Response.End();
        }

        private const string URL = "https://api.applicationinsights.io/beta/apps/{0}/{1}/{2}?{3}";

        public string GetTelemetry( string queryType, string queryPath, string parameterString)
        {
            if ((Session["ID"] != null) && (Session["Key"] != null))
            {
                HttpClient client = new HttpClient();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                client.DefaultRequestHeaders.Add("x-api-key", Convert.ToString(Session["Key"]));
                var req = string.Format(URL, Convert.ToString(Session["ID"]), queryType, queryPath, parameterString);
                HttpResponseMessage response = client.GetAsync(req).Result;
                if (response.IsSuccessStatusCode)
                {          
                    string jsonFormatted = JValue.Parse(response.Content.ReadAsStringAsync().Result).ToString(Formatting.Indented);
                    return jsonFormatted;
                }
                else
                {
                    return response.ReasonPhrase;
                }
            }
            else
            {
                return string.Empty;
            }

        }

        public ActionResult Query()
        {
            if (Session["ID"] != null)
            {
                return View();
            }
            else
            {
                return RedirectToAction("Login");
            }
        }

        public ActionResult Login()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Login(ProjectProfile objUser)
        {
            if (!String.IsNullOrEmpty(objUser.Name) && !String.IsNullOrEmpty(objUser.Key))
            {                
                Session["ID"] = objUser.Name.ToString();
                Session["Key"] = objUser.Key.ToString();

                try
                {
                    HttpClient client = new HttpClient();
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    client.DefaultRequestHeaders.Add("x-api-key", objUser.Key.ToString());
                    var req = "https://api.applicationinsights.io/beta/apps/" + objUser.Name.ToString() + "/metrics/requests/count";
                    HttpResponseMessage response = client.GetAsync(req).Result;
                    if (response.IsSuccessStatusCode)
                    {
                        ViewBag.Error = string.Empty;
                        return RedirectToAction("Events");
                    }
                    else
                    {
                        ViewBag.Error = "Invalid ID or Key";
                    }
                }
                catch(Exception ex)
                {
                    ViewBag.Error = "Server Down! Try later.";
                }
               
            }
            return View(objUser);
        }

       
    }
}

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public class MultipleButtonAttribute : ActionNameSelectorAttribute
{
    public string Name { get; set; }
    public string Argument { get; set; }

    public override bool IsValidName(ControllerContext controllerContext, string actionName, MethodInfo methodInfo)
    {
        var isValidName = false;
        var keyValue = string.Format("{0}:{1}", Name, Argument);
        var value = controllerContext.Controller.ValueProvider.GetValue(keyValue);

        if (value != null)
        {
            controllerContext.Controller.ControllerContext.RouteData.Values[Name] = Argument;
            isValidName = true;
        }

        return isValidName;
    }

   
}