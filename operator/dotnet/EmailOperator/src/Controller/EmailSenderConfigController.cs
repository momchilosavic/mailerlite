using k8s.Models;
using KubeOps.Operator.Controller;
using KubeOps.Operator.Controller.Results;
using KubeOps.Operator.Finalizer;
using KubeOps.Operator.Rbac;
using KubeOps.KubernetesClient;
using EmailOperator.Entities;
using EmailOperator.Finalizer;

namespace EmailOperator.Controller;

[EntityRbac(typeof(EmailSenderConfig), Verbs = RbacVerb.All)]
public class EmailSenderConfigController : IResourceController<EmailSenderConfig>
{
    private readonly IKubernetesClient _kuberentes;
    private readonly ILogger<EmailSenderConfigController> _logger;
    private readonly IFinalizerManager<EmailSenderConfig> _finalizerManager;

    public EmailSenderConfigController(IKubernetesClient kubernetes, ILogger<EmailSenderConfigController> logger, IFinalizerManager<EmailSenderConfig> finalizerManager)
    {
        _kuberentes = kubernetes;
        _logger = logger;
        _finalizerManager = finalizerManager;
    }

    public async Task<ResourceControllerResult?> ReconcileAsync(EmailSenderConfig entity)
    {
        _logger.LogInformation($"entity {entity.Name()} called {nameof(ReconcileAsync)}.");
        
        await _finalizerManager.RegisterFinalizerAsync<EmailSenderConfigFinalizer>(entity);

        return ResourceControllerResult.RequeueEvent(TimeSpan.FromSeconds(15));
    }

    public Task StatusModifiedAsync(EmailSenderConfig entity)
    {
        _logger.LogInformation($"entity {entity.Name()} called {nameof(StatusModifiedAsync)}.");

        return Task.CompletedTask;
    }

    public Task DeletedAsync(EmailSenderConfig entity)
    {
        _logger.LogInformation($"entity {entity.Name()} called {nameof(DeletedAsync)}.");

        return Task.CompletedTask;
    }
}
