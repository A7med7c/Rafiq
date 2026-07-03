using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi;
using System.Reflection;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Rafiq.API.Extensions;

public sealed class SwaggerAuthorizeOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var endpointMetadata = context.ApiDescription.ActionDescriptor.EndpointMetadata;
        var actionAttributes = context.MethodInfo.GetCustomAttributes(inherit: true);
        var controllerAttributes = context.MethodInfo.DeclaringType?.GetCustomAttributes(inherit: true) ?? [];

        var allowsAnonymous =
            endpointMetadata.OfType<IAllowAnonymous>().Any() ||
            actionAttributes.OfType<IAllowAnonymous>().Any() ||
            controllerAttributes.OfType<IAllowAnonymous>().Any();

        if (allowsAnonymous)
        {
            return;
        }

        var requiresAuthorization =
            endpointMetadata.OfType<IAuthorizeData>().Any() ||
            actionAttributes.OfType<IAuthorizeData>().Any() ||
            controllerAttributes.OfType<IAuthorizeData>().Any();

        if (!requiresAuthorization)
        {
            return;
        }

        operation.Security ??= [];
        operation.Security.Add(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecuritySchemeReference(
                    JwtBearerDefaults.AuthenticationScheme,
                    hostDocument: null),
                []
            }
        });
    }
}

public sealed class SwaggerAuthorizeDocumentFilter : IDocumentFilter
{
    public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
    {
        if (swaggerDoc.Paths is null)
        {
            return;
        }

        foreach (var path in swaggerDoc.Paths.Values)
        {
            if (path.Operations is null)
            {
                continue;
            }

            foreach (var operation in path.Operations.Values)
            {
                if (operation.Security is not { Count: > 0 })
                {
                    continue;
                }

                operation.Security =
                [
                    new OpenApiSecurityRequirement
                    {
                        {
                            new OpenApiSecuritySchemeReference(
                                JwtBearerDefaults.AuthenticationScheme,
                                swaggerDoc),
                            []
                        }
                    }
                ];
            }
        }
    }
}
