# ServiceFabric.Samples.WebApi
ASP.Net Core based Service Fabric Web API service with Client Certificate Authentication.

# Implementation
This sample implements a Service Fabric stateless frontend web api service with support for client certificate authentication.

Client Certificate Authentication is implemented by defining a custom middleware that uses [ITlsConnectionFeature](https://docs.microsoft.com/en-us/aspnet/core/api/microsoft.aspnetcore.http.features.tlsconnectionfeature) to retrieve and validate client certificate passed in the http request. 

## CertificateAuthenticationMiddleware.cs
```csharp
        public async Task Invoke(HttpContext httpContext)
        {
            var tlsConnectionFeature = httpContext.Features.Get<ITlsConnectionFeature>();
            var certificate = await tlsConnectionFeature.GetClientCertificateAsync(httpContext.RequestAborted);

            // Validate the certificate here
        }
```

And then inject this custom middleware in Startup.cs

## Startup.cs
```csharp
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddConsole(Configuration.GetSection("Logging"));
            loggerFactory.AddDebug();

            app.UseMiddleware<CertificateAuthenticationMiddleware>();
            app.UseMvc();
        }
```

### Note
*This implementation assumes Client Certificate will always be passed by the client along with Http Request. If you need client certificate negotiation, you will need additional setup.*
