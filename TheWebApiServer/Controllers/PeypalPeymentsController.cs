using System.Globalization;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json.Nodes;
using Google;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using TheWebApiServer.Data;
using TheWebApiServer.Model;
using TheWebApiServer.Requests;
using TheWebApiServer.Services;

namespace TheWebApiServer.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class PeypalPeymentsController : Controller
    {
        private readonly DataContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        public PeypalPeymentsController(DataContext context, UserManager<IdentityUser> userManager)
        {

            _context = context;
            _userManager = userManager;
        }

        [HttpGet("GetUserAmount")]
        [Authorize]
        public async Task<IActionResult> GetUserAmount()
        {
            var user = await _userManager.GetUserAsync(HttpContext.User);

            if (user == null)
                return BadRequest("Invalid User");

            var userAmount=await _context.treasure.Where(x => x.User==user).Select(x=>x.Amount).FirstOrDefaultAsync();
            
            return Ok(userAmount);
        }

        [HttpPost("create-order")]
        [Authorize]
        public async Task<IActionResult> CreateOrder([FromBody] Cart cart)
        {
            if(cart==null || cart.sku==null || cart.quantity == null)
            {
                return BadRequest("invalidData");
            }

            JsonObject createOrderRequest = new JsonObject();
            createOrderRequest.Add("intent", "CAPTURE");

            JsonObject amount = new JsonObject();
            amount.Add("currency_code", "PLN");
            amount.Add("value", cart.quantity);
            JsonObject purchaseUnit1 = new JsonObject();
            purchaseUnit1.Add("amount", amount); // Change here: Use amount object instead of plain value
            JsonArray purchaseUnits = new JsonArray();
            purchaseUnits.Add(purchaseUnit1);
            createOrderRequest.Add("purchase_units", purchaseUnits);

            var accessToken = GetPeypalAccessToken();

            string Url = "https://api-m.sandbox.paypal.com/v2/checkout/orders";
            string orderId = "";

            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("Authorization", "Bearer " + accessToken); // Change here: Added space after Bearer

                var requestMessage = new HttpRequestMessage(HttpMethod.Post, Url);
                requestMessage.Content = new StringContent(createOrderRequest.ToString(), Encoding.UTF8, "application/json"); // Change here: Added Encoding.UTF8

                var responseTask = client.SendAsync(requestMessage);
                responseTask.Wait();

                var result = responseTask.Result;
                if (result.IsSuccessStatusCode)
                {
                    var readTask = result.Content.ReadAsStringAsync();
                    readTask.Wait();

                    var strResponse = readTask.Result;
                    var jsonResponse = JsonNode.Parse(strResponse);
                    if (jsonResponse != null)
                    {
                        orderId = jsonResponse["id"]?.ToString() ?? "";
                    }
                }
                
            }
            return Ok(orderId);
        }


        [HttpPost("OnPostCompleteOrder")]
        [Authorize]
        public async Task<IActionResult> OnPostCompleteOrder([FromBody] JsonObject data)
        {
            if (data == null || data["orderID"] == null)
            {
                return BadRequest("InvalidValue");
            }
            string orderID = data["orderID"].ToString();
            
            string accessToken = GetPeypalAccessToken();

            string url = "https://api-m.sandbox.paypal.com/v2/checkout/orders/" + orderID + "/capture"; // Change here: Added "/" before orderID

            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("Authorization", "Bearer " + accessToken); // Change here: Added space after Bearer

                var requestMessage = new HttpRequestMessage(HttpMethod.Post, url);
                requestMessage.Content = new StringContent("",null, "application/json"); // Change here: Added Encoding.UTF8

                var responseTask = client.SendAsync(requestMessage);
                responseTask.Wait();

                var result = responseTask.Result;
                if (result.IsSuccessStatusCode)
                {
                    var readTask = result.Content.ReadAsStringAsync();
                    readTask.Wait();

                    var strResponse = readTask.Result;
                    var jsonResponse = JsonNode.Parse(strResponse);
                    if (jsonResponse != null)
                    {
                        string paypalResult = jsonResponse["status"]?.ToString() ?? "";


                        if (paypalResult.Equals("COMPLETED", StringComparison.OrdinalIgnoreCase))
                        {
                            string ammountStr = jsonResponse["purchase_units"][0]["payments"]["captures"][0]["amount"]["value"].ToString();

                            double amount = Convert.ToDouble(ammountStr, CultureInfo.InvariantCulture);
                            /* double amount = double.Parse(ammountStr);*/
                            var user = await _userManager.GetUserAsync(HttpContext.User);

                            if (user == null)
                                return BadRequest("Invalid User");

                            var curTreasure=await _context.treasure.FirstOrDefaultAsync(x=>x.UserId==user.Id);

                            curTreasure!.Amount += (int)amount;

                            _context.Update(curTreasure);
                            await _context.SaveChangesAsync();

                            return Ok();  
                        }
                    }
                }
                else
                {
                    string errorMessage = result.ReasonPhrase;
                    return BadRequest(errorMessage);
                }
            }

            return Ok();
        }




        private string GetPeypalAccessToken()
        {
            string accessToken = "";
            string url = "https://api-m.sandbox.paypal.com/v1/oauth2/token";
            using (HttpClient client = new HttpClient())
            {
                string credentials64 = Convert.ToBase64String(Encoding.UTF8.GetBytes("AV6WsEZglLfQ93XLU3uv3aUk0OEjLhBvq6X0wtjAurS7XNaldbBomMXXk4IEruTVWnvVGm3gNKCbZ6vD:EIdOTh1TKsYuSDJzOHX-L2B8QPi-CUIBxTBS9rn30TsJ2Z5Z_0FnQeu7XR-DGhFnrD4NtT3DSSHw7PPI"));

                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", credentials64);

                var requestMessage = new HttpRequestMessage(HttpMethod.Post, url);
                requestMessage.Content = new StringContent("grant_type=client_credentials", Encoding.UTF8, "application/x-www-form-urlencoded");

                var responseTask = client.SendAsync(requestMessage);
                responseTask.Wait();

                var result = responseTask.Result;

                if (result.IsSuccessStatusCode)
                {
                    var readTask = result.Content.ReadAsStringAsync();
                    readTask.Wait();

                    var strResponse = readTask.Result;

                    var jsonResponse = JsonNode.Parse(strResponse);
                    if (jsonResponse != null)
                    {
                        accessToken = jsonResponse["access_token"]?.ToString() ?? "";
                    }
                }
            }
            return accessToken;
        }




    }
}
