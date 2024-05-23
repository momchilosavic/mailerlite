using k8s.Models;
using KubeOps.Operator.Finalizer;
using EmailOperator.Entities;

namespace EmailOperator.Finalizer;

public class EmailFinalizer : IResourceFinalizer<Email>
{
    private readonly ILogger<EmailFinalizer> _logger;

    public EmailFinalizer(ILogger<EmailFinalizer> logger)
    {
        _logger = logger;
    }

    public Task FinalizeAsync(Email entity)
    {
        _logger.LogInformation($"entity {entity.Name()} called {nameof(FinalizeAsync)}.");

        return Task.CompletedTask;
    }
}
