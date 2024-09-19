using System;
using NUnit.Framework;
using CurrencyCloud.Tests.Mock.Data;
using CurrencyCloud.Tests.Mock.Http;
using CurrencyCloud.Environment;
using CurrencyCloud.Entity;
using CurrencyCloud.Exception;
using System.Threading.Tasks;
using CurrencyCloud.Authorization;

namespace CurrencyCloud.Tests
{
    [TestFixture]
    class ClientTest
    {
        private Client client = TestHelper.GetClient(Authentication.AuthorizationOptions);
        Player player = new Player("/Mock/Http/Recordings/Client.json");

        Credentials credentials = Authentication.Credentials;

        [OneTimeSetUpAttribute]
        public void SetUp()
        {
            player.Start(ApiServer.Mock.Url);
        }

        [OneTimeTearDownAttribute]
        public void TearDown()
        {
            player.Close();
        }

        /// <summary>
        /// Fails to make an API call before logging in.
        /// </summary>
        [Test]
        [Ignore(
            "Does not fail anymore according to our logic, it should authorize instead, it is tested in other tests")]
        public void FailBeforeInitialize()
        {
            Assert.ThrowsAsync<InvalidOperationException>(async () => await client.GetCurrentAccountAsync());
        }

        /// <summary>
        /// Successfully initializes the client and logs in.
        /// </summary>
        [Test]
        public async Task Initialize()
        {
            player.Play("Initialize");
            var initializeClient = TestHelper.GetClient(Authentication.AuthorizationOptions);
            var token = await initializeClient.InitializeAsync(Authentication.ApiServer);

            Assert.IsNotEmpty(token);

            await initializeClient.CloseAsync();
        }

        /// <summary>
        /// Persists authentication token and so can make a subsequent API call.
        /// </summary>
        [Test]
        public async Task PersistToken()
        {
            player.Play("PersistToken");
            var persistTokenClient = TestHelper.GetClient(Authentication.AuthorizationOptions);
            await persistTokenClient.InitializeAsync(Authentication.ApiServer);
            await persistTokenClient.GetCurrentAccountAsync();
            await persistTokenClient.CloseAsync();
        }

        /// <summary>
        /// Silently re-authenticates if token has expired.
        /// </summary>
        [Test]
        public async Task Reauthenticate()
        {
            player.Play("Reauthenticate");

            var reauthenticateClient = TestHelper.GetClient(Authentication.AuthorizationOptions);

            await reauthenticateClient.InitializeAsync(Authentication.ApiServer);

            var expired = "3907f05da86533710efc589d58f51f45";
            reauthenticateClient.Token = expired;

            await reauthenticateClient.GetCurrentAccountAsync();

            Assert.AreNotEqual(expired, reauthenticateClient.Token);
        }

        /// <summary>
        /// Successfully logs out.
        /// </summary>
        [Test]
        [Ignore("Method is deprecated")]
        public async Task Close()
        {
            player.Play("Close");

            await client.InitializeAsync(Authentication.ApiServer);
            await client.CloseAsync();

            Assert.IsFalse(client.IsInitialized);
        }

        /// <summary>
        /// Fails to make an API call once logged out.
        /// </summary>
        [Test]
        public void FailAfterClose()
        {
            Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                player.Play("FailAfterClose");
                var failAfterCloseClient = TestHelper.GetClient(Authentication.AuthorizationOptions);
                await failAfterCloseClient.InitializeAsync(Authentication.ApiServer);
                await failAfterCloseClient.CloseAsync();
                await failAfterCloseClient.GetCurrentAccountAsync();
            });
        }

        /// <summary>
        /// Returns full error information.
        /// </summary>
        [Test]
        public async Task FailWithError()
        {
            player.Play("FailWithError");
            var failWithErrorClient = TestHelper.GetClient(Authentication.AuthorizationOptions);
            try
            {
                await failWithErrorClient.GetBalanceAsync("wrong");

                Assert.Fail();
            }
            catch (ApiException ex)
            {
                Assert.That(ex.Platform, Is.Not.Null.And.Not.Empty);

                Assert.That(ex.Request.Verb, Is.Not.Null.And.Not.Empty);
                Assert.That(ex.Platform, Is.Not.Null.And.Not.Empty);
                Assert.IsEmpty(ex.Request.Parameters);

                Assert.AreEqual(ex.Response.StatusCode, 400);
                Assert.IsFalse(DateTime.Equals(ex.Response.Date, DateTime.MinValue));
                Assert.That(ex.Response.RequestId, Is.Not.Null.And.Not.Empty);

                Assert.IsNotEmpty(ex.Errors);

                await failWithErrorClient.CloseAsync();
            }
        }
        
        /// <summary>
        /// Returns full error information.
        /// </summary>
        [Test]
        public async Task FailWithMalFormedError()
        {
            player.Play("FailWithMalformedError");
            var failWithMalFormedErrorClient = TestHelper.GetClient(Authentication.AuthorizationOptions);
            try
            {
                await failWithMalFormedErrorClient.GetBankDetailsAsync("iban", "123abc456xyz");

                Assert.Fail();
            }
            catch (ApiException ex)
            {
                Assert.That(ex.Platform, Is.Not.Null.And.Not.Empty);

                Assert.That(ex.Request.Verb, Is.Not.Null.And.Not.Empty);
                Assert.That(ex.Platform, Is.Not.Null.And.Not.Empty);
                
                Assert.AreEqual(0,ex.Request.Parameters.Count);

                Assert.AreEqual(400, ex.Response.StatusCode);
                Assert.IsFalse(DateTime.Equals(ex.Response.Date, DateTime.MinValue));
                Assert.That(ex.Response.RequestId, Is.Not.Null.And.Not.Empty);

                Assert.IsNotEmpty(ex.Errors);
                Assert.AreEqual(1,ex.Errors.Count);
                Assert.AreEqual("base",ex.Errors[0].Field);
                Assert.AreEqual(1,ex.Errors[0].ErrorMessages.Count);
                Assert.AreEqual("invalid_iban",ex.Errors[0].ErrorMessages[0].Code);
                Assert.AreEqual("IBAN is invalid.",ex.Errors[0].ErrorMessages[0].Message);
                Assert.IsEmpty(ex.Errors[0].ErrorMessages[0].Params);

                await failWithMalFormedErrorClient.CloseAsync();
            }
        }

        /// <summary>
        /// Executes API calls on behalf of specified id; once completed, resets the id.
        /// </summary>
        [Test]
        public async Task RunOnbehalfof()
        {
            player.Play("RunOnbehalfof");
            var runOnbehalfof = TestHelper.GetClient(Authentication.AuthorizationOptions);

            var contactParams = Contacts.Contact1;
            var beneficiaryParams = Beneficiaries.Beneficiary1;

            Beneficiary beneficiary;

            Account account = await runOnbehalfof.GetCurrentAccountAsync();
            contactParams.AccountId = account.Id;
            if (!Authentication.ApiServer.Url.Contains("localhost"))
                contactParams.LoginId = ContactsTest.RandomString(10);
            Contact contact = await runOnbehalfof.CreateContactAsync(contactParams);
            await runOnbehalfof.OnBehalfOf(contact.Id, async () =>
            {
                beneficiary = await runOnbehalfof.CreateBeneficiaryAsync(beneficiaryParams);

                Assert.AreEqual(contact.Id, beneficiary.CreatorContactId);
            });

            contact = await runOnbehalfof.GetCurrentContactAsync();
            beneficiary = await runOnbehalfof.CreateBeneficiaryAsync(beneficiaryParams);

            Assert.AreEqual(contact.Id, beneficiary.CreatorContactId);

            await runOnbehalfof.CloseAsync();
        }

        /// <summary>
        /// Fails if id parameter of OnBehalfOf is invalid.
        /// </summary>
        [Test]
        public void FailOnbehalfof() {
            Assert.ThrowsAsync<ArgumentException>(async () => {
                await client.OnBehalfOf("wrong", null);
            });
        }
    }
}
