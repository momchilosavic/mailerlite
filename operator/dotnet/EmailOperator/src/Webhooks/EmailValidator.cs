using KubeOps.Operator.Webhooks;
using EmailOperator.Entities;

namespace EmailOperator.Webhooks;

public class EmailValidator : IValidationWebhook<Email>
{
    public AdmissionOperations Operations => AdmissionOperations.Create;

    public ValidationResult Create(Email newEntity, bool dryRun)
        => newEntity.Spec.Subject == "forbiddenUsername"
            ? ValidationResult.Fail(StatusCodes.Status400BadRequest, "Username is forbidden")
            : ValidationResult.Success();
}
