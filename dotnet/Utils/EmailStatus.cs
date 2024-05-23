using Newtonsoft.Json;

namespace EmailOperator.Utils{
    public class EmailStatus {
        public string DeliveryStatus { get; set; }
        public string MessageId { get; set; }
        public string Error { get; set; }
    }
}