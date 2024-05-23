using KubeOps.Operator.Webhooks;
using EmailOperator.Entities;

namespace EmailOperator.Webhooks;

public class EmailMutator : IMutationWebhook<Email>
{
    public AdmissionOperations Operations => AdmissionOperations.Create;

    public MutationResult Create(Email newEntity, bool dryRun)
    {
        //newEntity.Spec.Username = "not foobar";
        return MutationResult.Modified(newEntity);
    }
}
