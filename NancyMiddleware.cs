using Microsoft.AspNetCore.Http;
using Nancy.Helpers;
using Nancy.IO;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace Nancy.AspNetCore {
    public class NancyMiddleware {

        public const string AspNetCoreEnvironmentKey = "ASPNETCORE_ENVIRONMENT";
        public const string NancyEnvironmentKey = "NANCY_ENVIRONMENT";

        private readonly RequestDelegate _next;

        private NancyOptions _options;
        private INancyEngine _engine;

        public NancyMiddleware(RequestDelegate next, NancyOptions options) {
            _next = next;

            _options = options;
            _options.Bootstrapper.Initialise();
            _engine = _options.Bootstrapper.GetEngine();
        }

        public async Task Invoke(HttpContext environment) {
            var aspnetCoreRequestMethod = environment.Request.Method;
            var aspnetCoreRequestScheme = environment.Request.Scheme;
            var aspnetCoreRequestHeaders = environment.Request.Headers;
            var aspnetCoreRequestPathBase = environment.Request.PathBase;
            var aspnetCoreRequestPath = environment.Request.Path;
            var aspnetCoreRequestQueryString = environment.Request.QueryString.Value ?? string.Empty;
            var aspnetCoreRequestBody = environment.Request.Body;
            var aspnetCoreRequestProtocol = environment.Request.Protocol;
            var aspnetCoreCallCancelled = environment.RequestAborted;
            var aspnetCoreRequestHost = environment.Request.Host.Value ?? Dns.GetHostName();
            var aspnetCoreUser = environment.User;

            X509Certificate2 certificate = null;
            if (_options.EnableClientCertificates) {
                var clientCertificate = new X509Certificate2(environment.Connection.ClientCertificate.Export(X509ContentType.Cert));
                certificate = clientCertificate ?? null;
            }

            var serverClientIp = environment.Connection.RemoteIpAddress.ToString();

            var url = CreateUrl(aspnetCoreRequestHost, aspnetCoreRequestScheme, aspnetCoreRequestPathBase, aspnetCoreRequestPath, aspnetCoreRequestQueryString);

            var nancyRequestStream = new RequestStream(aspnetCoreRequestBody, ExpectedLength(aspnetCoreRequestHeaders), StaticConfiguration.DisableRequestStreamSwitching ?? false);

            var nancyRequest = new Request(
                    aspnetCoreRequestMethod,
                    url,
                    nancyRequestStream,
                    aspnetCoreRequestHeaders.ToDictionary(kv => kv.Key, kv => (IEnumerable<string>)kv.Value, StringComparer.OrdinalIgnoreCase),
                    serverClientIp,
                    certificate,
                    aspnetCoreRequestProtocol);

            var nancyContext = await _engine.HandleRequest(
                nancyRequest,
                StoreEnvironment(environment, aspnetCoreUser),
                aspnetCoreCallCancelled).ConfigureAwait(false);

            await RequestComplete(nancyContext, environment, _options.PerformPassThrough, _next).ConfigureAwait(false);
        }

        private static Task RequestComplete(
            NancyContext context,
            HttpContext environment,
            Func<NancyContext, bool> performPassThrough,
            RequestDelegate next) {
            var aspnetCoreResponseHeaders = environment.Response.Headers;
            var aspnetCoreResponseBody = environment.Response.Body;

            var nancyResponse = context.Response;
            if (!performPassThrough(context)) {
                environment.Response.StatusCode = (int)nancyResponse.StatusCode;

                if (nancyResponse.ReasonPhrase != null) {
                    environment.Response.Headers.Add("ReasonPhrase", nancyResponse.ReasonPhrase);
                }

                foreach (var responseHeader in nancyResponse.Headers) {
                    aspnetCoreResponseHeaders[responseHeader.Key] = new[] { responseHeader.Value };
                }

                if (!string.IsNullOrWhiteSpace(nancyResponse.ContentType)) {
                    aspnetCoreResponseHeaders["Content-Type"] = new[] { nancyResponse.ContentType };
                }

                if (nancyResponse.Cookies != null && nancyResponse.Cookies.Count != 0) {
                    const string setCookieHeaderKey = "Set-Cookie";
                    string[] setCookieHeader = aspnetCoreResponseHeaders.ContainsKey(setCookieHeaderKey)
                                                    ? aspnetCoreResponseHeaders[setCookieHeaderKey].ToArray()
                                                    : ArrayCache.Empty<string>();
                    aspnetCoreResponseHeaders[setCookieHeaderKey] = setCookieHeader
                        .Concat(nancyResponse.Cookies.Select(cookie => cookie.ToString()))
                        .ToArray();
                }

                nancyResponse.Contents(aspnetCoreResponseBody);
            } else {
                return next(environment);
            }

            context.Dispose();

            return TaskHelpers.CompletedTask;
        }

        private static long ExpectedLength(IHeaderDictionary headers) {
            var header = headers["Content-Length"];
            if (string.IsNullOrWhiteSpace(header))
                return 0;

            int contentLength;
            return int.TryParse(header, NumberStyles.Any, CultureInfo.InvariantCulture, out contentLength) ? contentLength : 0;
        }

        private static Url CreateUrl(
           string aspnetCoreRequestHost,
           string aspnetCoreRequestScheme,
           string aspnetCoreRequestPathBase,
           string aspnetCoreRequestPath,
           string aspnetCoreRequestQueryString) {
            int? port = null;

            var hostnameParts = aspnetCoreRequestHost.Split(':');
            if (hostnameParts.Length == 2) {
                aspnetCoreRequestHost = hostnameParts[0];

                int tempPort;
                if (int.TryParse(hostnameParts[1], out tempPort)) {
                    port = tempPort;
                }
            }

            var url = new Url {
                Scheme = aspnetCoreRequestScheme,
                HostName = aspnetCoreRequestHost,
                Port = port,
                BasePath = aspnetCoreRequestPathBase,
                Path = aspnetCoreRequestPath,
                Query = aspnetCoreRequestQueryString,
            };
            return url;
        }

        private static Func<NancyContext, NancyContext> StoreEnvironment(HttpContext environment, ClaimsPrincipal user) {
            return context => {
                context.CurrentUser = user;
                environment.Items[NancyEnvironmentKey] = context;
                context.Items[AspNetCoreEnvironmentKey] = environment;

                return context;
            };
        }
    }
}
