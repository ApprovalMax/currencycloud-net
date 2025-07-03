using System;
using Newtonsoft.Json;

namespace CurrencyCloud.Entity
{
    public class PaymentDeliveryDates : Entity
    {
        [JsonConstructor]
        public PaymentDeliveryDates() { }

        public PaymentDeliveryDates(DateOnly paymentDate, string paymentType, string currency, string bankCountry)
        {
            this.PaymentDate = paymentDate;
            this.PaymentType = paymentType;
            this.Currency = currency;
            this.BankCountry = bankCountry;
        }

        [Param]
        public DateOnly? PaymentDate { get; set; }

        [Param]
        public string PaymentType { get; set; }

        [Param]
        public string Currency { get; set; }

        [Param]
        public string BankCountry { get; set; }

        public DateOnly? PaymentDeliveryDate { get; set; }

        public DateTimeOffset? PaymentCutoffTime { get; set; }

        public string ToJSON()
        {
            var obj = new[]
            {
                new
                {
                    PaymentDate,
                    PaymentDeliveryDate,
                    PaymentCutoffTime,
                    PaymentType,
                    Currency,
                    BankCountry
                }
            };
            return JsonConvert.SerializeObject(obj);
        }

        public override bool Equals(object obj)
        {
            if (!(obj is PaymentDeliveryDates))
            {
                return false;
            }

            var paymentDeliveryDates = obj as PaymentDeliveryDates;

            return PaymentDate == paymentDeliveryDates.PaymentDate &&
                   PaymentDeliveryDate == paymentDeliveryDates.PaymentDate &&
                   PaymentCutoffTime == paymentDeliveryDates.PaymentCutoffTime &&
                   PaymentType == paymentDeliveryDates.PaymentType &&
                   Currency == paymentDeliveryDates.Currency &&
                   BankCountry == paymentDeliveryDates.BankCountry;
        }

        public override int GetHashCode()
        {
            return PaymentDate.GetHashCode() ^
                   PaymentDeliveryDate.GetHashCode() ^
                   PaymentType.GetHashCode() ^
                   Currency.GetHashCode() ^
                   BankCountry.GetHashCode();
        }
    }
}