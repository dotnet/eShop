namespace Webhooks.API.Extensions;

public static class RouteHandlerBuilderExtensions
{
    public static RouteHandlerBuilder ValidateWebhookSubscriptionRequest(this RouteHandlerBuilder routeHandlerBuilder)
    {
        return routeHandlerBuilder.AddEndpointFilter(async (context, next) =>
        {
            var webhookSubscriptionRequest = context.Arguments.OfType<WebhookSubscriptionRequest>().SingleOrDefault();

            if (webhookSubscriptionRequest == null)
            {
                return TypedResults.BadRequest("No WebhookSubscriptionRequest found.");
            }

            var validationResults = webhookSubscriptionRequest.Validate(new ValidationContext(webhookSubscriptionRequest));

            if (validationResults.Any())
            {
                return TypedResults.ValidationProblem(validationResults.ToErrors());
            }

            return await next(context);
        });
    }

    private static Dictionary<string, string[]> ToErrors(this IEnumerable<ValidationResult> validationResults)
    {
        Dictionary<string, string[]> errors = [];

        foreach (var validationResult in validationResults)
        {
            var propertyNames = validationResult.MemberNames.Any() ? validationResult.MemberNames : [string.Empty];

            foreach (string propertyName in propertyNames)
            {
                if (errors.TryGetValue(propertyName, out var value))
                {
                    errors[propertyName] = [..value, validationResult.ErrorMessage];
                }
                else
                {
                    errors.Add(propertyName, [validationResult.ErrorMessage]);  
                }
            }
        }
        return errors;
    }
}
