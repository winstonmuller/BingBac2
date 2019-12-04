using System;
using System.IO;
using System.Runtime.InteropServices;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace BingBac2
{
    
    class Program
    {
        public const int SPI_SETDESKWALLPAPER = 20;
        public const int SPIF_UPDATEINIFILE = 1;
        public const int SPIF_SENDCHANGE = 2;

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern int SystemParametersInfo(int uAction, int uParam, string lpvParam, int fuWinIni);

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
                    var jsonResult = JObject.Parse(bingJSON);
                    var filename = jsonResult.SelectToken("$.images[0].startdate").Value<string>() + ".jpg";
                    var relativeURL = jsonResult.SelectToken("$.images[0].url").Value<string>();

                    //  If we add the relative url of the image to the domain name, we will get the url of todays image
                    string imageUrl = $"https://www.bing.com{relativeURL}";

                    //  Put the image inside an object so we can save it to disk
                    var imageResult = await bingService.DownloadImage(imageUrl);
                    
                    // TODO: Make this path robust
                    string path = @"C:\Users\winst\OneDrive\Pictures\BingBac2\2019\";
                    string localImagePath = path + filename;

                    using (FileStream fs = File.Create(localImagePath))
                    {
                        await fs.WriteAsync(imageResult);
                    }

                    //  Call a function in the user32.dll Win32 API to set the desktop wallpaper
                    SystemParametersInfo(SPI_SETDESKWALLPAPER, 0, localImagePath, SPIF_UPDATEINIFILE | SPIF_SENDCHANGE);
                }
                catch (Exception ex)
                {
                    var logger = services.GetRequiredService<ILogger<Program>>();

                    logger.LogError(ex, "An error occurred.");

                    Console.WriteLine(ex.Message.ToString());
                }
            }

            return 0;
        }
    }
}
