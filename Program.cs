using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace BingBac2
{
    public class images
    {
        public string urlbase;
    }

    class Program
    {
        static async Task<int> Main(string[] args)
        {
            var builder = new HostBuilder()
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddHttpClient();
                    services.AddTransient<IBingBacService, BingBacService>();
                }).UseConsoleLifetime();

            var host = builder.Build();

            using (var serviceScope = host.Services.CreateScope())
            {
                var services = serviceScope.ServiceProvider;

                try
                {
                    //  I created a Service called BingBacService that we can use to retrieve data from the Bing endpoints
                    //  We need to create an instance of the it so we can start retrieving information and eventually get to the background URL
                    var bingService = services.GetRequiredService<IBingBacService>();
                    var bingJSON = await bingService.GetBingJson();

                    // ParseJSON for Image URL
                    //  The GetBingJson is going to return a big blob of JSON that we need to deserialize and extract the URL from
                    var output = JsonSerializer.Deserialize<images>(bingJSON, new JsonSerializerOptions {
                        AllowTrailingCommas = true
                    });

                    Console.WriteLine(output.urlbase);

                    //  Generate Image URL
                    string imageUrl = $"https://www.bing.com/{bingJSON}";

                    //Console.WriteLine(imageUrl);

                    //  Request Image
                    var imageResult = await bingService.DownloadImage(imageUrl);

                    //Console.WriteLine(pageContent.Substring(0, 500));
                    // Request the iotd
                    // Save the image to hard disk inside the user's pictures folder
                    // Set the image as the desktop background
                }
                catch (Exception ex)
                {
                    var logger = services.GetRequiredService<ILogger<Program>>();

                    logger.LogError(ex, "An error occurred.");
                }
            }

            return 0;
        }

        public interface IBingBacService
        {
            Task<byte[]> GetBingJson();
            Task<byte[]> DownloadImage(string imageUrl);

        }

        public class BingBacService : IBingBacService
        {
            private readonly IHttpClientFactory _clientFactory;

            public BingBacService(IHttpClientFactory clientFactory)
            {
                _clientFactory = clientFactory;
            }

            public async Task<byte[]> GetBingJson()
            {
                Console.WriteLine("Time to download a new wallpaper.");

                // First we call the URL to find out what the name is of the image of the day

                String url = @"http://www.bing.com/HPImageArchive.aspx?format=js&idx=0&n=1&mkt=en-US";

                var request = new HttpRequestMessage(HttpMethod.Get, url);
                var client = _clientFactory.CreateClient();
                var response = await client.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadAsByteArrayAsync();
                }
                else
                {
                    // response.StatusCode
                    return Encoding.UTF8.GetBytes(response.StatusCode.ToString());
                }
            }

            public async Task<byte[]> DownloadImage(string imageUrl)
            {
                var client = _clientFactory.CreateClient();

                var result = await client.GetAsync(imageUrl);

                if (result.IsSuccessStatusCode)
                {
                    return await result.Content.ReadAsByteArrayAsync();
                }

                return null;
            }
        }
    }
}
