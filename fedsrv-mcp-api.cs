using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace fedsrv_mcp_api
{
    public class fedsrv_mcp_api_service
    {
        private readonly ILogger<fedsrv_mcp_api_service> _logger;

        public fedsrv_mcp_api_service(ILogger<fedsrv_mcp_api_service> logger)
        {
            _logger = logger;
        }

        [Function("mcp")]
        public string RunMcp([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "mcp")] HttpRequestData req)
        {
            _logger.LogInformation("Processing MCP handshake request.");

            var response = new
            {
                Protocol = "mcp",
                Version = "1.0",
                Capabilities = new[] { "inventory" }
            };

            return JsonSerializer.Serialize(response);
        }

        [Function("resources_read")]
        public string RunResourcesRead([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "resources/read")] HttpRequestData req)
        {
            _logger.LogInformation("Processing resources/read request.");

            var resources = new[]
            {
                new { Id = "agent:model1", Type = "agent", Name = "Phi-3-mini-4k-instruct", Description = "Agent for MCP interactions" },
                new { Id = "data:oil_wells", Type = "data", Name = "Oil Wells", Description = "Oil wells dataset" }
            };

            var response = new { Resources = resources };
            return JsonSerializer.Serialize(response);
        }

        [Function("tools_list")]
        public string RunToolsList([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "tools/list")] HttpRequestData req)
        {
            _logger.LogInformation("Processing tools/list request.");

            var tools = new[]
            {
                new
                {
                    Name = "query_oil_wells_seismic",
                    Description = "Query oil wells with seismic data",
                    InputSchema = new
                    {
                        Type = "object",
                        Properties = new { Filter = new { Type = "string", Description = "Optional filter, e.g., 'has_seismic_data=true'" } },
                        Required = new string[] { }
                    }
                }
            };

            var response = new { Tools = tools };
            return JsonSerializer.Serialize(response);
        }
    }
}
