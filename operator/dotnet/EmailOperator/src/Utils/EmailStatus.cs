using Newtonsoft.Json;

namespace EmailOperator.Utils{
    public class EmailStatus {
        public string DeliveryStatus { get; set; } = String.Empty;
        public string MessageId { get; set; } = String.Empty;
        public string Error { get; set; } = String.Empty;
    
        public EmailStatus(string deliveryStatus, string messageId, string error) {
            DeliveryStatus = deliveryStatus;
            MessageId = messageId;
            Error = error;
        }
    }
}