using CurrencyCloud.Entity;
using CurrencyCloud.Environment;
using CurrencyCloud.Tests.Mock.Data;
using CurrencyCloud.Tests.Mock.Http;
using NUnit.Framework;

namespace CurrencyCloud.Tests;

[TestFixture]
internal class AccountsTest
{
    [OneTimeSetUpAttribute]
    public void SetUp()
    {
        player.Start(ApiServer.Mock.Url);
        player.Play("SetUp");

        var credentials = Authentication.Credentials;

        client.InitializeAsync(Authentication.ApiServer, credentials.LoginId, credentials.ApiKey).Wait();
    }

    [OneTimeTearDownAttribute]
    public void TearDown()
    {
        player.Play("TearDown");

        client.CloseAsync().Wait();

        player.Close();
    }

    private readonly Client client = new();
    private readonly Player player = new("/Mock/Http/Recordings/Accounts.json");

    /// <summary>
    ///     Successfully creates an account.
    /// </summary>
    [Test]
    public async Task Create()
    {
        player.Play("Create");

        var account1 = Accounts.Account1;

        var created = await client.CreateAccountAsync(account1);

        Assert.That(created.Status, Is.Not.Null.And.Not.Empty);
        Assert.AreEqual(account1.AccountName, created.AccountName);
        Assert.AreEqual(account1.LegalEntityType, created.LegalEntityType);
        Assert.AreEqual(account1.YourReference, created.YourReference);
        Assert.AreEqual(account1.Street, created.Street);
        Assert.AreEqual(account1.City, created.City);
        Assert.AreEqual(account1.StateOrProvince, created.StateOrProvince);
        Assert.AreEqual(account1.PostalCode, created.PostalCode);
        Assert.AreEqual(account1.Country, created.Country);
        Assert.AreEqual(account1.SpreadTable, created.SpreadTable);
        Assert.AreEqual(account1.IdentificationType, created.IdentificationType);
        Assert.AreEqual(account1.Brand, created.Brand);
        Assert.AreEqual(account1.ApiTrading, created.ApiTrading);
        Assert.AreEqual(account1.OnlineTrading, created.OnlineTrading);
        Assert.AreEqual(account1.PhoneTrading, created.PhoneTrading);
        Assert.AreEqual(account1.TermsAndConditionsAccepted, created.TermsAndConditionsAccepted);
    }

    /// <summary>
    ///     Successfully gets an account.
    /// </summary>
    [Test]
    public async Task Get()
    {
        player.Play("Get");

        var account1 = Accounts.Account1;

        var created = await client.CreateAccountAsync(account1);
        var gotten = await client.GetAccountAsync(created.Id);

        Assert.AreEqual(gotten, created);
    }

    /// <summary>
    ///     Successfully updates an account.
    /// </summary>
    [Test]
    public async Task Update()
    {
        player.Play("Update");

        var account1 = Accounts.Account1;
        var account2 = Accounts.Account2;

        var created = await client.CreateAccountAsync(account1);
        account2.Id = created.Id;
        var updated = await client.UpdateAccountAsync(account2);
        var gotten = await client.GetAccountAsync(created.Id);

        Assert.AreEqual(gotten, updated);
    }

    /// <summary>
    ///     Successfully finds an account with search parameters.
    /// </summary>
    [Test]
    public async Task FindWithParams()
    {
        player.Play("FindWithParams");

        var current = await client.GetCurrentAccountAsync();
        var found = await client.FindAccountsAsync(new AccountFindParameters
        {
            AccountName = current.AccountName,
            Order = "created_at",
            OrderAscDesc = FindParameters.OrderDirection.Desc,
            PerPage = 5
        });

        Assert.Contains(current, found.Accounts);
    }

    /// <summary>
    ///     Successfully finds an account without search parameters.
    /// </summary>
    [Test]
    public async Task FindNoParams()
    {
        player.Play("FindNoParams");

        var current = await client.GetCurrentAccountAsync();
        var found = await client.FindAccountsAsync();

        Assert.Contains(current, found.Accounts);
    }

    /// <summary>
    ///     Successfully gets current account.
    /// </summary>
    [Test]
    public void GetCurrent()
    {
        player.Play("GetCurrent");

        Assert.DoesNotThrowAsync(async () =>
        {
            var current = await client.GetCurrentAccountAsync();
        });
    }

    /// <summary>
    ///     Successfully gets payment charges settings for given account.
    /// </summary>
    [Test]
    public async Task GetChargesSettings()
    {
        player.Play("GetChargesSettings");

        var settings = Accounts.PaymentCharges;

        var charges = await client.GetPaymentChargesSettingsAsync(settings.AccountId);
        Assert.AreEqual(charges.PaymentChargesSettings[0].AccountId, settings.AccountId);
        Assert.AreEqual(charges.PaymentChargesSettings[0].ChargeSettingsId, settings.ChargeSettingsId);
    }

    /// <summary>
    ///     Successfully manages given Account's Payment Charge Settings.
    /// </summary>
    [Test]
    public async Task ManageChargesSettings()
    {
        player.Play("ManageChargesSettings");

        var settings = Accounts.PaymentCharges;

        var charges = await client.ManageAccountPaymentChargesSettingsAsync(settings);
        Assert.AreEqual(settings, charges);
    }
}