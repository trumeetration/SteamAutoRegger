using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Text.RegularExpressions;
using xNet;
using System.Linq;
using System.Text;
using System.Net;
using System.Security.Cryptography;
using MihaZupan;

namespace SteamRegger
{
    class Program
    {
        static void Main(string[] args)
        {
            
            string proxyHost = "";
            int proxyPort = 0;
            string proxyUserName = "";
            string proxyPassword = "";
            string apikey = "";
            Console.ForegroundColor = ConsoleColor.DarkMagenta;
            Console.WriteLine("Github: https://github.com/trumeetration \n");
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write("!!!Warning!!! ");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("This is test version of program. \n" +
                                "All bugs and troubles, uncomfortable moments will be fixed in future, i wish :3\n");
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("How to use: 1) Type path to file.\n" +
                                "            2) Type ProxyIp (program support only Socks5 proxy(ip:port:login:password) at the moment)\n" +
                                "            3) Type ProxyPort\n" +
                                "            4) Type ProxyLogin\n" +
                                "            5) Type ProxyPass\n" +
                                "            6) Type CaptchaGuru apikey(only CaptchaGuru API supports at the moment)\n" +
                                "            7) Choose amount of threads\n");
            Thread.Sleep(5000);
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.Write("Full path to file (e.g. \"C:\\folder\\file.txt\"): ");
            string path = Console.ReadLine();
            Console.Write("Ip: ");
            proxyHost = Console.ReadLine();
            Console.Write("Port: ");
            proxyPort = Convert.ToInt32(Console.ReadLine());
            Console.Write("Login: ");
            proxyUserName = Console.ReadLine();
            Console.Write("Pass: ");
            proxyPassword = Console.ReadLine();
            Console.Write("Apikey: ");
            apikey = Console.ReadLine();
            Console.Write("Amount of threads (not recomended more than 15): ");
            int threads = Convert.ToInt32(Console.ReadLine());
            Console.WriteLine("");
            for (int i = 1; i < threads + 1; i++)
            {
                (new Thread(() => SteamRegister(i, proxyHost, proxyPort, proxyUserName, proxyPassword, path, apikey))).Start();
                Thread.Sleep(300);
            }
        }

        public static void SteamRegister(int i, string proxyHost, int proxyPort, string proxyUserName, string proxyPassword, string path, string apikey)
        {
            while (true)
            {
                string input, zapros, VerificationLinkStatus, response;
                string CapResponse = "CAPCHA_NOT_READY";
                string pattern = @"gid\W{3}(\d+)\W";
                string Exponent = "010001";
                string login = GetRandomString(14).ToLower();
                string password = GetRandomString(12);
                string nickname = GetRandomString(14).ToLower();
                var proxy = new HttpToSocks5Proxy(proxyHost, proxyPort, proxyUserName, proxyPassword);
                var httpClientHandler = new HttpClientHandler
                {
                    Proxy = proxy,
                };
                HttpClient req = new HttpClient(httpClientHandler)
                {
                    Timeout = TimeSpan.FromMilliseconds(15000)
                };
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine($"[Thread {i}]: Started");
                while (true)
                {
                    try
                    {
                        response = req.GetAsync($"https://api.captcha.guru/in.php?key={apikey}&method=userrecaptcha&googlekey=6LerFqAUAAAAABMeByEoQX9u10KRObjwHf66-eya&pageurl=https://store.steampowered.com/join/").Result.Content.ReadAsStringAsync().Result;
                    }
                    catch
                    {
                        //Console.WriteLine("Trouble1");
                        Thread.Sleep(1000);
                        continue;
                    }
                    break;
                }
                //Console.WriteLine(response);
                if (!response.Contains("OK|"))
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"[Thread {i}]: Captcha not accepted");
                    continue;
                }
                string CaptchaId = response.Substring("OK|");
                //Console.WriteLine(CaptchaId);
                while (CapResponse == "CAPCHA_NOT_READY")
                {
                    Thread.Sleep(8000);
                    try
                    {
                        CapResponse = req.GetAsync($"https://api.captcha.guru/res.php?key={apikey}&action=get&id={CaptchaId}").Result.Content.ReadAsStringAsync().Result;
                    }
                    catch
                    {
                        //Console.WriteLine("Trouble2");
                        continue;
                    }
                }
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine($"[Thread {i}]: Captcha solving finished...");
                Thread.Sleep(1500);
                if (CapResponse == "ERROR_CAPTCHA_UNSOLVABLE")
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"[Thread {i}]: Captcha unsolvable");
                    continue;
                }
                if (CapResponse.Contains("OK|"))
                {
                    CapResponse = CapResponse.Substring("OK|");
                    while (true)
                    {
                        try
                        {
                            input = req.GetAsync("https://store.steampowered.com/join/refreshcaptcha/").Result.Content.ReadAsStringAsync().Result;
                        }
                        catch
                        {
                            //Console.WriteLine("Trouble3");
                            Thread.Sleep(1000);
                            continue;
                        }
                        break;
                    }
                    if (Regex.IsMatch(input, pattern, RegexOptions.IgnoreCase) == false)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"[Thread {i}]: Problem with recieving CaptchaID Steam");
                        continue;
                    }
                    string GID = Regex.Matches(input, pattern, RegexOptions.IgnoreCase).First().Groups[1].Value;
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.WriteLine($"[Thread {i}]: CaptchaGoogleID: {GID}");
                    var content = new System.Net.Http.StringContent($"email={login}%40uzundiz.best&captchagid={GID}&captcha_text={CapResponse}", Encoding.UTF8, "application/x-www-form-urlencoded");
                    while (true)
                    {
                        try
                        {
                            response = req.PostAsync("https://store.steampowered.com/join/ajaxverifyemail", content).Result.Content.ReadAsStringAsync().Result;
                        }
                        catch
                        {
                            //Console.WriteLine("Trouble4");
                            Thread.Sleep(1000);
                            continue;
                        }
                        break;
                    }
                    //Console.WriteLine(response);
                    Thread.Sleep(10000);
                    // получение ссылки из письма
                    while (true)
                    {
                        try
                        {
                            zapros = req.GetAsync($"https://generator.email/{login}@uzundiz.best").Result.Content.ReadAsStringAsync().Result;
                        }
                        catch
                        {
                            //Console.WriteLine($"Thread {i}: Проблема при загрузке сайта почтового сервиса");
                            Thread.Sleep(1000);
                            continue;
                        }
                        break;
                    }
                    if (Regex.IsMatch(zapros, "<a href=\"https://store\\.steampowered\\.com/account/newaccountverification\\?stoken=.+&amp;creationid=\\d+\"", RegexOptions.Singleline) == false)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"[Thread {i}]: Link for confirmation not found");
                        Thread.Sleep(5000);
                        continue;
                    }
                    string VerificationLink = Regex.Matches(zapros, "<a href=\"(https://store\\.steampowered\\.com/account/newaccountverification\\?stoken=.+&amp;creationid=\\d+)\"", RegexOptions.Singleline).First().Groups[1].Value.Replace("&amp;", "&");
                    string CreationId = Regex.Matches(zapros, "<a href=\"https://store\\.steampowered\\.com/account/newaccountverification\\?stoken=.+&amp;creationid=(\\d+)\"", RegexOptions.Singleline).First().Groups[1].Value;
                    while (true)
                    {
                        try
                        {
                            VerificationLinkStatus = req.GetAsync(VerificationLink).Result.Content.ReadAsStringAsync().Result; // переход по ссылке из письма
                        }
                        catch
                        {
                            //Console.WriteLine("Trouble5");
                            Thread.Sleep(1000);
                            continue;
                        }
                        break;
                    }
                    content = new System.Net.Http.StringContent("creationid=" + CreationId, Encoding.UTF8, "application/x-www-form-urlencoded");
                    while (true)
                    {
                        try
                        {
                            response = req.PostAsync("https://store.steampowered.com/join/ajaxcheckemailverified", content).Result.Content.ReadAsStringAsync().Result;
                        }
                        catch
                        {
                            //Console.WriteLine("Trouble6");
                            Thread.Sleep(1000);
                            continue;
                        }
                        break;
                    }
                    //Console.WriteLine(response);
                    if (response != "1")
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"[Thread {i}]: Bad action after mail confirmation");
                        continue;
                    }
                    while (true)
                    {
                        try
                        {
                            response = req.GetAsync($"https://store.steampowered.com/join/completesignup?creationid={CreationId}").Result.Content.ReadAsStringAsync().Result;
                        }
                        catch
                        {
                            //Console.WriteLine("Trouble7");
                            Thread.Sleep(1000);
                            continue;
                        }
                        break;
                    }
                    // Console.WriteLine(response);
                    content = new System.Net.Http.StringContent($"accountname={login}&password={password}&count=3&lt=0&creation_sessionid={CreationId}&embedded_appid=0", Encoding.UTF8, "application/x-www-form-urlencoded");
                    while (true)
                    {
                        try
                        {
                            response = req.PostAsync("https://store.steampowered.com/join/createaccount/", content).Result.Content.ReadAsStringAsync().Result; // регистрация аккаунта
                        }
                        catch
                        {
                            //Console.WriteLine("Trouble8");
                            Thread.Sleep(1000);
                            continue;
                        }
                        break;
                    }

                    // Авторизация
                    // шифрование пароля
                    RNGCryptoServiceProvider rngcryptoServiceProvider = new RNGCryptoServiceProvider();
                    RSACryptoServiceProvider rsacryptoServiceProvider = new RSACryptoServiceProvider();
                    byte[] inArray;
                    byte[] bytes = Encoding.ASCII.GetBytes(password);
                    content = new System.Net.Http.StringContent($"username={login}", Encoding.UTF8, "application/x-www-form-urlencoded");
                    // Получаем модуль
                    while (true)
                    {
                        try
                        {
                            response = req.PostAsync("https://steamcommunity.com/login/getrsakey/", content).Result.Content.ReadAsStringAsync().Result;
                        }
                        catch
                        {
                            //Console.WriteLine("Trouble9");
                            Thread.Sleep(1000);
                            continue;
                        }
                        break;
                    }
                    //Console.WriteLine(response);
                    if (!Regex.IsMatch(response, "{\"success\":true,\"publickey_mod\":\"(\\w+)\",\"publickey_exp\":\"010001\",\"timestamp\":\"(\\d+)\",\"token_gid\":\"\\w+\"}", RegexOptions.Singleline))
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"[Thread {i}] {login}: Bad attempt to recieve modulus. Status code: \n" + response);
                        continue;
                    }
                    var LoginRegex = Regex.Matches(response, "{\"success\":true,\"publickey_mod\":\"(\\w+)\",\"publickey_exp\":\"010001\",\"timestamp\":\"(\\d+)\",\"token_gid\":\"\\w+\"}", RegexOptions.Singleline).First();
                    string Modulus = LoginRegex.Groups[1].Value;
                    string RsaTimeStamp = LoginRegex.Groups[2].Value;
                    //Console.WriteLine(Modulus);
                    // Console.WriteLine(); Console.WriteLine(RsaTimeStamp);
                    RSAParameters parameters = rsacryptoServiceProvider.ExportParameters(false);
                    parameters.Exponent = Util.HexStringToByteArray(Exponent);
                    parameters.Modulus = Util.HexStringToByteArray(Modulus);
                    rsacryptoServiceProvider.ImportParameters(parameters);
                    inArray = rsacryptoServiceProvider.Encrypt(bytes, false);
                    string nocache = Util.GetSystemUnixTime().ToString();
                    var value = WebUtility.UrlEncode(Convert.ToBase64String(inArray));
                    //Console.WriteLine("\n nocache: " + nocache); Console.WriteLine("RsaStamp: " + RsaTimeStamp); Console.WriteLine("Pass: " + value);
                    System.Net.Http.HttpContent contentt = new System.Net.Http.StringContent($"donotcache={nocache}&password={value}&username={login}&twofactorcode=&emailauth=&loginfriendlyname=&captchagid=-1&captcha_text=&emailsteamid=&rsatimestamp={RsaTimeStamp}&remember_login=false", Encoding.UTF8, "application/x-www-form-urlencoded");
                    // donotcache=1584130728329&password=cgflr5vhQudyanK7ztqpwtgVCXQF1ulKRSs1v9pi4Wf9mSaLs0CdsgH5NjKK2rOjP1ooOXSpb1z%2BHJq6PWZNF6l76df7J6B%2BBhD%2FRtxLuG7UaAaL3uV8muOaiTdVJ%2BpQgy1kWBMVTrWNAK%2F%2FGpiKNx1cE%2BMEWUew3y1qhWlaHiBvDzVnfmCHH3lHEOR8SiWXvzHjAmSbwF7kui9UyXrnqMUvWx8Q%2BdcHGFr1brchijf%2B3e4UCu9VK%2BCpeq%2BWFrYx%2BB7q1oUVwV166%2FENOQFPeIdrMOsaaF%2FMUneSdqlWY9z1GUJX3qSlhCQ%2BpHkdnfoIeiHTIVhspeDcyTWZ2spag%3D%3D&username=ida25sasaki&twofactorcode=&emailauth=&loginfriendlyname=&captchagid=-1&captcha_text=&emailsteamid=&rsatimestamp=68428350000&remember_login=false
                    while (true)
                    {
                        try
                        {
                            response = req.PostAsync("https://steamcommunity.com/login/dologin/", contentt).Result.Content.ReadAsStringAsync().Result;
                        }
                        catch
                        {
                            //Console.WriteLine("Trouble10");
                            Thread.Sleep(1000);
                            continue;
                        }
                        break;
                    }
                    //Console.WriteLine();
                    //Console.WriteLine(response);
                    if (!response.StartsWith("{\"success\":true,\""))
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"[Thread {i}]: unsuccessfull login");
                        continue;
                    }
                    Console.ForegroundColor = ConsoleColor.Magenta;
                    Console.WriteLine($"[Thread {i}] {login}: Successful login");
                    var AfterLoginRegex = Regex.Matches(response, "{\"success\":true,\"requires_twofactor\":false,\"login_complete\":true,\"transfer_urls\":\\[.+\\],\"transfer_parameters\":{\"steamid\":\"(\\d+)\",\"token_secure\":\"\\w+\",\"auth\":\"\\w+\",\"remember_login\":false}}", RegexOptions.Singleline).First();
                    string SteamId = AfterLoginRegex.Groups[1].Value;
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.WriteLine($"[Thread {i}] {login}: Changing nickname");
                    while (true)
                    {
                        try
                        {
                            response = req.GetAsync($"https://steamcommunity.com/profiles/{SteamId}/edit").Result.Content.ReadAsStringAsync().Result;
                        }
                        catch
                        {
                            //Console.WriteLine("Trouble11");
                            Thread.Sleep(1000);
                            continue;
                        }
                        break;
                    }
                    if (!Regex.IsMatch(response, "g_sessionID = \"(\\w{5,40})\";", RegexOptions.Singleline))
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"[Thread {i}]: {login}: g_sessionID not found");
                        continue;
                    }
                    var sessionid = Regex.Matches(response, "g_sessionID = \"(\\w{5,40})\";", RegexOptions.Singleline).First().Groups[1].Value;
                    // Смена ника
                    content = new System.Net.Http.StringContent($"sessionID={sessionid}&type=profileSave&weblink_1_title=&weblink_1_url=&weblink_2_title=&weblink_2_url=&weblink_3_title=&weblink_3_url=&personaName={nickname}&real_name=&country=&state=&city=&customURL=&summary=&primary_group_steamid=0", Encoding.UTF8, "application/x-www-form-urlencoded");
                    while (true)
                    {
                        try
                        {
                            response = req.PostAsync($"https://steamcommunity.com/profiles/{SteamId}/edit", content).Result.Content.ReadAsStringAsync().Result;
                        }
                        catch
                        {
                            //Console.WriteLine("Trouble12");
                            Thread.Sleep(1000);
                            continue;
                        }
                        break;
                    }
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.WriteLine($"[Thread {i}]: {login}: Changed nickname to {nickname}");
                    ////todo: открыть профиль
                    ////multipartdata - неизведанное мною пока что
                    content = new System.Net.Http.StringContent($"action=actuallynone&sessionid={sessionid}", Encoding.UTF8, "application/x-www-form-urlencoded");
                    while (true)
                    {
                        try
                        {
                            response = req.PostAsync("https://store.steampowered.com/twofactor/manage_action", content).Result.Content.ReadAsStringAsync().Result;
                        }
                        catch
                        {
                            //Console.WriteLine("Trouble13");
                            Thread.Sleep(1000);
                            continue;
                        }
                        break;
                    }
                    //Console.WriteLine(response);
                    StreamWriter file = new StreamWriter(path, true);
                    file.WriteLine(login + ":" + password);
                    file.Close();
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"[Thread {i}]: Success registration and configuration. Login - {login}\t Pass - {password}");
                }
                Thread.Sleep(5000);
            }
            
        }

        public static string GetRandomString(int x)
        {
            string pass = "";
            var r = new Random();
            while (pass.Length < x)
            {
                Char c = (char)r.Next(33, 125);
                if (Char.IsLetterOrDigit(c))
                    pass += c;
            }
            return pass;
        }

    }

    public class Util
    {
        public static long GetSystemUnixTime()
        {
            return (long)DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalSeconds;
        }

        public static byte[] HexStringToByteArray(string hex)
        {
            int length = hex.Length;
            byte[] array = new byte[length / 2];
            for (int i = 0; i < length; i += 2)
            {
                array[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
            }
            return array;
        }
    }


}
