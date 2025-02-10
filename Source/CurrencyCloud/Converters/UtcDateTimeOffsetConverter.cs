using System;
using System.Globalization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace CurrencyCloud.Converters;

public class UtcDateTimeOffsetConverter : IsoDateTimeConverter
{
    public UtcDateTimeOffsetConverter()
    {
        DateTimeStyles = DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal;
    }

    public override bool CanConvert(Type objectType)
    {
        return objectType == typeof(DateTimeOffset) || objectType == typeof(DateTimeOffset?);
    }

    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    {
        if (reader.TokenType == JsonToken.Null)
        {
            return null;
        }

        if (reader.TokenType == JsonToken.String)
        {
            if (reader.Value is DateTime dateTimeValue)
            {
                return new DateTimeOffset(dateTimeValue.ToUniversalTime());
            }
            
            var stringValue = (string)reader.Value;
            if (string.IsNullOrEmpty(stringValue))
            {
                return null;
            }
            
            if (DateTime.TryParse(stringValue, null, DateTimeStyles.RoundtripKind, out var dateTime))
            {
                return dateTime.Kind == DateTimeKind.Unspecified
                    ? new DateTimeOffset(dateTime, TimeSpan.Zero)
                    : new DateTimeOffset(dateTime.ToUniversalTime());
            }

            throw new JsonSerializationException($"Unexpected token parsing date. String value '{stringValue}' is not a valid DateTime.");
        }
        
        if (reader.TokenType == JsonToken.Date)
        {
            return reader.Value switch
            {
                DateTimeOffset offset => offset.ToUniversalTime(),
                DateTime dateTimeValue => new DateTimeOffset(dateTimeValue.ToUniversalTime()),
                _ => throw new JsonSerializationException($"Unexpected date type. Expected DateTimeOffset or DateTime, got {reader.Value?.GetType().Name}.")
            };
        }

        throw new JsonSerializationException($"Unexpected token parsing date. Expected String or Date, got {reader.TokenType}.");
    }
}
