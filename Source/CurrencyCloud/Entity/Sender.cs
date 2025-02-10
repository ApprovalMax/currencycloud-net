using System;
using Newtonsoft.Json;

namespace CurrencyCloud.Entity
{
    public class Sender : Entity
    {
        [JsonConstructor]
        public Sender() { }
        
        public Guid? SenderId { get; set; }
        public string SenderAddress { get; set; }
        public string SenderCountry { get; set; }
        public string SenderName { get; set; }
        public string SenderBic { get; set; }
        public string SenderIban { get; set; }
        public string SenderAccountNumber { get; set; }
        public string SenderRoutingCode { get; set; }
    }
}
