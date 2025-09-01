using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Data.SqlClient;
using Azure.Identity;
using Azure.Core;
using System.Text.Json;

namespace FunctionsOneLakeDemo.Endpoints;

public class GetEmployeesBySql
{
    private readonly ILogger<GetEmployeesBySql> _logger;

    public GetEmployeesBySql(ILogger<GetEmployeesBySql> logger)
    {
        _logger = logger;
    }

    [Function("GetEmployeesBySql")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "employees/sql")] HttpRequest req)
    {
        _logger.LogInformation("Processing SQL employees aggregation request.");

        try
        {
            // Get environment variables
            var sqlEndpoint = Environment.GetEnvironmentVariable("SQL_ENDPOINT");
            var sqlDatabase = Environment.GetEnvironmentVariable("SQL_DATABASE");

            if (string.IsNullOrEmpty(sqlEndpoint) || string.IsNullOrEmpty(sqlDatabase))
            {
                _logger.LogError("SQL_ENDPOINT or SQL_DATABASE environment variables are not configured.");
                return new BadRequestObjectResult(new { error = "Database configuration missing. Please configure SQL_ENDPOINT and SQL_DATABASE environment variables." });
            }

            // Get department query parameter
            var department = req.Query["department"].ToString();

            // Get Entra ID access token
            var credential = new DefaultAzureCredential();
            var tokenRequestContext = new TokenRequestContext(new[] { "https://database.windows.net/.default" });
            var token = await credential.GetTokenAsync(tokenRequestContext);

            // Build connection string
            var connectionStringBuilder = new SqlConnectionStringBuilder
            {
                DataSource = sqlEndpoint,
                InitialCatalog = sqlDatabase,
                Encrypt = true,
                TrustServerCertificate = false,
                ConnectTimeout = 30,
                CommandTimeout = 30
            };

            var connectionString = connectionStringBuilder.ConnectionString;

            // Build SQL query
            string sqlQuery;
            List<SqlParameter> parameters = new List<SqlParameter>();

            if (!string.IsNullOrEmpty(department))
            {
                sqlQuery = @"
                    SELECT 
                        COUNT(*) as Total,
                        AVG(CAST(salary AS FLOAT)) as AverageSalary
                    FROM dbo.employees 
                    WHERE department = @department";
                parameters.Add(new SqlParameter("@department", department));
            }
            else
            {
                sqlQuery = @"
                    SELECT 
                        COUNT(*) as Total,
                        AVG(CAST(salary AS FLOAT)) as AverageSalary
                    FROM dbo.employees";
            }

            // Execute query
            using var connection = new SqlConnection(connectionString);
            connection.AccessToken = token.Token;

            await connection.OpenAsync();

            using var command = new SqlCommand(sqlQuery, connection);
            foreach (var param in parameters)
            {
                command.Parameters.Add(param);
            }

            using var reader = await command.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                var result = new
                {
                    total = reader.GetInt32(0),  // First column: Total
                    department = !string.IsNullOrEmpty(department) ? department : null,
                    averageSalary = reader.IsDBNull(1) ? 0 : (int)Math.Round(reader.GetDouble(1))  // Second column: AverageSalary
                };

                // Remove null properties for cleaner JSON
                var options = new JsonSerializerOptions
                {
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                };

                return new OkObjectResult(result);
            }
            else
            {
                return new OkObjectResult(new { total = 0, department = department, averageSalary = 0 });
            }
        }
        catch (SqlException sqlEx)
        {
            _logger.LogError(sqlEx, "SQL error occurred while querying employee data.");
            return new ObjectResult(new { error = "Database connection failed. Please check the SQL endpoint configuration and ensure the database is accessible." })
            {
                StatusCode = 500
            };
        }
        catch (AuthenticationFailedException authEx)
        {
            _logger.LogError(authEx, "Authentication failed while connecting to database.");
            return new ObjectResult(new { error = "Authentication failed. Please ensure Entra ID authentication is properly configured." })
            {
                StatusCode = 500
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error occurred while processing SQL employees request.");
            return new ObjectResult(new { error = "An unexpected error occurred while processing the request." })
            {
                StatusCode = 500
            };
        }
    }
}