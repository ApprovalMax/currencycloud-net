using System;
using CurrencyCloud.Entity.Onboarding.Enums;

namespace CurrencyCloud.Entity.Onboarding;

public class AccountUsage
{
    /// <summary>
    /// List of IDs for any uploaded supporting documents.
    /// </summary>
    public Guid[] DocumentIds { get; set; }
    /// <summary>
    /// Does the account receive funds from third parties.
    /// </summary>
    public required bool CollectionsAccount { get; set; }
    /// <summary>
    /// List of currencies to fund account with. Three character ISO currency codes.
    /// </summary>
    public string[] CollectionCurrencies { get; set; }
    /// <summary>
    /// List of payout currencies. Three character ISO currency codes.
    /// </summary>
    public string[] PaymentCurrencies { get; set; }
    /// <summary>
    /// List of countries that corporate accounts want to send/receive money to/from.
    /// </summary>
    public string[] TransactionCountries { get; set; }
    /// <summary>
    /// Do corporate accounts just want to send a single one-off transaction.
    /// </summary>
    public bool OneOffTransaction { get; set; }
    /// <summary>
    /// Currency of the estimated monthly transaction value.
    /// </summary>
    public string EstimatedMonthlyTransactionCurrency { get; set; }
    /// <summary>
    /// Estimated monthly value of what a corporate account will transact on the platform.
    /// </summary>
    public int EstimatedMonthlyTransactionValue { get; set; }
    /// <summary>
    /// How often do corporates expect to send or receive funds.
    /// </summary>
    public int EstimatedMonthlyTransactionVolume { get; set; }
    /// <summary>
    /// The source of the funds that will be sent through the platform.
    /// </summary>
    public SourceOfFunds SourceOfFunds { get; set; }
    /// <summary>
    /// The reason for opening an account with Currencycloud.
    /// </summary>
    public required PrimaryPurpose PrimaryPurpose { get; set; }
    /// <summary>
    /// Additional details if primary_purpose is 'other'.
    /// </summary>
    public string PurposeDetails { get; set; }
    /// <summary>
    /// Initial capital of the company.
    /// </summary>
    public string InitialCapital { get; set; }
}
