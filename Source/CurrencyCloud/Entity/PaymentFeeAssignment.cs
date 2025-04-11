using System;
using Newtonsoft.Json;

namespace CurrencyCloud.Entity
{
    public class PaymentFeeAssignment : Entity
    {
        [JsonConstructor]
        public PaymentFeeAssignment() { }
        
        public PaymentFeeAssignment(string paymentFeeId, string accountId)
        {
            this.Id = paymentFeeId;
            this.AccountId = accountId;
        }
            
        /// <summary>
        /// ID of the payment fee
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// ID of the sub-account the rule is assigned to
        /// </summary>
        [Param]
        public string AccountId { get; set; }

        public string ToJSON()
        {
            var obj = new[]
            {
                new
                {
                    Id,
                    AccountId
                }
            };
            return JsonConvert.SerializeObject(obj);
        }

        public override bool Equals(object obj)
        {
            if (!(obj is PaymentFeeAssignment))
            {
                return false;
            }

            var paymentFeeAssignment = obj as PaymentFeeAssignment;

            return Id == paymentFeeAssignment.Id &&
                   AccountId == paymentFeeAssignment.AccountId;
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }
    }
} 
