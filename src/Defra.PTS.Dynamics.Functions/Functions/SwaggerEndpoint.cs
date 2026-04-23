using System.Diagnostics.CodeAnalysis;
using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Defra.PTS.Dynamics.Functions.Functions;

[ExcludeFromCodeCoverage]
public static class SwaggerEndpoint
{
    private static readonly string OpenApiSpec = /*lang=json*/ """
    {
      "openapi": "3.0.1",
      "info": {
        "title": "Defra PTS Dynamics Functions",
        "version": "1.0.0",
        "description": "Pet Travel Scheme - Dynamics integration functions"
      },
      "paths": {
        "/health": {
          "get": {
            "operationId": "HealthCheck",
            "summary": "Health check endpoint",
            "tags": ["Health"],
            "responses": {
              "200": { "description": "Services are healthy" },
              "503": { "description": "One or more services are unavailable" }
            }
          }
        },
        "/writetoqueue": {
          "post": {
            "operationId": "WriteApplicationToQueue",
            "summary": "Submit an application message to the Service Bus queue",
            "tags": ["Queue"],
            "requestBody": {
              "required": true,
              "content": {
                "application/json": {
                  "schema": {
                    "type": "object",
                    "properties": {
                      "applicationId": { "type": "string", "format": "uuid" }
                    }
                  }
                }
              }
            },
            "responses": {
              "200": {
                "description": "Message added to queue successfully",
                "content": {
                  "application/json": {
                    "schema": { "type": "string" }
                  }
                }
              }
            }
          }
        },
        "/fetchupdateaddress": {
          "post": {
            "operationId": "FetchAndUpdateAddress",
            "summary": "Fetch contact details from Dynamics and update the user address",
            "tags": ["Address"],
            "requestBody": {
              "required": true,
              "content": {
                "application/json": {
                  "schema": {
                    "type": "object",
                    "properties": {
                      "contactId": { "type": "string", "format": "uuid" }
                    }
                  }
                }
              }
            },
            "responses": {
              "200": {
                "description": "Address updated successfully",
                "content": {
                  "application/json": {
                    "schema": { "type": "string" }
                  }
                }
              },
              "404": {
                "description": "User not found for the given contact"
              }
            }
          }
        }
      }
    }
    """;

    private static readonly string SwaggerHtml = """
    <!DOCTYPE html>
    <html lang="en">
    <head>
        <meta charset="UTF-8" />
        <title>Defra PTS Dynamics Functions - Swagger UI</title>
        <link rel="stylesheet" href="https://unpkg.com/swagger-ui-dist@5/swagger-ui.css" />
    </head>
    <body>
        <div id="swagger-ui"></div>
        <script src="https://unpkg.com/swagger-ui-dist@5/swagger-ui-bundle.js"></script>
        <script>
            SwaggerUIBundle({
                url: '/api/swagger.json',
                dom_id: '#swagger-ui',
                presets: [SwaggerUIBundle.presets.apis, SwaggerUIBundle.SwaggerUIStandalonePreset],
                layout: 'BaseLayout'
            });
        </script>
    </body>
    </html>
    """;

    [Function("SwaggerJson")]
    public static IActionResult GetSwaggerJson(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "api/swagger.json")] HttpRequest req)
    {
        return new ContentResult
        {
            Content = OpenApiSpec,
            ContentType = "application/json",
            StatusCode = (int)HttpStatusCode.OK
        };
    }

    [Function("SwaggerUI")]
    public static IActionResult GetSwaggerUI(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "swagger")] HttpRequest req)
    {
        return new ContentResult
        {
            Content = SwaggerHtml,
            ContentType = "text/html",
            StatusCode = (int)HttpStatusCode.OK
        };
    }
}
