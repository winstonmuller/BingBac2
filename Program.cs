using System;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.IO;

namespace BingBac2
{
    
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
                    var jsonResult = JObject.Parse(bingJSON);
                    var filename = jsonResult.SelectToken("$.images[0].startdate").Value<string>() + ".jpg";
                    var relativeURL = jsonResult.SelectToken("$.images[0].url").Value<string>();

                    //  If we add the relative url of the image to the domain name, we will get the url of todays image
                    string imageUrl = $"https://www.bing.com{relativeURL}";

                    //  Put the image inside an object so we can save it to disk
                    var imageResult = await bingService.DownloadImage(imageUrl);
                    
                    // TODO: Make this path robust
                    string path = @"C:\Users\winst\OneDrive\Pictures\BingBac2\2019\";

                    using (FileStream fs = File.Create(path + filename))
                    {
                        await fs.WriteAsync(imageResult);
                    }

                    // TODO: Set the image as the desktop background
                    // Coming up: .NET Interoperability starring dotnet core:
                    //  https://github.com/dotnet/samples/tree/master/core/extensions/ExcelDemo

                    
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
