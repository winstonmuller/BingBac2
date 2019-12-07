using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

public interface IBingBacService
{
    Task<string> GetBingJson();
    Task<byte[]> DownloadImage(string imageUrl);

    string GetImageFilename();

    string GetImageUrlFromJson();

    string GetImageYear();

    // Populates the JSON result so we can parse it for values
    void PopulateJson(string bingJson);

    string GetImagePath(string relativeUrl);
}
// This class handles the retrieving of JSON from Bing, the parsing of it's urls and retrieving an image from Bing
public class BingBacService : IBingBacService
{
    private readonly IHttpClientFactory _clientFactory;
    private JObject _jsonResult;

    public void PopulateJson(string bingJSON)
    {
        _jsonResult = JObject.Parse(bingJSON);
    }

    public BingBacService(IHttpClientFactory clientFactory)
    {
        _clientFactory = clientFactory;
    }

    public async Task<string> GetBingJson()
    {
        /* This url is the one that the Bing website uses to check for image information, so we'll use it too.
           It returns a JSON object with a few properties including the date that the image is for and a 
           relative path to the image that we can re-create the background image url from. */
        var url = @"http://www.bing.com/HPImageArchive.aspx?format=js&idx=0&n=1&mkt=en-US";

        var request = new HttpRequestMessage(HttpMethod.Get, url);
        var client = _clientFactory.CreateClient();
        var response = await client.SendAsync(request);

        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadAsStringAsync();
        }
        else
        {
            return await Task.Run(() => response.StatusCode.ToString());
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

    public string GetImageFilename()
    {
        return _jsonResult.SelectToken("$.images[0].startdate").Value<string>() + ".jpg";
    }

    public string GetImageUrlFromJson()
    {
        return _jsonResult.SelectToken("$.images[0].url").Value<string>();
    }

    public string GetImagePath(string relativeUrl)
    {
        return $"https://www.bing.com{relativeUrl}";
    }

    // Over new years it's possible to get a difference in timezones,
    // so we will use the year from the json instead of DateTime.Now();
    public string GetImageYear()
    {
        var startDate = _jsonResult.SelectToken("$.images[0].startdate").Value<string>();

        return startDate.Substring(0, 4);
    }
}