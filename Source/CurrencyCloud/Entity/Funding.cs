using System;
using Newtonsoft.Json;

namespace CurrencyCloud.Entity
{
    public class Funding : Entity
    {
        [JsonConstructor]
        public Funding() { }
        
        public Guid Id { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; }
        public string Rail { get; set; }
        public string AdditionalInformation { get; set; }
        public string ReceivingAccountRoutingCode { get; set; }
        public DateTimeOffset ValueDate { get; set; }
        public string ReceivingAccountNumber { get; set; }
        public string ReceivingAccountIban { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }
        public DateTimeOffset? CompletedAt { get; set; }
        public Sender Sender { get; set; }
    }
}