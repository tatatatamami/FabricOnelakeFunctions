using Azure.Identity;
using Azure.Storage.Files.DataLake;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Net;

namespace function_onelake.Endpoints;

public class GetFilePassthrough
{
    private readonly ILogger<GetFilePassthrough> _logger;

    public GetFilePassthrough(ILogger<GetFilePassthrough> logger)
    {
        _logger = logger;
    }

    [Function("GetFilePassthrough")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "files/raw")] HttpRequest req)
    {
        _logger.LogInformation("Processing request for OneLake CSV file.");

        try
        {
            // Get the OneLake DFS file URL from environment variables
            var oneLakeFileUrl = Environment.GetEnvironmentVariable("ONELAKE_DFS_FILE_URL");
            
            if (string.IsNullOrEmpty(oneLakeFileUrl))
            {
                _logger.LogError("ONELAKE_DFS_FILE_URL environment variable is not set.");
                return new StatusCodeResult((int)HttpStatusCode.InternalServerError);
            }

            // Create DataLakeFileClient with DefaultAzureCredential
            var dataLakeFileClient = new DataLakeFileClient(new Uri(oneLakeFileUrl), new DefaultAzureCredential());

            // Check if file exists
            var existsResponse = await dataLakeFileClient.ExistsAsync();
            if (!existsResponse.Value)
            {
                _logger.LogWarning("File not found at URL: {FileUrl}", oneLakeFileUrl);
                return new StatusCodeResult((int)HttpStatusCode.NotFound);
            }

            // Download the file content
            var downloadResponse = await dataLakeFileClient.ReadAsync();
            
            // Set the content type for CSV
            var result = new FileStreamResult(downloadResponse.Value.Content, "text/csv; charset=utf-8");
            
            _logger.LogInformation("Successfully retrieved CSV file from OneLake.");
            return result;
        }
        catch (Azure.RequestFailedException ex) when (ex.Status == 403)
        {
            _logger.LogError(ex, "Access forbidden when trying to access OneLake file.");
            return new StatusCodeResult((int)HttpStatusCode.Forbidden);
        }
        catch (Azure.RequestFailedException ex) when (ex.Status == 404)
        {
            _logger.LogError(ex, "File not found in OneLake.");
            return new StatusCodeResult((int)HttpStatusCode.NotFound);
        }
        catch (Azure.RequestFailedException ex)
        {
            _logger.LogError(ex, "Azure request failed with status {Status}: {Message}", ex.Status, ex.Message);
            return new StatusCodeResult((int)HttpStatusCode.InternalServerError);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error occurred while processing OneLake file request.");
            return new StatusCodeResult((int)HttpStatusCode.InternalServerError);
        }
    }
}