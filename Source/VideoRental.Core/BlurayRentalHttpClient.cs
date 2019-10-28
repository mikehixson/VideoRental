using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace VideoRental.Core
{
    public class BlurayRentalHttpClient
    {
        private readonly HttpClient _httpClient;
        private readonly ICookieTokenProvider _cookieTokenProvider;

        private static readonly string[] _cookieNames = { "slt", "CustomerID", "ASPSESSIONIDASRDRCAC" };

        public BlurayRentalHttpClient(HttpClient httpClient, ICookieTokenProvider cookieTokenProvider)
        {
            _httpClient = httpClient;
            _cookieTokenProvider = cookieTokenProvider;
        }

        public async Task<string> LoginAsync(string emailAddress, string password)
        {
            var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>( "email", emailAddress),
                new KeyValuePair<string, string>( "password", password),
                new KeyValuePair<string, string>( "CustomerNewOld", "old"),
                new KeyValuePair<string, string>( "imageField2.x", "62"),
                new KeyValuePair<string, string>( "imageField2.y", "15"),
            });

            using (var response = await _httpClient.PostAsync("/login.asp", content))
            {
                var cookies = ParseCookies(response.Headers.GetValues("Set-Cookie"));

                return GetCookieToken(cookies);
            }
        }

        public async Task<Stream> GetVideosStreamAsync(int category, int pageIndex, int sortBy, int maxResults)
        {
            var url = $"/category-s/{category}.htm?sort={sortBy}&show={maxResults}&page={pageIndex + 1}";

            var message = new HttpRequestMessage(HttpMethod.Get, url);
            message.Headers.Add("Cookie", _cookieTokenProvider.GetToken());

            var response = await _httpClient.SendAsync(message);

            return await response.Content.ReadAsStreamAsync();  //todo: how does this get disposed?
        }

        // From "Your Cart" page. Click "Empty My Entire Cart"
        // CartId will still exist, but the next time an item is added to cart a new CarId will be used.
        public async Task ClearCart()
        {
            var url = "/ShoppingCart.asp?ClearCart=Y";

            using (var request = new HttpRequestMessage(HttpMethod.Get, url))
            {
                request.Headers.Add("Cookie", _cookieTokenProvider.GetToken());

                using (var response = await _httpClient.SendAsync(request))
                {

                }
            }
        }

        // Click on "Add To Cart" button from the poduct list
        // index is 1-based
        public async Task<string> AddToCart(string id)
        {
            var url = $"/ProductDetails.asp?ProductCode={id}&btnaddtocart=btnaddtocart&AjaxError=Y&batchadd=Y";

            using (var request = new HttpRequestMessage(HttpMethod.Post, url))
            {
                request.Headers.Add("Cookie", _cookieTokenProvider.GetToken());

                request.Content = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>( "ProductCode", id),
                    new KeyValuePair<string, string>( $"QTY.{id}", "1")
                });


                using (var response = await _httpClient.SendAsync(request))
                {
                    return ParseCookies(response.Headers.GetValues("Set-Cookie"))
                        .Single(c => c.Name.Equals("CartID5", StringComparison.OrdinalIgnoreCase)).Value;
                }
            }
        }

        public async Task PlaceOrder(string cartId, Order order)
        {
            var url = $"/one-page-checkout.asp";

            var request = new HttpRequestMessage(HttpMethod.Post, url);
            {
                request.Headers.Add("Cookie", _cookieTokenProvider.GetToken());
                request.Headers.Add("Cookie", $"CartID5={cartId}");

                request.Content = new FormUrlEncodedContent(new[]
                {   
                    // Billing
                    new KeyValuePair<string, string>( "BillingFirstName", order.BillingAddress.FirstName),
                    new KeyValuePair<string, string>( "BillingLastName", order.BillingAddress.LastName),
                    new KeyValuePair<string, string>( "BillingCompanyName", ""),
                    new KeyValuePair<string, string>( "BillingAddress1", order.BillingAddress.Street1),
                    new KeyValuePair<string, string>( "BillingAddress2", order.BillingAddress.Street2),
                    new KeyValuePair<string, string>( "BillingCity", order.BillingAddress.City),
                    new KeyValuePair<string, string>( "BillingState", order.BillingAddress.State),
                    new KeyValuePair<string, string>( "BillingPostalCode", order.BillingAddress.PostalCode),                    
                    new KeyValuePair<string, string>( "BillingCountry", "United States"),
                    new KeyValuePair<string, string>( "BillingPhoneNumber", order.BillingAddress.Phone),
                    

                    new KeyValuePair<string, string>( "My_Saved_Billing: Select", "Select"),    // Drop down. Indicate that we are not using a saved address
                    new KeyValuePair<string, string>( "BillingCityChanged", "N"),
                    new KeyValuePair<string, string>( "BillingCountryChanged", "N"),
                    new KeyValuePair<string, string>( "BillingState_Required", "N"),
                    new KeyValuePair<string, string>( "BillingState_dropdown", order.BillingAddress.State),                    
                    new KeyValuePair<string, string>( "BillingStateChanged", "N"),


                    // Shipping
                    new KeyValuePair<string, string>( "ShipFirstName", order.ShippingAddress.FirstName),
                    new KeyValuePair<string, string>( "ShipLastName", order.ShippingAddress.LastName),
                    new KeyValuePair<string, string>( "ShipCompanyName", ""),
                    new KeyValuePair<string, string>( "ShipAddress1", order.ShippingAddress.Street1),
                    new KeyValuePair<string, string>( "ShipAddress2", order.ShippingAddress.Street2),
                    new KeyValuePair<string, string>( "ShipCity", order.ShippingAddress.City),
                    new KeyValuePair<string, string>( "ShipState", order.ShippingAddress.State),
                    new KeyValuePair<string, string>( "ShipPostalCode", order.ShippingAddress.PostalCode),
                    new KeyValuePair<string, string>( "ShipCountry", "United States"),
                    new KeyValuePair<string, string>( "ShipPhoneNumber", order.ShippingAddress.Phone),

                    new KeyValuePair<string, string>( "My_Saved_Shipping: Select", "Select"),    // Drop down. Indicate that we are not using a saved address
                    new KeyValuePair<string, string>( "ShipCityChanged", "N"),
                    new KeyValuePair<string, string>( "ShipCountryChanged", "N"),
                    new KeyValuePair<string, string>( "ShipState_Required", "N"),
                    new KeyValuePair<string, string>( "ShipState_dropdown", order.ShippingAddress.State),
                    new KeyValuePair<string, string>( "ShipStateChanged", "N"),


                    new KeyValuePair<string, string>( "ShipResidential", "Y"),
                    new KeyValuePair<string, string>( "ShippingSpeedChoice", "101"),


                    new KeyValuePair<string, string>( "btnSubmitOrder", "DoThis")                    
                });
            
                var response = await _httpClient.SendAsync(request);

                var s = await response.Content.ReadAsStringAsync();
            }
        }




        public async Task<string> GetOrderTotals(string cartId)
        {
            var url = $"/one-page-checkout.asp?ShippingSpeedChoice=ShippingSpeedChoice";

            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("Cookie", _cookieTokenProvider.GetToken());
            request.Headers.Add("Cookie", $"CartID5={cartId}");

            var response = await _httpClient.SendAsync(request);

            return await response.Content.ReadAsStringAsync();
            //return await response.Content.ReadAsStreamAsync();  //todo: how does this get disposed?.
        }

        public async Task<Address[]> GetShippingAddressesAsync6()
        {
            var url = "/one-page-checkout.asp";

            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("Cookie", _cookieTokenProvider.GetToken());

            var response = await _httpClient.SendAsync(request);

            response.EnsureSuccessStatusCode();

            var document = new HtmlDocument();
            document.Load(await response.Content.ReadAsStreamAsync());

            var slect1 = document.DocumentNode.SelectNodes("//select[@name='My_Saved_Billing']/options");
            var slect2 = document.DocumentNode.SelectNodes("//select[@name='My_Saved_Shipping']/options");

            //var script = document.DocumentNode.SelectNodes("//script[not(@src)]");


            return new Address[0];

        }
/*
        public async Task<Address[]> GetShippingAddressesAsync()
        {
            var url = "/AccountSettings.asp?modwhat=change_s";

            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("Cookie", _cookieTokenProvider.GetToken());

            var response = await _httpClient.SendAsync(request);

            response.EnsureSuccessStatusCode();

            var document = new HtmlDocument();
            document.Load(await response.Content.ReadAsStreamAsync());

            var addressIds = document.DocumentNode.SelectNodes("//input[@type='radio' and @name='ShipCHOSEN']")
                .Select(n => int.Parse(n.GetAttributeValue("value", default(string))));


            return await Task.WhenAll(EnumerateAddressIds());

            IEnumerable<Task<Address>> EnumerateAddressIds()
            {
                foreach (var addressId in addressIds)
                    yield return GetShippingAddressAsync(addressId);
            }
        }


        private async Task<Address> GetShippingAddressAsync(int id)
        {
            var url = $"/AccountSettings.asp?modwhat=change_s&ShipID={id}&Recurring=&DirectLink=&OrderPlaced=&ReturnTo=";

            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("Cookie", _cookieTokenProvider.GetToken());


            var response = await _httpClient.SendAsync(request);

            response.EnsureSuccessStatusCode();

            var document = new HtmlDocument();
            document.Load(await response.Content.ReadAsStreamAsync());

            var inputs = document.DocumentNode.SelectNodes("//input[@name and @value]");

            var address = new Address();
            address.Id = id;

            foreach (var input in inputs)        //todo: do this differntly. query for each name
            {
                switch (input.GetAttributeValue("name"))
                {
                    case var name when name.Equals("ShipFirstName", StringComparison.OrdinalIgnoreCase):
                        address.FirstName = input.GetAttributeValue("value");
                        break;

                    case var name when name.Equals("ShipLastName", StringComparison.OrdinalIgnoreCase):
                        address.LastName = input.GetAttributeValue("value");
                        break;

                    case var name when name.Equals("ShipCompanyName", StringComparison.OrdinalIgnoreCase):
                        address.Company = input.GetAttributeValue("value");
                        break;

                    case var name when name.Equals("ShipAddress1", StringComparison.OrdinalIgnoreCase):
                        address.Street1 = input.GetAttributeValue("value");
                        break;

                    case var name when name.Equals("ShipAddress2", StringComparison.OrdinalIgnoreCase):
                        address.Street2 = input.GetAttributeValue("value");
                        break;

                    case var name when name.Equals("ShipCity", StringComparison.OrdinalIgnoreCase):
                        address.City = input.GetAttributeValue("value");
                        break;

                    case var name when name.Equals("ShipPostalCode", StringComparison.OrdinalIgnoreCase):
                        address.PostalCode = input.GetAttributeValue("value");
                        break;

                    case var name when name.Equals("ShipPhoneNumber", StringComparison.OrdinalIgnoreCase):
                        address.Phone = input.GetAttributeValue("value");
                        break;
                }
            }

            foreach (var script in document.DocumentNode.SelectNodes("//script[not(@src)]"))
            {
                // todo: move this somewhere else
                var regex = new Regex("(ShipCountry_default_value = \"(?<Country>[^\"]+)\").*?(ShipState_dropdown_default_value = \"(?<State>[^\"]+)\")", RegexOptions.Singleline | RegexOptions.ExplicitCapture | RegexOptions.Compiled);

                var match = regex.Match(script.InnerText);

                if (match.Success)
                {
                    address.State = match.Groups["State"].Value;
                    address.Country = match.Groups["Country"].Value;

                    break;
                }
            }

            return address;
        }

        public async Task DeleteShippingAddressAsync(int id)
        {
            var url = $"/AccountSettings.asp?modwhat=change_s&Delete={id}";

            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("Cookie", _cookieTokenProvider.GetToken());

            await _httpClient.SendAsync(request);
        }
*/

        private IEnumerable<Cookie> ParseCookies(IEnumerable<string> cookieHeaders)
        {
            var container = new CookieContainer();

            foreach (var cookieHeader in cookieHeaders)
            {
                if (cookieHeader.StartsWith("slt", StringComparison.OrdinalIgnoreCase))
                {
                    // Repair the expire date timezone. This is the whole reason we cant use CookieContainer normally.
                    container.SetCookies(_httpClient.BaseAddress, cookieHeader.Replace("UTC;", "GMT;"));
                    continue;
                }

                container.SetCookies(_httpClient.BaseAddress, cookieHeader);
            }

            return container.GetCookies(_httpClient.BaseAddress).OfType<Cookie>();
        }

        // cookie named "slt" has an expire date like "11 Oct 2019 10:31:55 UTC", which fails to parse due to the use of UTC instead of GMT
        // official date format here: https://developer.mozilla.org/en-US/docs/Web/HTTP/Headers/Set-Cookie
        // It seems as if .net knows about this format here: https://docs.microsoft.com/en-us/dotnet/standard/base-types/standard-date-and-time-format-strings#RFC1123
        private string GetCookieToken(IEnumerable<Cookie> cookies)
        {
            var container = new CookieContainer();
            var valid = false;

            foreach (var cookie in cookies)//.Where(c => _cookieNames.Any(n => c.Name.Equals(n, StringComparison.OrdinalIgnoreCase))))
            {
                // Presenece of this cookie indicates a sucessful login
                if (cookie.Name.Equals("slt", StringComparison.OrdinalIgnoreCase))
                    valid = true;

                container.Add(cookie);
            }

            if (!valid)
                return null;

            return container.GetCookieHeader(_httpClient.BaseAddress);
        }


    }

    public static class Extension
    {
        public static string GetAttributeValue(this HtmlNode node, string name)
        {
            return node.GetAttributeValue(name, default(string));
        }
    }
}
