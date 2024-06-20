using Microsoft.Extensions.Options;

namespace TheWebApiServer.Services
{
    public class PayPalClient
    {
        /* private readonly PayPalSettings _payPalSettings;

         public PayPalClient(IOptions<PayPalSettings> payPalSettings)
         {
             _payPalSettings = payPalSettings.Value;
         }

         public PayPalEnvironment GetEnvironment()
         {
             return _payPalSettings.Environment.ToLower() == "live"
                 ? (PayPalEnvironment)new LiveEnvironment(_payPalSettings.ClientId, _payPalSettings.ClientSecret)
                 : new SandboxEnvironment(_payPalSettings.ClientId, _payPalSettings.ClientSecret);
         }

         public PayPalHttpClient GetClient()
         {
             return new PayPalHttpClient(GetEnvironment());
         }
     }
     public class PayPalSettings
     {
         public string ClientId { get; set; }
         public string ClientSecret { get; set; }
         public string Environment { get; set; }
     }*/
    }
}
