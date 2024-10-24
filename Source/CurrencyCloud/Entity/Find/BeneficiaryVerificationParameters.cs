using Newtonsoft.Json;

namespace CurrencyCloud.Entity
{
    public class BeneficiaryVerificationParameters : Entity
    {
        [JsonConstructor]
        public BeneficiaryVerificationParameters()
        {
        }

        /// <summary>
        /// Type of payment
        /// </summary>
        [Param]
        public string PaymentType { get; set; }

        /// <summary>
        /// Country of the bank
        /// </summary>
        [Param]
        public string BankCountry { get; set; }

        /// <summary>
        /// Currency of the payment
        /// </summary>
        [Param]
        public string Currency { get; set; }

        /// <summary>
        /// Account number
        /// </summary>
        [Param]
        public string AccountNumber { get; set; }

        /// <summary>
        /// Type of beneficiary entity
        /// </summary>
        [Param]
        public string BeneficiaryEntityType { get; set; }

        /// <summary>
        /// Name of the beneficiary company
        /// </summary>
        [Param]
        public string BeneficiaryCompanyName { get; set; }

        /// <summary>
        /// First name of the beneficiary
        /// </summary>
        [Param]
        public string BeneficiaryFirstName { get; set; }

        /// <summary>
        /// Last name of the beneficiary
        /// </summary>
        [Param]
        public string BeneficiaryLastName { get; set; }

        /// <summary>
        /// Type of the first routing code
        /// </summary>
        [Param]
        public string RoutingCodeType1 { get; set; }

        /// <summary>
        /// Value of the first routing code
        /// </summary>
        [Param]
        public string RoutingCodeValue1 { get; set; }

        /// <summary>
        /// Type of the second routing code
        /// </summary>
        [Param]
        public string RoutingCodeType2 { get; set; }

        /// <summary>
        /// Value of the second routing code
        /// </summary>
        [Param]
        public string RoutingCodeValue2 { get; set; }

        /// <summary>
        /// BIC/SWIFT code
        /// </summary>
        [Param]
        public string BicSwift { get; set; }

        /// <summary>
        /// IBAN
        /// </summary>
        [Param]
        public string Iban { get; set; }

        /// <summary>
        /// Secondary reference data
        /// </summary>
        [Param]
        public string SecondaryReferenceData { get; set; }

        public override bool Equals(object obj)
        {
            if (!(obj is BeneficiaryVerificationParameters))
            {
                return false;
            }

            var request = obj as BeneficiaryVerificationParameters;

            return PaymentType == request.PaymentType &&
                   BankCountry == request.BankCountry &&
                   Currency == request.Currency &&
                   AccountNumber == request.AccountNumber &&
                   BeneficiaryEntityType == request.BeneficiaryEntityType &&
                   BeneficiaryCompanyName == request.BeneficiaryCompanyName &&
                   BeneficiaryFirstName == request.BeneficiaryFirstName &&
                   BeneficiaryLastName == request.BeneficiaryLastName &&
                   RoutingCodeType1 == request.RoutingCodeType1 &&
                   RoutingCodeValue1 == request.RoutingCodeValue1 &&
                   RoutingCodeType2 == request.RoutingCodeType2 &&
                   RoutingCodeValue2 == request.RoutingCodeValue2 &&
                   BicSwift == request.BicSwift &&
                   Iban == request.Iban &&
                   SecondaryReferenceData == request.SecondaryReferenceData;
        }

        public override int GetHashCode()
        {
            return AccountNumber.GetHashCode();
        }
    }
}
