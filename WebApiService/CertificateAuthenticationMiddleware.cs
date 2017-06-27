using System;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging;

namespace WebApiService
{
    public class CertificateAuthenticationMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger _logger;

        public CertificateAuthenticationMiddleware(RequestDelegate next, ILoggerFactory loggerFactory)
        {
            _next = next;
            _logger = loggerFactory.CreateLogger<CertificateAuthenticationMiddleware>();
        }

        public async Task Invoke(HttpContext httpContext)
        {
            // Validate the client certificate
            try
            {
                var tlsConnectionFeature = httpContext.Features.Get<ITlsConnectionFeature>();
                var certificate = await tlsConnectionFeature.GetClientCertificateAsync(httpContext.RequestAborted);
                if (certificate == null)
                {
                    _logger.LogError("Certificate cannot be found in the request.");
                    httpContext.Response.StatusCode = 401;
                    await httpContext.Response.WriteAsync("Certificate cannot be found in the request.");
                    return;
                }

                if (!IsValidClientCertificate(certificate))
                {
                    _logger.LogError("Invalid client certificate.");
                    httpContext.Response.StatusCode = 401;
                    await httpContext.Response.WriteAsync("Invalid client certificate.");
                    return;
                }

                _logger.LogInformation("Certificate validation successful.");
                await _next.Invoke(httpContext);
            }
            catch (Exception ex)
            {
                _logger.LogError("Certificate validation failed. {0}", ex);
                await httpContext.Response.WriteAsync(ex.Message);
            }
        }

        private bool IsValidClientCertificate(X509Certificate2 certificate)
        {
            // Validate the certificate using basic validation policy.
            if (!certificate.Verify())
            {
                return false;
            }

            // Check whether this certificate is installed in local certificate store
            var certCollection = LoadCertificate(certificate.Thumbprint);
            if (certCollection != null && !certCollection.Contains(certificate))
            {
                _logger.LogError("Client certificate cannot be found in local certificate store.");
                return false;
            }

            return true;
        }

        private static X509Certificate2Collection LoadCertificate(string thumbprint)
        {
            if (string.IsNullOrEmpty(thumbprint))
            {
                return null;
            }

            X509Store store = null;
            try
            {
                store = new X509Store(StoreName.My, StoreLocation.LocalMachine);
                store.Open(OpenFlags.OpenExistingOnly | OpenFlags.ReadOnly);

                // select the certificate from store
                return store.Certificates.Find(X509FindType.FindByThumbprint, thumbprint, true);
            }
            finally
            {
                if (store != null)
                {
                    store.Close();
                }
            }
        }
    }
}
