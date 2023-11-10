using Microsoft.AspNetCore.Mvc.RazorPages;

namespace WebhookClient.Pages
{
    [Authorize]

    public class RegisterWebhookModel(IWebhooksClient client, IOptions<WebhookClientOptions> options) : PageModel
    {
        [BindProperty] public string Token { get; set; }

        public int ResponseCode { get; set; }
        public string RequestUrl { get; set; }
        public string GrantUrl { get; set; }
        public string ResponseMessage { get; set; }
        public string RequestBodyJson { get; set; }

        private readonly WebhookClientOptions _options = options.Value;

        public void OnGet()
        {
            ResponseCode = StatusCodes.Status200OK;
            Token = _options.Token;
        }

        public async Task<IActionResult> OnPost()
        {
            ResponseCode = StatusCodes.Status200OK;
            var protocol = Request.IsHttps ? "https" : "http";
            var selfurl = !string.IsNullOrEmpty(_options.SelfUrl) ? _options.SelfUrl : $"{protocol}://{Request.Host}/{Request.PathBase}";

            if (!selfurl.EndsWith("/"))
            {
                selfurl += "/";
            }

            var granturl = $"{selfurl}check";
            var url = $"{selfurl}webhook-received";

            var payload = new WebhookSubscriptionRequest()
            {
                Event = "OrderPaid",
                GrantUrl = granturl,
                Url = url,
                Token = Token
            };

            var response = await client.AddWebHookAsync(payload);

            if (response.IsSuccessStatusCode)
            {
                return RedirectToPage("WebhooksList");
            }
            else
            {
                RequestBodyJson = JsonSerializer.Serialize(payload);
                ResponseCode = (int)response.StatusCode;
                ResponseMessage = response.ReasonPhrase;
                GrantUrl = granturl;
                RequestUrl = $"{response.RequestMessage.Method} {response.RequestMessage.RequestUri}";
            }

            return Page();
        }
    }
}
