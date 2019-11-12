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
        private readonly CookieContainer _cookieContainer;

        private static readonly string[] _cookieNames = { "slt", "CustomerID", "ASPSESSIONIDASRDRCAC" };

        public BlurayRentalHttpClient(HttpClient httpClient, ICookieTokenProvider cookieTokenProvider, CookieContainer cookieContainer)
        {
            _httpClient = httpClient;
            _cookieTokenProvider = cookieTokenProvider;
            _cookieContainer = cookieContainer;

            var token = _cookieTokenProvider.GetToken();
                        
            if (token != null)
                _cookieContainer.SetCookies(_httpClient.BaseAddress, token.Replace(';', ','));
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
            message.Headers.Add("Cookie", _cookieContainer.GetCookieHeader(_httpClient.BaseAddress));

            var response = await _httpClient.SendAsync(message);

            StoreCookies(response);

            return await response.Content.ReadAsStreamAsync();  //todo: how does this get disposed?
        }

        // From "Your Cart" page. Click "Empty My Entire Cart"
        // CartId will still exist, but the next time an item is added to cart a new CarId will be used.
        public async Task ClearCart()
        {
            var url = "/ShoppingCart.asp?ClearCart=Y";

            using (var request = new HttpRequestMessage(HttpMethod.Get, url))
            {
                request.Headers.Add("Cookie", _cookieContainer.GetCookieHeader(_httpClient.BaseAddress));

                using (var response = await _httpClient.SendAsync(request))
                {
                    StoreCookies(response);
                }
            }
        }

        // Click on "Add To Cart" button from the poduct list
        public async Task AddToCart(string id)
        {
            var url = $"/ProductDetails.asp?ProductCode={id}&btnaddtocart=btnaddtocart&AjaxError=Y&batchadd=Y";

            using (var request = new HttpRequestMessage(HttpMethod.Post, url))
            {
                request.Headers.Add("Cookie", _cookieContainer.GetCookieHeader(_httpClient.BaseAddress));

                request.Content = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>( "ProductCode", id),
                    new KeyValuePair<string, string>( $"QTY.{id}", "1")
                });

                using (var response = await _httpClient.SendAsync(request))
                {
                    StoreCookies(response);
                }
            }
        }

        public class AjaxCart
        {
            public Product[] Products { get; set; }
        }

        public class Product
        {
            public string ProductCode { get; set; }
            public string ProductPrice { get; set; }
        }

        public async Task<AjaxCart> GetCart()
        {
            var url = "/ajaxcart.asp";

            using (var request = new HttpRequestMessage(HttpMethod.Get, url))
            {
                request.Headers.Add("Cookie", _cookieContainer.GetCookieHeader(_httpClient.BaseAddress));

                using (var response = await _httpClient.SendAsync(request))
                {
                    StoreCookies(response);

                    return Deserialize<AjaxCart>(await response.Content.ReadAsStreamAsync());
                }
            }
        }

        private T Deserialize<T>(Stream stream)
        {
            using (var textReader = new StreamReader(stream))
            using (var jsonReader = new Newtonsoft.Json.JsonTextReader(textReader))
            {
                var serializer = Newtonsoft.Json.JsonSerializer.Create();
                return serializer.Deserialize<T>(jsonReader);
            }
        }

        // Loads Cookie:Session%5FToken4Checkout
        public async Task GetOrderPage()
        {
            var url = "/one-page-checkout.asp";

            using (var request = new HttpRequestMessage(HttpMethod.Get, url))
            {
                request.Headers.Add("Cookie", _cookieContainer.GetCookieHeader(_httpClient.BaseAddress));

                using (var response = await _httpClient.SendAsync(request))
                {
                    StoreCookies(response);
                }
            }
        }

        // Acknowledges the available shipping options
        public async Task GetShippingOptions(Order order)
        {
            var url = "/one-page-checkout.asp?RecalcShipping=RecalcShipping";

            using (var request = new HttpRequestMessage(HttpMethod.Post, url))
            {
                request.Headers.Referrer = new Uri(_httpClient.BaseAddress, "/one-page-checkout.asp");
                request.Headers.Add("Cookie", _cookieContainer.GetCookieHeader(_httpClient.BaseAddress));

                request.Content = GetHttpContent(order);

                using (var response = await _httpClient.SendAsync(request))
                {
                    StoreCookies(response);
                }
            }
        }

        // Acknowledges the order total
        // Response is JSON
        public async Task GetOrderTotal(Order order)
        {
            var url = "/one-page-checkout.asp?ShippingSpeedChoice=ShippingSpeedChoice";

            using (var request = new HttpRequestMessage(HttpMethod.Post, url))
            {
                request.Headers.Referrer = new Uri(_httpClient.BaseAddress, "/one-page-checkout.asp");
                request.Headers.Add("Cookie", _cookieContainer.GetCookieHeader(_httpClient.BaseAddress));

                request.Content = GetHttpContent(order);

                using (var response = await _httpClient.SendAsync(request))
                {
                    StoreCookies(response);
                }
            }
        }


        public async Task<long> PostOrderPage(Order order)
        {
            var url = "/one-page-checkout.asp";
            long orderId;
            string location;

            using (var request = new HttpRequestMessage(HttpMethod.Post, url))
            {
                request.Headers.Referrer = new Uri(_httpClient.BaseAddress, url);
                request.Headers.Add("Cookie", _cookieContainer.GetCookieHeader(_httpClient.BaseAddress));

                request.Content = GetHttpContent(order);

                using (var response = await _httpClient.SendAsync(request))
                {
                    StoreCookies(response);

                    // look for redirect response with content like: OrderFinished.asp?Order=Finished&amp;OrderID=236337
                    if (response.StatusCode != HttpStatusCode.Redirect)
                        throw new Exception($"Expected status code: {HttpStatusCode.Redirect}, but received {response.StatusCode}.");

                    location = response.Headers.Location.OriginalString;

                    var regex = new Regex(@"OrderFinished\.asp\?Order=Finished&OrderID=(?<OrderId>\d+)");
                    var match = regex.Match(location);

                    if (!match.Success)
                        throw new Exception($"Unexpected Location Header: {location}.");

                    orderId = long.Parse(match.Groups["OrderId"].Value);
                }
            }

            await FollowRedirect(location);
            return orderId;
        }

        private async Task FollowRedirect(string url)
        {
            using (var request = new HttpRequestMessage(HttpMethod.Get, url))
            {
                request.Headers.Add("Cookie", _cookieContainer.GetCookieHeader(_httpClient.BaseAddress));

                using (var response = await _httpClient.SendAsync(request))
                {
                    StoreCookies(response);
                }
            }
        }


        private HttpContent GetHttpContent(Order order)
        {
            var content = new List<KeyValuePair<string, string>>()
                {
                    new KeyValuePair<string, string>( "AccountNumber", ""),
                    new KeyValuePair<string, string>( "AccountType", ""),
                    new KeyValuePair<string, string>( "BankName", ""),
                    new KeyValuePair<string, string>( "BillingAddress1", order.BillingAddress.Street1),
                    new KeyValuePair<string, string>( "BillingAddress2", order.BillingAddress.Street2 ?? string.Empty),
                    new KeyValuePair<string, string>( "BillingCity", order.BillingAddress.City),
                    new KeyValuePair<string, string>( "BillingCityChanged", "Y"),
                    new KeyValuePair<string, string>( "BillingCompanyName", ""),
                    new KeyValuePair<string, string>( "BillingCountry", "United States"),
                    new KeyValuePair<string, string>( "BillingCountryChanged", "N"),
                    new KeyValuePair<string, string>( "BillingFirstName", order.BillingAddress.FirstName),
                    new KeyValuePair<string, string>( "BillingLastName", order.BillingAddress.LastName),
                    new KeyValuePair<string, string>( "BillingPhoneNumber", order.BillingAddress.Phone),
                    new KeyValuePair<string, string>( "BillingPostalCode", order.BillingAddress.PostalCode),
                    new KeyValuePair<string, string>( "BillingPostalCodeChanged", "Y"),
                    new KeyValuePair<string, string>( "BillingState", order.BillingAddress.State),
                    new KeyValuePair<string, string>( "BillingState_dropdown", order.BillingAddress.State),
                    new KeyValuePair<string, string>( "BillingState_Required", "Y"), // N for puerto rico?
                    new KeyValuePair<string, string>( "BillingStateChanged", "N"),
                    new KeyValuePair<string, string>( "btnSubmitOrder", "DoThis"),
                    new KeyValuePair<string, string>( "CCards", ""),
                    new KeyValuePair<string, string>( "CheckNumber", ""),
                    new KeyValuePair<string, string>( "code1", ""),
                    new KeyValuePair<string, string>( "code2", ""),
                    new KeyValuePair<string, string>( "code3", ""),
                    new KeyValuePair<string, string>( "CouponCode", ""),
                    new KeyValuePair<string, string>( "hidden_btncalc_shipping", ""),
                    new KeyValuePair<string, string>( "Keep_Payment_Method_On_File_eCheck", "Y"),
                    new KeyValuePair<string, string>( "last-form-submit-date", ""),// DateTime.UtcNow.ToString(@"ddd MMM dd yyyy HH:mm:ss \G\M\T\+0000 \(\U\T\C\)")), //Wed Oct 18 2017 12:41:34 GMT+0000 (UTC)
                    new KeyValuePair<string, string>( "My_Saved_Billing", "Select"),
                    new KeyValuePair<string, string>( "My_Saved_Shipping", "Select"),
                    new KeyValuePair<string, string>( "PaymentMethodType", "NONE"),
                    new KeyValuePair<string, string>( "PaymentMethodTypeDisplay", ""),
                    new KeyValuePair<string, string>( "PCIaaS_CardId", ""),
                    new KeyValuePair<string, string>( "PONum", ""),
                    new KeyValuePair<string, string>( "Previous_Calc_Shipping", "0"),
                    new KeyValuePair<string, string>( "Previous_Tax_Percents", "000"),
                    //new KeyValuePair<string, string>( "Quantity1", "1"),    //foreach item in the order (below)
                    new KeyValuePair<string, string>( "remove_billingid", ""),
                    new KeyValuePair<string, string>( "remove_ccardid", ""),
                    new KeyValuePair<string, string>( "remove_shipid", ""),
                    new KeyValuePair<string, string>( "RoutingNumber", ""),
                    new KeyValuePair<string, string>( "ShipAddress1", order.ShippingAddress.Street1),
                    new KeyValuePair<string, string>( "ShipAddress2", order.ShippingAddress.Street2 ?? string.Empty),
                    new KeyValuePair<string, string>( "ShipCity", order.ShippingAddress.City),
                    new KeyValuePair<string, string>( "ShipCityChanged", "Y"), //
                    new KeyValuePair<string, string>( "ShipCompanyName", ""),
                    new KeyValuePair<string, string>( "ShipCountry", "United States"),
                    new KeyValuePair<string, string>( "ShipFirstName", order.ShippingAddress.FirstName),
                    new KeyValuePair<string, string>( "ShipLastName", order.ShippingAddress.LastName),
                    new KeyValuePair<string, string>( "ShipPhoneNumber", order.ShippingAddress.Phone),
                    new KeyValuePair<string, string>( "ShippingSpeedChoice", "101"),
                    new KeyValuePair<string, string>( "ShipPostalCode", order.ShippingAddress.PostalCode),
                    new KeyValuePair<string, string>( "ShipPostalCodeChanged", "Y"),    //
                    new KeyValuePair<string, string>( "ShipResidential", "Y"),
                    new KeyValuePair<string, string>( "ShipState", order.ShippingAddress.State),
                    new KeyValuePair<string, string>( "ShipState_dropdown", order.ShippingAddress.State),
                    new KeyValuePair<string, string>( "ShipState_Required", "Y"),   // N for Puerto Rico?
                    new KeyValuePair<string, string>( "ShipTo", "use_different_address"),
                    new KeyValuePair<string, string>( "Using_Existing_Account", "Y")
                };

            for (var i = 0; i < order.Items.Length; i++)
                content.Add(new KeyValuePair<string, string>($"Quantity{i + 1}", "1"));    // We only allow 1 of each item

            return new FormUrlEncodedContent(content); ;
        }



        public async Task<string> GetOrderTotals(string cartId)
        {
            var url = $"/one-page-checkout.asp?ShippingSpeedChoice=ShippingSpeedChoice";

            var request = new HttpRequestMessage(HttpMethod.Get, url);
            //request.Headers.Add("Cookie", _cookieTokenProvider.GetToken());
            //request.Headers.Add("Cookie", $"CartID5={cartId}");
            request.Headers.Add("Cookie", _cookieContainer.GetCookieHeader(_httpClient.BaseAddress));


            var response = await _httpClient.SendAsync(request);

            StoreCookies(response);

            return await response.Content.ReadAsStringAsync();
            //return await response.Content.ReadAsStreamAsync();  //todo: how does this get disposed?.
        }

        public async Task<Address[]> GetShippingAddressesAsync()
        {
            var url = "/one-page-checkout.asp";

            var request = new HttpRequestMessage(HttpMethod.Get, url);
            //request.Headers.Add("Cookie", _cookieTokenProvider.GetToken());
            request.Headers.Add("Cookie", _cookieContainer.GetCookieHeader(_httpClient.BaseAddress));

            var response = await _httpClient.SendAsync(request);

            StoreCookies(response);

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

                // These cokies are maintaining some sort of state
                if (cookie.Name.StartsWith("TS014fe2d9", StringComparison.OrdinalIgnoreCase))
                    continue;

                container.Add(cookie);
            }

            if (!valid)
                return null;

            return container.GetCookieHeader(_httpClient.BaseAddress);
        }

        private void StoreCookies(HttpResponseMessage response)
        {
            foreach (var cookie in ParseCookies(response.Headers.GetValues("Set-Cookie")))
                _cookieContainer.Add(cookie);

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
