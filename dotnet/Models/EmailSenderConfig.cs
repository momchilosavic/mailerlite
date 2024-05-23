using k8s;
using k8s.Models;
using Newtonsfot.Json;

namespace EmailOperator.Models{
    public class EmailSenderConfigSpec{
        public string ApiTokenSecretRef { get; set; }
        public string SenderEmail { get; set; }
    }

    public class EmailSenderConfig : CustomResource<EmailSenderConfigSpec> {}
}