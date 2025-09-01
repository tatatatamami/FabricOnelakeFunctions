using Azure.Identity;
using Azure.Storage.Files.DataLake;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;

namespace function_onelake.Endpoints;

public class GetFilePassthrough
{
    private readonly ILogger<GetFilePassthrough> _logger;
    private readonly DefaultAzureCredential _credential;

    public GetFilePassthrough(ILogger<GetFilePassthrough> logger, DefaultAzureCredential credential)
    {
        _logger = logger;
        _credential = credential;
    }

    [Function("GetFilePassthrough")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "files/raw")] HttpRequestData req)
    {
        _logger.LogInformation("Processing request for OneLake CSV file.");

        try
        {
            // 環境変数から OneLake のファイル URL を取得
            var oneLakeFileUrl = Environment.GetEnvironmentVariable("ONELAKE_DFS_FILE_URL");
            _logger.LogInformation("ONELAKE_DFS_FILE_URL = {Url}", oneLakeFileUrl);

            if (string.IsNullOrEmpty(oneLakeFileUrl))
            {
                _logger.LogError("ONELAKE_DFS_FILE_URL environment variable is not set.");
                return req.CreateResponse(HttpStatusCode.InternalServerError);
            }

            // OneLake が要求する API バージョンを明示（2023-11-03）
            var dlOptions = new DataLakeClientOptions(DataLakeClientOptions.ServiceVersion.V2023_11_03);

            // まずは Azure CLI と同じ資格情報で動かしてみる（動作確認用）
            // デモで問題なければ _credential に差し替え可能
            var credential = new AzureCliCredential();

            // FileClient を生成
            var fileClient = new DataLakeFileClient(new Uri(oneLakeFileUrl), credential, dlOptions);

            // ファイル存在確認（任意、なくても Read 側で 404 を拾える）
            var existsResponse = await fileClient.ExistsAsync();
            if (!existsResponse.Value)
            {
                _logger.LogWarning("File not found at URL: {FileUrl}", oneLakeFileUrl);
                return req.CreateResponse(HttpStatusCode.NotFound);
            }

            // ファイルをダウンロード
            var downloadResponse = await fileClient.ReadAsync();

            var resp = req.CreateResponse(HttpStatusCode.OK);
            resp.Headers.Add("Content-Type", "text/csv; charset=utf-8");
            await downloadResponse.Value.Content.CopyToAsync(resp.Body);

            _logger.LogInformation("Successfully retrieved CSV file from OneLake.");
            return resp;
        }
        catch (Azure.RequestFailedException ex) when (ex.Status == 403)
        {
            _logger.LogError(ex, "Access forbidden when trying to access OneLake file.");
            return req.CreateResponse(HttpStatusCode.Forbidden);
        }
        catch (Azure.RequestFailedException ex) when (ex.Status == 404)
        {
            _logger.LogError(ex, "File not found in OneLake.");
            return req.CreateResponse(HttpStatusCode.NotFound);
        }
        catch (Azure.RequestFailedException ex)
        {
            _logger.LogError(ex, "Azure request failed with status {Status}: {Message}", ex.Status, ex.Message);
            return req.CreateResponse(HttpStatusCode.InternalServerError);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error occurred while processing OneLake file request.");
            return req.CreateResponse(HttpStatusCode.InternalServerError);
        }
    }
}
