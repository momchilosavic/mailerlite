using System;

namespace EmailOperator.Controllers {
    public class EmailSenderConfigController {
        private const string API_GROUP = "mailerlite";
        private const string API_VERSION = "v1";

        private readonly IKubernetes _kubernetes;
        private readonly ILogger<EmailController> _logger;
    
        public EmailSenderConfigController(IKubernetes kubernetes, ILogger<EmailController> logger){
            _kubernetes = kubernetes;
            _logger = logger;
        }

        public async Task ReconcileAsync(EmailSenderConfig emailSenderConfig){
            try{
                _logger.LogInformation($"Detected new configuration: {emailSenderConfig.Metadata.Name} in {emailSenderConfig.Metadata.Namespace} namespace");
            }
            catch (Exception exception){
                _logger.LogError(exception, "Error reconciling EmailSenderConfig");
            }
        }
    }
}