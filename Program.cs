using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

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
                    // The BingBacService wraps the bing methods and url parsing. Populate it with today's json so we can begin retrieving values.
                    var bingService = services.GetRequiredService<IBingBacService>();
                    var bingJSON = await bingService.GetBingJson();
                    bingService.PopulateJson(bingJSON);

                    // Figure out the image url from the Json and place the image in a buffer for now
                    var relativeImageUrl = bingService.GetImageUrlFromJson();
                    var imageUrl = bingService.GetImagePath(relativeImageUrl);
                    ReadOnlyMemory<byte> imageBuffer = await bingService.DownloadImage(imageUrl);

                    // Create a path to the local disk and save the image buffer from earlier there
                    var year = bingService.GetImageYear();
                    var fileName = bingService.GetImageFilename();
                    var directory = FileSystemHelper.GetDirectory(year);
                    var fullPath = directory + fileName;

                    FileSystemHelper.SaveFileToDisk(directory, fullPath, imageBuffer);

                    SystemHelper.SetDesktopBackground(fullPath);
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

    public static class FileSystemHelper
    {
        public const string BingBac2FolderName = "BingBac2";
        
        public static string GetUsersPicturesFolder()
        {
            return Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
        }

        public static string GetDirectory(string year)
        {
            var result = GetUsersPicturesFolder() + @"\" + BingBac2FolderName + @"\" + year + @"\";

            return result;
        }

        public async static void SaveFileToDisk(string directory, string fullpath, ReadOnlyMemory<byte> image)
        {
            Directory.CreateDirectory(directory);

            using (FileStream fs = File.Create(fullpath))
            {
                await fs.WriteAsync(image);
            }
        }
    }

    public static class SystemHelper
    {
        public const int SPI_SETDESKWALLPAPER = 20;
        public const int SPIF_UPDATEINIFILE = 1;
        public const int SPIF_SENDCHANGE = 2;

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern int SystemParametersInfo(int uAction, int uParam, string lpvParam, int fuWinIni);

        public static void SetDesktopBackground(string imagePath)
        {
            //  Call a function in the user32.dll Win32 API to set the desktop wallpaper
            SystemParametersInfo(SPI_SETDESKWALLPAPER, 0, imagePath, SPIF_UPDATEINIFILE | SPIF_SENDCHANGE);
        }
    }
}
