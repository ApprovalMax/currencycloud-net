using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace CurrencyCloud.Entity.List
{
    public class PaymentDatesList
    {
        internal PaymentDatesList() { }

        public Dictionary<DateTimeOffset, string> InvalidPaymentDates { get; set; }

        public DateTimeOffset FirstPaymentDate { get; set; }

        public string ToJSON()
        {
            var obj = new[]
            {
                new
                {
                    InvalidPaymentDates,
                    FirstPaymentDate
                }
            };
            return JsonConvert.SerializeObject(obj);
        }
    }
}
