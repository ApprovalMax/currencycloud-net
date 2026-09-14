using System;
using System.Collections.Generic;
using System.Text;

namespace CurrencyCloud.Exception
{
    /// <summary>
    /// Represents errors that occur when making API calls.
    /// </summary>
    public class ApiException : System.Exception
    {
        private readonly string yamlString;

        private string CreateYamlString()
        {
            StringBuilder yamlBuilder = new StringBuilder();

            yamlBuilder.AppendLine(GetType().Name);
            yamlBuilder.AppendLine("---");

            yamlBuilder.AppendFormat("platform: {0}", Platform);
            yamlBuilder.AppendLine();

            yamlBuilder.AppendLine("request:");
            yamlBuilder.Append("  parameters:");
            if (Request.Parameters.Count == 0)
            {
                yamlBuilder.Append(" {}");
            }
            else
            {
                foreach (var parameter in Request.Parameters)
                {
                    yamlBuilder.AppendLine();
                    yamlBuilder.AppendFormat("    {0}: {1}", parameter.Key, parameter.Value);
                }
            }
            yamlBuilder.AppendLine();
            yamlBuilder.AppendFormat("  verb: {0}", Request.Verb);
            yamlBuilder.AppendLine();
            yamlBuilder.AppendFormat("  url: {0}", Request.Url);
            yamlBuilder.AppendLine();

            yamlBuilder.AppendLine("response:");
            yamlBuilder.AppendFormat("  status_code: {0}", Response.StatusCode);
            yamlBuilder.AppendLine();
            yamlBuilder.AppendFormat("  date: {0}", Response.Date.ToUniversalTime().ToString("r"));
            yamlBuilder.AppendLine();
            yamlBuilder.AppendFormat("  request_id: {0}", Response.RequestId);
            yamlBuilder.AppendLine();

            if (Errors.Count == 0 && !string.IsNullOrEmpty(Response.RawBody))
            {
                // The only diagnostic that exists when the body carried no error_messages
                // (e.g. a 413 refused by an edge proxy). Bounded by Response.RawBodyMaxLength.
                yamlBuilder.AppendFormat("  raw_body: {0}", FlattenToSingleLine(Response.RawBody));
                yamlBuilder.AppendLine();
            }

            yamlBuilder.Append("errors:");
            foreach (var error in Errors)
            {
                foreach (var errorMessage in error.ErrorMessages)
                {
                    yamlBuilder.AppendLine();
                    yamlBuilder.AppendFormat("- field: {0}", error.Field);
                    yamlBuilder.AppendLine();
                    yamlBuilder.AppendFormat("  code: {0}", errorMessage.Code);
                    yamlBuilder.AppendLine();
                    yamlBuilder.AppendFormat("  message: {0}", errorMessage.Message);
                    yamlBuilder.AppendLine();
                    yamlBuilder.Append("  params:");
                    if (errorMessage.Params.Count == 0)
                    {
                        yamlBuilder.Append(" {}");
                    }
                    else
                    {
                        foreach (var param in errorMessage.Params)
                        {
                            yamlBuilder.AppendLine();
                            yamlBuilder.AppendFormat("    {0}: {1}", param.Key, param.Value);
                        }
                    }
                }
            }

            return yamlBuilder.ToString();
        }

        private static string FlattenToSingleLine(string value)
        {
            return value.Replace("\r\n", " ").Replace("\n", " ").Replace("\r", " ");
        }

        protected ApiException(Request request, Response response, List<Error> errors)
        {
            Platform = Environment.Platform.Version;
            Request = request;
            Response = response;
            Errors = errors;

            yamlString = CreateYamlString();
        }

        public readonly string Platform;
        public readonly Request Request;
        public readonly Response Response;
        public readonly List<Error> Errors;

        public string ToYamlString()
        {
            return yamlString;
        }

        public override string Message
        {
            get
            {
                return yamlString;
            }
        }
    }

    public class BadRequestException : ApiException
    {
        public BadRequestException(Request request, Response response, List<Error> errors): base(request, response, errors) { }
    }

    public class AuthenticationException : ApiException
    {
        public AuthenticationException(Request request, Response response, List<Error> errors): base(request, response, errors) { }
    }

    public class ForbiddenException : ApiException
    {
        public ForbiddenException(Request request, Response response, List<Error> errors): base(request, response, errors) { }
    }

    public class NotFoundException : ApiException
    {
        public NotFoundException(Request request, Response response, List<Error> errors): base(request, response, errors) { }
    }

    public class TooManyRequestsException : ApiException
    {
        public TooManyRequestsException(Request request, Response response, List<Error> errors, DateTimeOffset? retryAfter)
            : base(request, response, errors)
        {
            RetryAfter = retryAfter;
        }
        
        public DateTimeOffset? RetryAfter { get; }
    }

    public class InternalApplicationException : ApiException
    {
        public InternalApplicationException(Request request, Response response, List<Error> errors): base(request, response, errors) { }
    }
    
    public class ValidationException : ApiException
    {
        public ValidationException(Request request, Response response, List<Error> errors): base(request, response, errors) { }
    }

    /// <summary>
    /// Thrown when the provider answers 413. Terminal: retrying sends the same oversized body again.
    /// </summary>
    public class PayloadTooLargeException : ApiException
    {
        public PayloadTooLargeException(Request request, Response response, List<Error> errors): base(request, response, errors) { }
    }

    public class RequestTimeoutException : ApiException
    {
        public RequestTimeoutException(Request request, Response response, List<Error> errors): base(request, response, errors) { }
    }

    public class UndefinedException : ApiException
    {
        public UndefinedException(Request request, Response response, List<Error> errors) : base(request, response, errors) { }
    }

    /// <summary>
    /// Represents request parameters of the HTTP call that caused API exception.
    /// </summary>
    public class Request
    {
        public readonly Dictionary<string, string> Parameters;
        public readonly string Verb;
        public readonly string Url;

        public Request(Dictionary<string, string> parameters, string verb, string url)
        {
            Parameters = parameters;
            Verb = verb;
            Url = url;
        }
    }

    /// <summary>
    /// Represents response parameters of the HTTP call that caused API exception.
    /// </summary>
    public class Response
    {
        /// <summary>
        /// Upper bound on the retained response body, so that <see cref="ApiException.Message"/> stays bounded.
        /// </summary>
        public const int RawBodyMaxLength = 2048;

        public readonly int StatusCode;
        public readonly DateTime Date;
        public readonly string RequestId;

        /// <summary>
        /// The response body as received, truncated to <see cref="RawBodyMaxLength"/>. May be empty.
        /// It is the only diagnostic available when the body could not be parsed into errors.
        /// </summary>
        public readonly string RawBody;

        public Response(int statusCode, DateTime date, string requestId)
            : this(statusCode, date, requestId, string.Empty)
        {
        }

        public Response(int statusCode, DateTime date, string requestId, string rawBody)
        {
            StatusCode = statusCode;
            Date = date;
            RequestId = requestId;
            RawBody = Truncate(rawBody);
        }

        private static string Truncate(string rawBody)
        {
            if (string.IsNullOrEmpty(rawBody))
            {
                return string.Empty;
            }

            return rawBody.Length <= RawBodyMaxLength
                ? rawBody
                : rawBody.Substring(0, RawBodyMaxLength);
        }
    }

    /// <summary>
    /// Represents API error.
    /// </summary>
    public class Error
    {
        public readonly string Field;
        public readonly List<ErrorMessage> ErrorMessages;

        public Error(string field, List<ErrorMessage> errorMessages)
        {
            Field = field;
            ErrorMessages = errorMessages;
        }

        /// <summary>
        /// Represents API error message.
        /// </summary>
        public class ErrorMessage
        {
            public readonly string Code;
            public readonly string Message;
            public readonly Dictionary<string, string> Params;

            public ErrorMessage(string code, string message, Dictionary<string, string> @params)
            {
                Code = code;
                Message = message;
                Params = @params;
            }
        }
    }
}
