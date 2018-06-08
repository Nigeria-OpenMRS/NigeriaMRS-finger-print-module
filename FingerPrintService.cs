using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.ServiceProcess;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.SelfHost;

namespace FingerPrintModule
{
    public partial class FingerPrintService : ServiceBase
    {
        HttpSelfHostServer httpServer = null;
        static string filename = string.Format("{0}//_{1:dd-MMM-yyyy}.txt", System.Configuration.ConfigurationManager.AppSettings["Logfile"], DateTime.Now);

        public FingerPrintService()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            var httpAddress = System.Configuration.ConfigurationManager.AppSettings["APIUrl"];
            var httpconfig = new HttpSelfHostConfiguration(httpAddress);

            httpconfig.MessageHandlers.Add(new CorsHandler());

            httpconfig.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "api/{controller}/{action}/{id}",
                defaults: new { id = RouteParameter.Optional }
            );
            httpServer = new HttpSelfHostServer(httpconfig);
            httpServer.OpenAsync().Wait();
        }

        protected override void OnStop()
        {
        }

        public class CorsHandler : DelegatingHandler
        {
            const string Origin = "Origin";
            const string AccessControlRequestMethod = "Access-Control-Request-Method";
            const string AccessControlRequestHeaders = "Access-Control-Request-Headers";
            const string AccessControlAllowOrigin = "Access-Control-Allow-Origin";
            const string AccessControlAllowMethods = "Access-Control-Allow-Methods";
            const string AccessControlAllowHeaders = "Access-Control-Allow-Headers";

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                bool isCorsRequest = request.Headers.Contains(Origin);
                bool isPreflightRequest = request.Method == HttpMethod.Options;
                if (isCorsRequest)
                {
                    if (isPreflightRequest)
                    {
                        HttpResponseMessage response = new HttpResponseMessage(HttpStatusCode.OK);
                        response.Headers.Add(AccessControlAllowOrigin, request.Headers.GetValues(Origin).First());

                        string accessControlRequestMethod = request.Headers.GetValues(AccessControlRequestMethod).FirstOrDefault();
                        if (accessControlRequestMethod != null)
                        {
                            response.Headers.Add(AccessControlAllowMethods, accessControlRequestMethod);
                        }

                        string requestedHeaders = string.Join(", ", request.Headers.GetValues(AccessControlRequestHeaders));
                        if (!string.IsNullOrEmpty(requestedHeaders))
                        {
                            response.Headers.Add(AccessControlAllowHeaders, requestedHeaders);
                        }

                        TaskCompletionSource<HttpResponseMessage> tcs = new TaskCompletionSource<HttpResponseMessage>();
                        tcs.SetResult(response);
                        return tcs.Task;
                    }
                    else
                    {
                        return base.SendAsync(request, cancellationToken).ContinueWith<HttpResponseMessage>(t =>
                        {
                            HttpResponseMessage resp = t.Result;
                            resp.Headers.Add(AccessControlAllowOrigin, "*");
                            return resp;
                        });
                    }
                }
                else
                {
                    return base.SendAsync(request, cancellationToken);
                }
            }
        }
    }
}
