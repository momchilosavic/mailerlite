using System;

namespace EmailOperator.Controllers {
    public class EmailController {
        private const string API_GROUP = "mailersend";
        private const string API_VERSION = "v1";
        private const string API_TOKEN_SECRET_KEY = "token";
        private const string TOKEN_VALUE = "eyJ0eXAiOiJKV1QiLCJhbGciOiJSUzI1NiJ9.eyJhdWQiOiI0IiwianRpIjoiNzVjMWMzNWMyZTc1MmE4MzAyYjhjM2FlOWJhOTg3OTYyYjZjYjQyZjkzNWJmZjcxOWE2ODUwYmU3YzVjYmExM2FlNTFhMDE5Y2FjZDBkMDIiLCJpYXQiOjE3MTYzOTc1MTYuNTI2ODA0LCJuYmYiOjE3MTYzOTc1MTYuNTI2ODA3LCJleHAiOjQ4NzIwNzExMTYuNTIzNDE5LCJzdWIiOiI5NjcyMjIiLCJzY29wZXMiOltdfQ.icGY_Du77P1PbN0-HpPqXwGxZWeiAhaFZHMD7MYKZNXPG4qe2CeOTMrcw2sJLwEIFw-bq826xhJwza5XDFzdbQ2GQUpy82Ilj3hk_GwCCUtKmSpq6XsUY7X9xS_WBl8D8pra9i6_lqgzWuZXme7Eqwhk79GAhdaRNiVnz4qm5QtB-_FvYOuFI8E8bbTmLzx4LHMsae64tWZ8b8QuqY9sty0cyiihGJb9EyHZuw9-vBlkTOrzyhLSLPDknPUWv9MIXfXuSV6LIKrvmZkZE1GmxBjWCi4AAI961y2uhvoTZz6RL1vBuY5kNe0gSfxk6mmaUuI5nlcLreXNFZkK8aokyv6QpqLQfduMhKCKnQkmrsPhgYyhWMo51L_tNUTfcr9qqg70sRVak5-_g82n9HbECOXjQoDi28VpbnxihvQ1QoP62r64_dounK0j5qNjSSRApSGPw7_POV-MimLFvjRARw_jtKRI8Iv7FwPa6bGAUh9EvU2hOYw70F988YelhLTsM0ivywfgxhgSDAhlSJ77OkRApdGmvsHqucxva6Km7LgebFO1bLSXMKpnKTTA4hyW03p4rx4Z2hJJGuocRD7Rma8ex_4XfKQINR0voOVosm56N4W-5d_US-uUI1za6oaUdWvNWPirXsgkAaD6KzNc-DWzKSUxgRzuPXSRkJP7whw";
        private const string MAIL_SERVICE_ENDPOINT = "https://connect.mailerlite.com";

        private readonly IKubernetes _kubernetes;
        private readonly ILogger<EmailController> _logger;
        private readonly HttpClient _httpClient;
    
        public EmailController(IKubernetes kubernetes, ILogger<EmailController> logger){
            _kubernetes = kubernetes;
            _logger = logger;
            _httpClient = new HttpClient();
        }

        private async Task UpdateEmailStatusAsync(Email email, string deliveryStatus, string messageId, string error){
            email.Status.DeliveryStatus = deliveryStatus;
            email.Status.MessageId = messageId;
            email.Status.Error = error;

            await _kubernetes.CustomObjects.ReplaceNamespacedCustomObjectStatusAsync(email, API_GROUP, API_VERSION, email.Namespace(), email.Name());
        }

        private async Task UpdateEmailStatusAsync(Email email, EmailStatus emailStatus){
            UpdateEmailStatusAsync(email, emailStatus.DeliveryStatus, emailStatus.MessageId, emailStatus.Error);
        }

        public async Task ReconcileAsync(Email email){
            try{
                var senderConfig = await _kubernetes.CustomObject.ReadNamespacedCustomObjectAsync<EmailSenderConfig>(API_GROUP, API_VERSION, email.Namespace(), email.Spec.SenderConfigRef);
                if (senderConfig == null) throw new Exception("Sender config not found");

                var apiTokenSecret = await _kuberentes.CoreV1.ReadNamespacedSecretAsync(senderConfig.Spec.ApiTokenSecretRef, email.Namespace());
                if (apiTokenSecret == null) throw new Exception("API Token secret not found");

                var apiToken = Encoding.UTF8.GetString(apiTokenSecret.Data[API_TOKEN_SECRET_KEY]);

                EmailStatus emailStatus = await SendEmailAsync(apiToken, senderConfig.Spec.SenderEmail, email.Spec.RecipientEmail, email.Spec.Subject, email.Spec.Body);

                await UpdateEmailStatusAsync(email, emailStatus);
            }
            catch (Exception exception){
                _logger.LogError(exception, "Error reconciling Email");
                await UpdateEmailStatusAsync(email, "Failed", null, exception.Message);
            }
        }

        public async Task<EmailStatus> SendEmailAsync(string apiToken, string senderEmail, string recipientEmail, string subject, string body){
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
                    { "Content-Type": "application/json" },
                    { "Accept": "application/json" }
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
    }
}