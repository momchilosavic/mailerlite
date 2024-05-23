using k8s.Models;
using KubeOps.Operator.Entities;

namespace EmailOperator.Entities;

[KubernetesEntity(Group = "mailerlite.com", ApiVersion = "v1", Kind = "Email")]
public class Email : CustomKubernetesEntity<Email.EmailSpec, Email.EmailStatus>
{
    public class EmailSpec
    {
        public string SenderConfigRef { get; set; }
        public string RecipientEmail { get; set; }
        public string Subject { get; set; }
        public string Body { get; set; }
    }

    public class EmailStatus
    {
        public string DeliveryStatus { get; set; }
        public string MessageId { get; set; }
        public string Error { get; set; }
    }
}
