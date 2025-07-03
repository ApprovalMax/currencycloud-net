using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace CurrencyCloud.Entity.List
{
    public class PaymentDatesList
    {
        internal PaymentDatesList() { }

        public Dictionary<DateOnly, string> InvalidPaymentDates { get; set; }

        public DateOnly FirstPaymentDate { get; set; }

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
