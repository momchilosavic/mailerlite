using System.Text;
using k8s.Models;
using KubeOps.Operator.Controller;
using KubeOps.Operator.Controller.Results;
using KubeOps.Operator.Finalizer;
using KubeOps.Operator.Rbac;
using KubeOps.KubernetesClient;
using EmailOperator.Entities;
using EmailOperator.Finalizer;
using EmailOperator.Utils;
using Newtonsoft.Json;

namespace EmailOperator.Controller;

[EntityRbac(typeof(Email), Verbs = RbacVerb.All)]
public class EmailController : IResourceController<Email>
{
    private const string API_TOKEN_SECRET_KEY = "token";
    private const string MAIL_SERVICE_ENDPOINT = "https://connect.mailerlite.com";
    
    private readonly IKubernetesClient _kubernetes;
    private readonly ILogger<EmailController> _logger;
    private readonly IFinalizerManager<Email> _finalizerManager;
    private readonly HttpClient _httpClient;

    public EmailController(IKubernetesClient kubernetes, ILogger<EmailController> logger, IFinalizerManager<Email> finalizerManager)
    {
        _kubernetes = kubernetes;
        _logger = logger;
        _finalizerManager = finalizerManager;
        _httpClient = new HttpClient();
    }

    public async Task<ResourceControllerResult?> ReconcileAsync(Email email)
    {
        _logger.LogInformation($"entity {email.Name()} called {nameof(ReconcileAsync)}.");

///////////////////////////////////////////
        try{
            var emailSenderConfig = await _kubernetes.Get<EmailSenderConfig>(email.Spec.SenderConfigRef, email.Metadata.Namespace());
            if (emailSenderConfig == null) throw new Exception("Sender config not found");

            var apiTokenSecret = await _kubernetes.Get<V1Secret>(emailSenderConfig.Spec.ApiTokenSecretRef, email.Metadata.Namespace() ?? "default");
            if (apiTokenSecret == null) throw new Exception("API Token secret not found");

            var apiToken = Encoding.UTF8.GetString(apiTokenSecret.Data[API_TOKEN_SECRET_KEY]);

            EmailStatus emailStatus = await SendEmailAsync(apiToken, emailSenderConfig.Spec.SenderEmail, email.Spec.RecipientEmail, email.Spec.Subject, email.Spec.Body);
            await UpdateEmailStatusAsync(email, emailStatus);
        }
        catch(Exception ex) {
            _logger.LogError(ex, "Error reconciling Email");
            await UpdateEmailStatusAsync(email, "Failed", null, ex.Message);
        }

////////////////////////////////////////////////

        await _finalizerManager.RegisterFinalizerAsync<EmailFinalizer>(email);

        return ResourceControllerResult.RequeueEvent(TimeSpan.FromSeconds(15));
    }

    public Task StatusModifiedAsync(Email entity)
    {
        _logger.LogInformation($"entity {entity.Name()} called {nameof(StatusModifiedAsync)}.");

        return Task.CompletedTask;
    }

    public Task DeletedAsync(Email entity)
    {
        _logger.LogInformation($"entity {entity.Name()} called {nameof(DeletedAsync)}.");

        return Task.CompletedTask;
    }

    private async Task<EmailStatus> SendEmailAsync(string apiToken, string senderEmail, string recipientEmail, string subject, string body){
        var payloadJson = JsonConvert.SerializeObject(new { 
            from = new {
                email = senderEmail
            },
            to = new[] {
                new {
                    email = recipientEmail
                }
            },
            subject,
            text = body
        });
        var content = new StringContent(payloadJson, Encoding.UTF8, "application/json");

        var request = new HttpRequestMessage(HttpMethod.Post, MAIL_SERVICE_ENDPOINT){
            Headers = {
                { "Authorization", $"Bearer {apiToken}" },
                { "Accept", "application/json" }
            },
            Content = content
        };

        var response = await _httpClient.SendAsync(request);
        var responseContent = await response.Content.ReadAsStringAsync();

        if (response.IsSuccessStatusCode){
            var responseData = JsonConvert.DeserializeObject<dynamic>(responseContent);
            var messageId = responseData.message_id;
            return new EmailStatus("Sent", messageId, null);
        }

        return new EmailStatus("Failed", null, responseContent);
    }

    
    private async Task UpdateEmailStatusAsync(Email email, string deliveryStatus, string messageId, string error){
        email.Status.DeliveryStatus = deliveryStatus;
        email.Status.MessageId = messageId;
        email.Status.Error = error;

        await _kubernetes.Get<Email>(email.Name(), email.Namespace());
    }

    private async Task UpdateEmailStatusAsync(Email email, EmailStatus emailStatus){
        UpdateEmailStatusAsync(email, emailStatus.DeliveryStatus, emailStatus.MessageId, emailStatus.Error);
    }
}
