using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ZMangaScanner.Entities;
using ZMangaScanner.Loggers;
using static System.Net.WebRequestMethods;

namespace ZMangaScanner.Utilities
{
    public static class WebHelper
    {

        public async static Task<String> GetIssueData(IConfigurationRoot config, string url)
        {
            using (HttpClient client = new HttpClient())
            {
                try
                {
                    HttpResponseMessage response = await client.GetAsync(url);
                    var rawData = await response.Content.ReadAsByteArrayAsync();                    ;

                    // Create UTF8 decoder that ignores invalid bytes
                    Encoding utf8 = Encoding.GetEncoding(
                        "UTF-8",
                        new EncoderReplacementFallback(""),
                        new DecoderReplacementFallback("") // ignore invalid sequences
                    );

                    return utf8.GetString(rawData);
                }
                catch (HttpRequestException e)
                {
                    EmailNotifier emailNotifier = new EmailNotifier(config);
                    emailNotifier.SendNotificationEmailError("GetTokenQueryString", e.ToString());
                }
            }

            return string.Empty;
        }

        public static bool IsIssueFound(string rawData)
        {
            if (rawData.Contains("コンテンツが見つかりませんでした"))
                return false;

            return true;
        }
        public static bool IsSearchResultFound(string rawData, string searchValue)
        {
            if (rawData.Contains(searchValue))
                return true;

            return false;
        }

        public static async Task<HttpStatusCode> GetUrlStatusCode(string url)
        {
            //var data = new byte[4];
            //new Random().NextBytes(data);
            //IPAddress ip = new IPAddress(data);
            using (HttpClient client = new HttpClient())
            {
                HttpResponseMessage response = await client.GetAsync(url);

                return response.StatusCode;
            }
        }

        public static HttpClient GetHttpClient(IPAddress address)
        {
            if (IPAddress.Any.Equals(address))
                return new HttpClient();

            SocketsHttpHandler handler = new SocketsHttpHandler();

            handler.ConnectCallback = async (context, cancellationToken) =>
            {
                Socket socket = new Socket(SocketType.Stream, ProtocolType.Tcp);

                socket.Bind(new IPEndPoint(address, 0));

                socket.NoDelay = true;

                try
                {
                    await socket.ConnectAsync(context.DnsEndPoint, cancellationToken).ConfigureAwait(false);

                    return new NetworkStream(socket, true);
                }
                catch
                {
                    socket.Dispose();

                    throw;
                }
            };

            return new HttpClient(handler);
        }

    }
}
