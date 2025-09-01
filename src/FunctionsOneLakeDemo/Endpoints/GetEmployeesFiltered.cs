using System.Globalization;
using System.Net;
using Azure.Identity;
using Azure.Storage.Files.DataLake;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using function_onelake.Models;

namespace function_onelake.Endpoints;

public class GetEmployeesFiltered
{
    private readonly ILogger<GetEmployeesFiltered> _logger;
    private const int MaxItems = 50;

    public GetEmployeesFiltered(ILogger<GetEmployeesFiltered> logger)
    {
        _logger = logger;
    }

    [Function("GetEmployeesFiltered")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "employees")] HttpRequestData req)
    {
        try
        {
            _logger.LogInformation("Processing GET /api/employees request");

            // クエリ: department 必須
            var query = System.Web.HttpUtility.ParseQueryString(req.Url.Query);
            var department = query.Get("department");
            if (string.IsNullOrWhiteSpace(department))
            {
                var bad = req.CreateResponse(HttpStatusCode.BadRequest);
                await bad.WriteStringAsync("Query parameter 'department' is required.");
                return bad;
            }

            // OneLake CSV の URL
            var csvUrl = Environment.GetEnvironmentVariable("ONELAKE_DFS_FILE_URL");
            if (string.IsNullOrWhiteSpace(csvUrl))
            {
                _logger.LogError("ONELAKE_DFS_FILE_URL environment variable is not set.");
                return req.CreateResponse(HttpStatusCode.InternalServerError);
            }

            // OneLake は 2023-11-03 の API バージョンを使用
            var options = new DataLakeClientOptions(DataLakeClientOptions.ServiceVersion.V2023_11_03);

            // まずは Azure CLI 資格情報で動作確認（必要なら DefaultAzureCredential に切替）
            var credential = new AzureCliCredential();

            var fileClient = new DataLakeFileClient(new Uri(csvUrl), credential, options);

            // CSV をストリームで読み込み
            var download = await fileClient.ReadAsync();
            using var stream = download.Value.Content;
            using var reader = new StreamReader(stream);
            using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = true,
                TrimOptions = TrimOptions.Trim,
                DetectDelimiter = true,
                BadDataFound = null
            });

            // 列名: id,name,age,department,salary を Employee にマップ
            csv.Context.RegisterClassMap<EmployeeMap>();

            // フィルタ（大文字小文字無視）
            var deptLower = department.Trim().ToLowerInvariant();
            var employees = new List<Employee>();
            await foreach (var rec in csv.GetRecordsAsync<Employee>())
            {
                if ((rec.Department ?? "").Trim().ToLowerInvariant() == deptLower)
                {
                    employees.Add(rec);
                }
            }

            // レスポンス生成
            if (employees.Count == 0)
            {
                var okEmpty = req.CreateResponse(HttpStatusCode.OK);
                await okEmpty.WriteAsJsonAsync(new EmployeeResponse
                {
                    Total = 0,
                    Department = department,
                    AverageSalary = 0,
                    Items = new List<Employee>()
                });
                return okEmpty;
            }

            var avg = Math.Round(employees.Average(e => e.Salary));
            var items = employees.Take(MaxItems).ToList();

            var ok = req.CreateResponse(HttpStatusCode.OK);
            await ok.WriteAsJsonAsync(new EmployeeResponse
            {
                Total = employees.Count,
                Department = department,
                AverageSalary = avg,
                Items = items
            });
            return ok;
        }
        catch (Azure.RequestFailedException ex) when (ex.Status == 404)
        {
            _logger.LogWarning(ex, "CSV file not found or inaccessible in OneLake.");
            return req.CreateResponse(HttpStatusCode.NotFound);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in GetEmployeesFiltered.");
            return req.CreateResponse(HttpStatusCode.InternalServerError);
        }
    }

    // CsvHelper マッピング（CSVヘッダーに一致）
    private sealed class EmployeeMap : ClassMap<Employee>
    {
        public EmployeeMap()
        {
            Map(m => m.Id).Name("id");
            Map(m => m.Name).Name("name");
            Map(m => m.Age).Name("age");
            Map(m => m.Department).Name("department");
            Map(m => m.Salary).Name("salary");
        }
    }
}
