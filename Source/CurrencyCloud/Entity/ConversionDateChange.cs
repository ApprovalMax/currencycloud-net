using System;
using Newtonsoft.Json;

namespace CurrencyCloud.Entity
{
    public class ConversionDateChange : Entity
    {
        [JsonConstructor]
        public ConversionDateChange() { }

        public ConversionDateChange(string id, DateTimeOffset newSettlementDate)
        {
            this.ConversionId = id;
            this.NewSettlementDate = newSettlementDate;
        }

        public string ConversionId { get; set; }

        public string Currency { get; set; }

        public decimal? Amount { get; set; }

        public DateTimeOffset? NewConversionDate { get; set; }

        /// <summary>
        /// New conversion settlement date
        /// </summary>
        [Param]
        public DateTimeOffset? NewSettlementDate { get; set; }

        public DateTimeOffset? OldConversionDate { get; set; }

        public DateTimeOffset? OldSettlementDate { get; set; }

        public DateTimeOffset? EventDateTime { get; set; }

        public string ToJSON()
        {
            var obj = new[]
            {
                new
                {
                    ConversionId,
                    Amount,
                    Currency,
                    NewConversionDate,
                    NewSettlementDate,
                    OldConversionDate,
                    OldSettlementDate,
                    EventDateTime
                }
            };
            return JsonConvert.SerializeObject(obj);
        }

        public override bool Equals(object obj)
        {
            if (!(obj is ConversionDateChange))
            {
                return false;
            }

            var conversionDateChange = obj as ConversionDateChange;

            return ConversionId == conversionDateChange.ConversionId &&
                   Amount == conversionDateChange.Amount &&
                   Currency == conversionDateChange.Currency &&
                   NewConversionDate == conversionDateChange.NewConversionDate &&
                   NewSettlementDate == conversionDateChange.NewSettlementDate &&
                   OldConversionDate == conversionDateChange.OldConversionDate &&
                   OldSettlementDate == conversionDateChange.OldSettlementDate &&
                   EventDateTime == conversionDateChange.EventDateTime;
        }

        public override int GetHashCode()
        {
            return ConversionId.GetHashCode();
        }
    }
}
