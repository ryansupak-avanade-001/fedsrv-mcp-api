using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System.Text.Json;
using System.Text.Json.Serialization;

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
        public IActionResult RunMcp([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = "mcp")] HttpRequest req)
        {
            _logger.LogInformation("Processing MCP handshake request.");

            if (req.Method == "GET")
            {
                var response = new
                {
                    Protocol = "mcp",
                    Version = "1.0",
                    Capabilities = new[] { "inventory" }
                };
                return new OkObjectResult(response);
            }
            else // POST
            {
                return HandleJsonRpcRequest(req, "mcp", () => new
                {
                    Protocol = "mcp",
                    Version = "1.0",
                    Capabilities = new[] { "inventory" }
                });
            }
        }

        [Function("resources_read")]
        public IActionResult RunResourcesRead([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = "resources/read")] HttpRequest req)
        {
            _logger.LogInformation("Processing resources/read request.");

            if (req.Method == "GET")
            {
                var resources = new[]
                {
                    new { Id = "agent:model1", Type = "agent", Name = "Phi-3-mini-4k-instruct", Description = "Agent for MCP interactions" },
                    new { Id = "data:oil_wells", Type = "data", Name = "Oil Wells", Description = "Oil wells dataset" }
                };
                var response = new { Resources = resources };
                return new OkObjectResult(response);
            }
            else // POST
            {
                return HandleJsonRpcRequest(req, "resources/read", () => new
                {
                    Resources = new[]
                    {
                        new { Id = "agent:model1", Type = "agent", Name = "Phi-3-mini-4k-instruct", Description = "Agent for MCP interactions" },
                        new { Id = "data:oil_wells", Type = "data", Name = "Oil Wells", Description = "Oil wells dataset" }
                    }
                });
            }
        }

        [Function("tools_list")]
        public IActionResult RunToolsList([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = "tools/list")] HttpRequest req)
        {
            _logger.LogInformation("Processing tools/list request.");

            if (req.Method == "GET")
            {
                var tools = GetTools();
                var response = new { Tools = tools };
                return new OkObjectResult(response);
            }
            else // POST
            {
                return HandleJsonRpcRequest(req, "tools/list", () => new { Tools = GetTools() });
            }
        }

        [Function("tools")]
        public IActionResult RunTools([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = "tools")] HttpRequest req)
        {
            _logger.LogInformation("Processing tools request.");

            if (req.Method == "GET")
            {
                var tools = GetTools();
                var response = new { Tools = tools };
                return new OkObjectResult(response);
            }
            else // POST
            {
                return HandleJsonRpcRequest(req, "tools", () => new { Tools = GetTools() });
            }
        }

        private object[] GetTools()
        {
            return new[]
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
        }

        private IActionResult HandleJsonRpcRequest(HttpRequest req, string expectedMethod, Func<object> getResult)
        {
            try
            {
                using var reader = new StreamReader(req.Body);
                string body = reader.ReadToEnd();
                var json = JsonSerializer.Deserialize<JsonElement>(body);

                if (!json.TryGetProperty("jsonrpc", out var jsonrpc) || jsonrpc.GetString() != "2.0" ||
                    !json.TryGetProperty("method", out var method) || method.GetString() != expectedMethod ||
                    !json.TryGetProperty("id", out var id))
                {
                    return new BadRequestObjectResult(new
                    {
                        jsonrpc = "2.0",
                        error = new { code = -32600, message = "Invalid Request" },
                        id = json.TryGetProperty("id", out var idVal) ? idVal.GetInt32() : (int?)null
                    });
                }

                var result = getResult();
                return new OkObjectResult(new
                {
                    jsonrpc = "2.0",
                    result,
                    id = id.GetInt32()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing JSON-RPC request.");
                return new BadRequestObjectResult(new
                {
                    jsonrpc = "2.0",
                    error = new { code = -32603, message = "Internal error" },
                    id = (int?)null
                });
            }
        }
    }
}
