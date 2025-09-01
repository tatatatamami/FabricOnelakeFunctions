using System.Globalization;
using CsvHelper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using function_onelake.Models;

namespace function_onelake.Endpoints;

public class GetEmployeesFiltered
{
    private readonly ILogger<GetEmployeesFiltered> _logger;
    private readonly HttpClient _httpClient;
    private const int MaxItems = 50;

    public GetEmployeesFiltered(ILogger<GetEmployeesFiltered> logger, HttpClient httpClient)
    {
        _logger = logger;
        _httpClient = httpClient;
    }

    [Function("GetEmployeesFiltered")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "employees")] HttpRequest req)
    {
        try
        {
            _logger.LogInformation("Processing GET /api/employees request");

            // Get the department filter from query parameters
            string? department = req.Query["department"];
            
            if (string.IsNullOrWhiteSpace(department))
            {
                return new BadRequestObjectResult(new { error = "Department parameter is required" });
            }

            // Get the CSV file URL from environment variables
            string? csvUrl = Environment.GetEnvironmentVariable("ONELAKE_DFS_FILE_URL");
            if (string.IsNullOrWhiteSpace(csvUrl))
            {
                _logger.LogError("ONELAKE_DFS_FILE_URL environment variable is not set");
                return new StatusCodeResult(500);
            }

            // Read and parse CSV data
            List<Employee> allEmployees;
            try
            {
                allEmployees = await ReadCsvDataAsync(csvUrl);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Failed to fetch CSV data from URL: {CsvUrl}", csvUrl);
                return new NotFoundObjectResult(new { error = "CSV file not found or inaccessible" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing CSV data");
                return new StatusCodeResult(500);
            }

            // Filter by department (case-insensitive)
            var filteredEmployees = allEmployees
                .Where(e => string.Equals(e.Department, department, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (!filteredEmployees.Any())
            {
                return new OkObjectResult(new EmployeeResponse
                {
                    Total = 0,
                    Department = department,
                    AverageSalary = 0,
                    Items = new List<Employee>()
                });
            }

            // Calculate average salary
            decimal averageSalary = filteredEmployees.Average(e => e.Salary);

            // Limit to max 50 items
            var limitedEmployees = filteredEmployees.Take(MaxItems).ToList();

            var response = new EmployeeResponse
            {
                Total = filteredEmployees.Count,
                Department = department,
                AverageSalary = Math.Round(averageSalary, 0), // Round to nearest whole number
                Items = limitedEmployees
            };

            return new OkObjectResult(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in GetEmployeesFiltered");
            return new StatusCodeResult(500);
        }
    }

    private async Task<List<Employee>> ReadCsvDataAsync(string csvUrl)
    {
        var response = await _httpClient.GetAsync(csvUrl);
        response.EnsureSuccessStatusCode();

        using var stream = await response.Content.ReadAsStreamAsync();
        using var reader = new StreamReader(stream);
        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);

        var employees = new List<Employee>();
        await foreach (var record in csv.GetRecordsAsync<Employee>())
        {
            employees.Add(record);
        }

        return employees;
    }
}