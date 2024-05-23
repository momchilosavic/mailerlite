using k8s;
using k8s.Models;
using Newtonsoft.Json;

namespace EmailOperator.Models{
    public class EmailSpec {
        public string SenderConfigRef { get; set; }
        public string RecipientEmail { get; set; }
        public string Subject { get; set; }
        public string Body { get; set; }
    }

    public class EmailStatus {
        public string DeliveryStatus { get; set; }
        public string MessageId { get; set; }
        public string Error { get; set; }
    }

    public class Email : CustomResource<EmailSpec, EmailStatus> {}
}