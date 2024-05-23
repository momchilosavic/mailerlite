using k8s.Models;
using KubeOps.Operator.Finalizer;
using EmailOperator.Entities;

namespace EmailOperator.Finalizer;

public class EmailSenderConfigFinalizer : IResourceFinalizer<EmailSenderConfig>
{
    private readonly ILogger<EmailSenderConfigFinalizer> _logger;

    public EmailSenderConfigFinalizer(ILogger<EmailSenderConfigFinalizer> logger)
    {
        _logger = logger;
    }

    public Task FinalizeAsync(EmailSenderConfig entity)
    {
        _logger.LogInformation($"entity {entity.Name()} called {nameof(FinalizeAsync)}.");

        return Task.CompletedTask;
    }
}
