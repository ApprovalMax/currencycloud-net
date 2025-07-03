using System;
using System.Globalization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace CurrencyCloud.Converters;

public class DateOnlyConverter : IsoDateTimeConverter
{
    public DateOnlyConverter()
    {
        DateTimeStyles = DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal;
    }

    public override bool CanConvert(Type objectType)
    {
        return objectType == typeof(DateOnly) || objectType == typeof(DateOnly?);
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
                return DateOnly.FromDateTime(dateTimeValue.ToUniversalTime());
            }
            
            var stringValue = (string)reader.Value;
            if (string.IsNullOrEmpty(stringValue))
            {
                return null;
            }
            
            if (DateTime.TryParse(stringValue, provider: null, DateTimeStyles.AdjustToUniversal, out var dateTime))
            {
                return DateOnly.FromDateTime(dateTime.ToUniversalTime());
            }

            throw new JsonSerializationException($"Unexpected token parsing date. String value '{stringValue}' is not a valid DateTime.");
        }
        
        if (reader.TokenType == JsonToken.Date)
        {
            return reader.Value switch
            {
                DateTimeOffset dateTimeOffsetValue => DateOnly.FromDateTime(dateTimeOffsetValue.ToUniversalTime().UtcDateTime),
                DateTime dateTimeValue => DateOnly.FromDateTime(dateTimeValue.ToUniversalTime()),
                DateOnly dateOnlyValue => dateOnlyValue,
                _ => throw new JsonSerializationException($"Unexpected date type. Expected DateTimeOffset, DateTime or DateOnly, got {reader.Value?.GetType().Name}.")
            };
        }

        throw new JsonSerializationException($"Unexpected token parsing date. Expected String or Date, got {reader.TokenType}.");
    }
}