using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace CurrencyCloud.Entity.List
{
    public class ConversionDatesList
    {
        internal ConversionDatesList() { }

        public Dictionary<DateOnly, string> InvalidConversionDates { get; set; }

        public DateOnly FirstConversionDate { get; set; }

        public DateOnly NextDayConversionDate { get; set; }

        public DateOnly DefaultConversionDate { get; set; }

        public DateOnly OptimizeLiquidityConversionDate { get; set; }

        public DateTimeOffset FirstConversionCutoffDatetime { get; set; }

        public string ToJSON()
        {
            var obj = new[]
            {
                new
                {
                    InvalidConversionDates,
                    FirstConversionDate,
                    DefaultConversionDate,
                    NextDayConversionDate,
                    OptimizeLiquidityConversionDate,
                    FirstConversionCutoffDatetime
                }
            };
            return JsonConvert.SerializeObject(obj);
        }
    }
}
