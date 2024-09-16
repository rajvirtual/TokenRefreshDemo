using Microsoft.Identity.Client;
using System.Security.Cryptography.X509Certificates;

namespace TokenRefreshDemo
{
    public class TokenRefreshService : BackgroundService
    {
        private readonly ILogger<TokenRefreshService> _logger;
        private readonly IConfidentialClientApplication _confidentialClient;
        private string sessionKey;
        private readonly TimeSpan _maxDuration = TimeSpan.FromHours(4);
        private DateTime _startTime;
        private readonly string clientId = "024a61dc-2529-4609-99ea-40cc8ba8d756";
        private readonly string tenantId = "d6ef095c-bab3-44e5-a20c-3484b3407046";
        private readonly string redirectUri = "https://localhost:7269/callback";
        private readonly string[] scopes = { "https://management.azure.com/user_impersonation" };
        private readonly string certPath = @"C:\Dev\cert.pfx";

        public TokenRefreshService(ILogger<TokenRefreshService> logger)
        {
            _logger = logger;
            var certificate = new X509Certificate2(certPath);

            _confidentialClient = ConfidentialClientApplicationBuilder.Create(clientId)
                    .WithAuthority(AzureCloudInstance.AzurePublic, tenantId)
                    .WithRedirectUri(redirectUri)
                    .WithClientSecret("")
                    .Build();
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _startTime = DateTime.Now;
            string sessionKey = null;
            string rpOboToken = "TokenFromSC";
            try
            {
                _ = await ((ILongRunningWebApi)_confidentialClient)
                             .InitiateLongRunningProcessInWebApi(
                                  scopes,
                                  rpOboToken,
                                  ref sessionKey)
                             .WithSendX5C(true)
                             .ExecuteAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var elapsedTime = DateTime.Now - _startTime;
                    if (elapsedTime >= _maxDuration)
                    {
                        _logger.LogInformation("Service has reached the maximum duration of 4 hours. Stopping.");
                        break;
                    }

                    _logger.LogInformation("Attempting to acquire a new token at: {Time}", DateTimeOffset.Now);

                    AuthenticationResult? authResult = await ((ILongRunningWebApi)_confidentialClient)
                   .AcquireTokenInLongRunningProcess(
                        scopes,
                        sessionKey)
                   .WithSendX5C(true)
                   .ExecuteAsync();

                    string newToken = authResult.AccessToken;

                    _logger.LogInformation("Token acquired successfully at: {Time}", DateTimeOffset.Now);
                    _logger.LogInformation("Access Token: {AccessToken}", newToken);

                    await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error while refreshing token");
                }
            }

            _logger.LogInformation("TokenRefreshService has stopped.");
        }
    }
}
