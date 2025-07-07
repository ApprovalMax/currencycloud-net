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
            var stringValue = (string)reader.Value;
            if (string.IsNullOrEmpty(stringValue))
            {
                return null;
            }

            if (DateOnly.TryParse(stringValue, out var dateOnlyValue))
            {
                return dateOnlyValue;
            }
            
            if (DateTimeOffset.TryParse(stringValue,  formatProvider: null, DateTimeStyles.AdjustToUniversal, out var dateTimeOffset))
            {
                return ToDateOnly(dateTimeOffset);
            }

            throw new JsonSerializationException($"Unexpected token parsing date. String value '{stringValue}' is not a valid DateTime.");
        }
        
        if (reader.TokenType == JsonToken.Date)
        {
            return reader.Value switch
            {
                DateTimeOffset dateTimeOffsetValue => ToDateOnly(dateTimeOffsetValue),
                DateTime dateTimeValue => ToDateOnly(dateTimeValue),
                DateOnly dateOnlyValue => dateOnlyValue,
                _ => throw new JsonSerializationException(
                    $"Unexpected date type. Expected DateTimeOffset, DateTime or DateOnly, got {reader.Value?.GetType().Name}.")
            };
        }

        throw new JsonSerializationException($"Unexpected token parsing date. Expected String or Date, got {reader.TokenType}.");
    }

    private static DateOnly ToDateOnly(DateTimeOffset dateTimeOffset)
    {
        var dateTimeUtc = dateTimeOffset.UtcDateTime;

        return UtcDateTimeToDateOnly(dateTimeUtc);
    }

    private static DateOnly ToDateOnly(DateTime dateTime)
    {
        var dateTimeUtc = dateTime.ToUniversalTime();

        return UtcDateTimeToDateOnly(dateTimeUtc);
    }
    
    private static DateOnly UtcDateTimeToDateOnly(DateTime dateTime)
    {
        if (dateTime.TimeOfDay != TimeSpan.Zero)
        {
            throw new JsonSerializationException($"Time component found in date string '{dateTime}'. DateOnly expects a date without a time component.");
        }
        
        var (dateOnly, _) = dateTime;
        
        return dateOnly;
    }
}
