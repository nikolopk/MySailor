using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace MySailorApi.Filters;

public class OptionalCitySwaggerParamFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, Swashbuckle.AspNetCore.SwaggerGen.OperationFilterContext context)
    {
        if (operation.Parameters is not null)
        {
            var cityParameter = operation.Parameters.SingleOrDefault(p => p.Name == "city" && p.In == ParameterLocation.Path);
            if (cityParameter != null)
            {
                cityParameter.AllowEmptyValue = true;
                cityParameter.Schema.Nullable = true;
            }
        }
    }
}