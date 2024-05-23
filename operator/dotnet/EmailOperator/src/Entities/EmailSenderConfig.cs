using k8s.Models;
using KubeOps.Operator.Entities;

namespace EmailOperator.Entities;

[KubernetesEntity(Group = "mailerlite.com", ApiVersion = "v1", Kind = "EmailSenderConfig")]
public class EmailSenderConfig : CustomKubernetesEntity<EmailSenderConfig.EmailSenderConfigSpec>
{
    public class EmailSenderConfigSpec
    {
        public string ApiTokenSecretRef { get; set; }
        public string SenderEmail { get; set; }
    }
}
