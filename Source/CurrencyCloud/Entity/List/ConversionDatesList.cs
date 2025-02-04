using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace CurrencyCloud.Entity.List
{
    public class ConversionDatesList
    {
        internal ConversionDatesList() { }

        public Dictionary<DateTimeOffset, string> InvalidConversionDates { get; set; }

        public DateTimeOffset FirstConversionDate { get; set; }

        public DateTimeOffset NextDayConversionDate { get; set; }

        public DateTimeOffset DefaultConversionDate { get; set; }

        public DateTimeOffset OptimizeLiquidityConversionDate { get; set; }

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
