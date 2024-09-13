using Newtonsoft.Json;

namespace CurrencyCloud.Entity
{
    public class AddedFunds : Entity
    {
        [JsonConstructor]
        public AddedFunds() { }

        /// <summary>
        /// ID of the Funding Account
        /// </summary>
        public string Id { get; set; }

        ///<summary>
        /// ID of the Account this SSI is attached to
        ///</summary>
        public string AccountId { get; set; }

        /// <summary>
        /// The state of the simulated fund
        /// </summary>
        public string State { get; set; }
        
        /// <summary>
        /// The sender's name
        /// </summary>
        public string SenderName { get; set; }
        
        /// <summary>
        /// The sender's address
        /// </summary>
        public string SenderAddress { get; set; }
        
        /// <summary>
        /// Two-digit sender country
        /// </summary>
        public string SenderCountry { get; set; }
        
        /// <summary>
        /// Sender reference
        /// </summary>
        public string SenderReference { get; set; }
        
        /// <summary>
        /// Sender account number
        /// </summary>
        public string SenderAccountNumber { get; set; }
        
        /// <summary>
        /// Sender routing code
        /// </summary>
        public string SenderRoutingCode{ get; set; }
        
        /// <summary>
        /// A client or its sub-account's account number
        /// </summary>
        public string ReceiverAccountNumber { get; set; }
            
        /// <summary>
        /// Routing code for a client or its sub-account
        /// </summary>
        public string ReceiverRoutingCode { get; set; }
        
        /// <summary>
        /// Amount of the emulated transaction
        /// </summary>
        public decimal Amount{ get; set; }

        /// <summary>
        /// Three-digit currency code
        /// </summary>
        public string Currency { get; set; }
        
        /// <summary>
        /// Allows you to trigger approval or rejection behaviour
        /// </summary>
        public string Action { get; set; }
        
        /// <summary>
        /// The date-time the SSI was added to the account
        /// </summary>
        public DateTimeOffset? CreatedAt { get; set; }

        /// <summary>
        /// The date-time the SSI was last updated on the account
        /// </summary>
        public DateTimeOffset? UpdatedAt { get; set; }


        public string ToJSON()
        {
            var obj = new[]
            {
                new
                {
                    Id,
                    AccountId,
                    State,
                    SenderName,
                    SenderAddress,
                    SenderCountry,
                    SenderReference,
                    SenderAccountNumber,
                    SenderRoutingCode,
                    ReceiverAccountNumber,
                    ReceiverRoutingCode,
                    Amount,
                    Currency,
                    Action,
                    CreatedAt,
                    UpdatedAt
                }
            };
            return JsonConvert.SerializeObject(obj);
        }

        public override bool Equals(object obj)
        {
            if (!(obj is AddedFunds))
            {
                return false;
            }

            var account = obj as AddedFunds;

            return Id == account.Id &&
                   AccountId == account.AccountId &&
                   State == account.State &&
                   SenderName == account.SenderName &&
                   SenderAddress == account.SenderAddress &&
                   SenderCountry == account.SenderCountry &&
                   SenderReference == account.SenderReference &&
                   SenderAccountNumber == account.SenderAccountNumber &&
                   SenderRoutingCode == account.SenderRoutingCode &&
                   ReceiverAccountNumber == account.ReceiverAccountNumber &&
                   ReceiverRoutingCode == account.ReceiverRoutingCode &&
                   Amount == account.Amount &&
                   Currency == account.Currency &&
                   Action == account.Action &&
                   CreatedAt == account.CreatedAt &&
                   UpdatedAt == account.UpdatedAt;
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }
    }
}
