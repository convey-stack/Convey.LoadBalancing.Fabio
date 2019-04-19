using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Polly;

namespace Convey.LoadBalancing.Fabio.MessageHandlers
{
    internal sealed class FabioMessageHandler : DelegatingHandler
    {
        private readonly FabioOptions _options;
        private readonly string _servicePath;

        public FabioMessageHandler(FabioOptions options, string serviceName = null)
        {
            if (string.IsNullOrWhiteSpace(options.Url))
            {
                throw new InvalidOperationException("Fabio URL was not provided.");
            }

            _options = options;
            _servicePath = string.IsNullOrWhiteSpace(serviceName) ? string.Empty : $"{serviceName}/";
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            request.RequestUri = GetRequestUri(request);

            return await Policy.Handle<Exception>()
                .WaitAndRetryAsync(RequestRetries, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)))
                .ExecuteAsync(async () => await base.SendAsync(request, cancellationToken));
        }

        private Uri GetRequestUri(HttpRequestMessage request)
            =>  new Uri($"{_options.Url}/{_servicePath}{request.RequestUri.Host}{request.RequestUri.PathAndQuery}");
        
        private int RequestRetries => _options.RequestRetries <= 0 ? 3 : _options.RequestRetries;
    }
}