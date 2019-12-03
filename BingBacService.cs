using System.Net.Http;
using System.Threading.Tasks;

public interface IBingBacService
{
    Task<string> GetBingJson();
    Task<byte[]> DownloadImage(string imageUrl);

}
public class BingBacService : IBingBacService
{
    private readonly IHttpClientFactory _clientFactory;

    public BingBacService(IHttpClientFactory clientFactory)
    {
        _clientFactory = clientFactory;
    }

    public async Task<string> GetBingJson()
    {
        /*
            This url is the one that the Bing website uses to check for image information, so we'll use it too.
            It returns a JSON object with a few properties including the date that the image is for and a 
            relative path to the image that we can re-create the background image url from.
        */
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
}