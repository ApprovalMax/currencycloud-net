using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using CurrencyCloud.Exception;
using NUnit.Framework;

namespace CurrencyCloud.Tests
{
    /// <summary>
    /// Locks the design invariant of AP-2804: FromHttpResponse always returns an ApiException
    /// carrying the real HTTP status, whatever the provider put in the body.
    /// </summary>
    [TestFixture]
    class ApiExceptionFactoryTests
    {
        /// <summary>
        /// CreateRequest reads RequestMessage, which is null on a hand-built response, so every
        /// case must set it.
        /// </summary>
        private static HttpResponseMessage Build(HttpStatusCode statusCode, string body)
        {
            return new HttpResponseMessage(statusCode)
            {
                RequestMessage = new HttpRequestMessage(HttpMethod.Post,
                    "https://devapi.currencycloud.com/v1/forms/1/documents/2/document_images"),
                Content = new StringContent(body ?? string.Empty)
            };
        }

        private const string WellFormedBody = @"{
            ""error_messages"": {
                ""base"": [
                    {
                        ""code"": ""payload_too_large"",
                        ""message"": ""The request body is too large"",
                        ""params"": { ""limit"": ""10485760"" }
                    }
                ]
            }
        }";

        [Test]
        public async Task PayloadTooLarge_WithJsonBodyWithoutErrorMessages_KeepsStatusAndReturnsNoErrors()
        {
            // The incident body: valid JSON, no error_messages. Used to throw ArgumentNullException.
            var res = Build(HttpStatusCode.RequestEntityTooLarge, @"{""message"":""Request Entity Too Large""}");

            var exception = await ApiExceptionFactory.FromHttpResponse(res);

            Assert.IsInstanceOf<PayloadTooLargeException>(exception);
            Assert.AreEqual(413, exception.Response.StatusCode);
            Assert.IsEmpty(exception.Errors);
            Assert.AreEqual(@"{""message"":""Request Entity Too Large""}", exception.Response.RawBody);
        }

        [Test]
        public async Task PayloadTooLarge_WithErrorMessagesAsArray_KeepsStatus()
        {
            var res = Build(HttpStatusCode.RequestEntityTooLarge, @"{""error_messages"":[""too big""]}");

            var exception = await ApiExceptionFactory.FromHttpResponse(res);

            Assert.IsInstanceOf<PayloadTooLargeException>(exception);
            Assert.AreEqual(413, exception.Response.StatusCode);
            Assert.IsEmpty(exception.Errors);
        }

        [Test]
        public async Task PayloadTooLarge_WithScalarLeaf_KeepsStatus()
        {
            var res = Build(HttpStatusCode.RequestEntityTooLarge, @"{""error_messages"":{""base"":""too big""}}");

            var exception = await ApiExceptionFactory.FromHttpResponse(res);

            Assert.IsInstanceOf<PayloadTooLargeException>(exception);
            Assert.AreEqual(413, exception.Response.StatusCode);
            Assert.IsEmpty(exception.Errors);
        }

        [Test]
        public async Task PayloadTooLarge_WithWellFormedBody_ParsesErrors()
        {
            var res = Build(HttpStatusCode.RequestEntityTooLarge, WellFormedBody);

            var exception = await ApiExceptionFactory.FromHttpResponse(res);

            Assert.IsInstanceOf<PayloadTooLargeException>(exception);
            Assert.AreEqual(413, exception.Response.StatusCode);
            Assert.AreEqual(1, exception.Errors.Count);
            Assert.AreEqual("base", exception.Errors[0].Field);
            Assert.AreEqual("payload_too_large", exception.Errors[0].ErrorMessages[0].Code);
            Assert.AreEqual("The request body is too large", exception.Errors[0].ErrorMessages[0].Message);
            Assert.AreEqual("10485760", exception.Errors[0].ErrorMessages[0].Params["limit"]);
        }

        [Test]
        public async Task BadGateway_WithHtmlBody_KeepsStatusAndPreservesRawBody()
        {
            // The incident body: an HTML gateway page. Used to throw JsonReaderException.
            const string html = "<html><head><title>502 Bad Gateway</title></head><body>502</body></html>";
            var res = Build(HttpStatusCode.BadGateway, html);

            var exception = await ApiExceptionFactory.FromHttpResponse(res);

            Assert.IsInstanceOf<UndefinedException>(exception);
            Assert.AreEqual(502, exception.Response.StatusCode);
            Assert.IsEmpty(exception.Errors);
            Assert.AreEqual(html, exception.Response.RawBody);
        }

        [Test]
        public async Task BareObjectErrorMessages_ParsesErrors()
        {
            // Provider variant recorded in the SDK's own fixtures (invalid_iban).
            var res = Build(HttpStatusCode.BadRequest, @"{
                ""error_messages"": {
                    ""base"": {
                        ""code"": ""invalid_iban"",
                        ""message"": ""IBAN is invalid"",
                        ""params"": {}
                    }
                }
            }");

            var exception = await ApiExceptionFactory.FromHttpResponse(res);

            Assert.IsInstanceOf<BadRequestException>(exception);
            Assert.AreEqual(1, exception.Errors.Count);
            Assert.AreEqual("base", exception.Errors[0].Field);
            Assert.AreEqual(1, exception.Errors[0].ErrorMessages.Count);
            Assert.AreEqual("invalid_iban", exception.Errors[0].ErrorMessages[0].Code);
            Assert.AreEqual("IBAN is invalid", exception.Errors[0].ErrorMessages[0].Message);
        }

        [Test]
        public async Task ErrorMessagesWithFallbackKeys_ParsesErrors()
        {
            var res = Build(HttpStatusCode.BadRequest, @"{
                ""error_messages"": {
                    ""account_id"": [ { ""error_code"": ""not_found"", ""reason"": ""No such account"" } ]
                }
            }");

            var exception = await ApiExceptionFactory.FromHttpResponse(res);

            Assert.AreEqual("not_found", exception.Errors[0].ErrorMessages[0].Code);
            Assert.AreEqual("No such account", exception.Errors[0].ErrorMessages[0].Message);
            Assert.IsEmpty(exception.Errors[0].ErrorMessages[0].Params);
        }

        [Test]
        public async Task MalformedField_DoesNotDiscardTheWellFormedFields()
        {
            var res = Build(HttpStatusCode.BadRequest, @"{
                ""error_messages"": {
                    ""account_id"": [ { ""code"": ""not_found"", ""message"": ""No such account"" } ],
                    ""base"": ""a bare scalar the API is not documented to send""
                }
            }");

            var exception = await ApiExceptionFactory.FromHttpResponse(res);

            Assert.AreEqual(1, exception.Errors.Count);
            Assert.AreEqual("account_id", exception.Errors[0].Field);
            Assert.AreEqual("not_found", exception.Errors[0].ErrorMessages[0].Code);
        }

        [Test]
        public async Task ScalarItemInsideAnErrorMessageArray_IsSkippedButKeepsTheRest()
        {
            var res = Build(HttpStatusCode.BadRequest, @"{
                ""error_messages"": {
                    ""base"": [ ""bare string"", { ""code"": ""not_found"", ""message"": ""No such account"" } ]
                }
            }");

            var exception = await ApiExceptionFactory.FromHttpResponse(res);

            Assert.AreEqual(1, exception.Errors.Count);
            Assert.AreEqual(1, exception.Errors[0].ErrorMessages.Count);
            Assert.AreEqual("not_found", exception.Errors[0].ErrorMessages[0].Code);
        }

        [Test]
        public async Task EmptyBody_DoesNotThrowAndReturnsNoErrors()
        {
            var res = Build(HttpStatusCode.RequestEntityTooLarge, string.Empty);

            var exception = await ApiExceptionFactory.FromHttpResponse(res);

            Assert.IsInstanceOf<PayloadTooLargeException>(exception);
            Assert.AreEqual(413, exception.Response.StatusCode);
            Assert.IsEmpty(exception.Errors);
            Assert.AreEqual(string.Empty, exception.Response.RawBody);
        }

        [Test]
        public async Task NullContent_DoesNotThrow()
        {
            var res = Build(HttpStatusCode.RequestEntityTooLarge, string.Empty);
            res.Content = null;

            var exception = await ApiExceptionFactory.FromHttpResponse(res);

            Assert.IsInstanceOf<PayloadTooLargeException>(exception);
            Assert.AreEqual(string.Empty, exception.Response.RawBody);
        }

        [Test]
        public async Task NullRequestMessage_DoesNotThrow()
        {
            var res = new HttpResponseMessage(HttpStatusCode.RequestEntityTooLarge)
            {
                Content = new StringContent("{}")
            };

            var exception = await ApiExceptionFactory.FromHttpResponse(res);

            Assert.IsInstanceOf<PayloadTooLargeException>(exception);
            Assert.AreEqual(413, exception.Response.StatusCode);
            Assert.AreEqual(string.Empty, exception.Request.Url);
        }

        [Test]
        public async Task Validation_WithWellFormedBody_StaysValidationException()
        {
            var res = Build((HttpStatusCode)422, WellFormedBody);

            var exception = await ApiExceptionFactory.FromHttpResponse(res);

            Assert.IsInstanceOf<ValidationException>(exception);
            Assert.AreEqual(422, exception.Response.StatusCode);
            Assert.AreEqual(1, exception.Errors.Count);
        }

        [Test]
        public async Task RequestTimeout_IsTyped()
        {
            var res = Build(HttpStatusCode.RequestTimeout, string.Empty);

            var exception = await ApiExceptionFactory.FromHttpResponse(res);

            Assert.IsInstanceOf<RequestTimeoutException>(exception);
            Assert.AreEqual(408, exception.Response.StatusCode);
        }

        [Test]
        public async Task RawBody_IsTruncatedToTheMaxLength()
        {
            var body = "<html>" + new string('x', Response.RawBodyMaxLength) + "more</html>";
            var res = Build(HttpStatusCode.BadGateway, body);

            var exception = await ApiExceptionFactory.FromHttpResponse(res);

            Assert.AreEqual(Response.RawBodyMaxLength, exception.Response.RawBody.Length);
            Assert.AreEqual(body.Substring(0, Response.RawBodyMaxLength), exception.Response.RawBody);
        }

        [Test]
        public async Task RawBody_IsReportedOnASingleLineWhenThereAreNoErrors()
        {
            // Short enough to survive truncation, so that the flattening is what is under test.
            var res = Build(HttpStatusCode.BadGateway, "<html>\r\n<title>502</title>\n502\r</html>");

            var exception = await ApiExceptionFactory.FromHttpResponse(res);

            StringAssert.Contains("raw_body: <html> <title>502</title> 502 </html>", exception.Message);
            Assert.AreEqual(1, exception.Message.Split("raw_body:").Length - 1);
            Assert.IsFalse(exception.Response.RawBody.Length == 0);
        }

        [Test]
        public async Task RawBody_IsNotReportedWhenErrorsWereParsed()
        {
            var res = Build((HttpStatusCode)422, WellFormedBody);

            var exception = await ApiExceptionFactory.FromHttpResponse(res);

            StringAssert.DoesNotContain("raw_body:", exception.Message);
        }
    }
}
