using CurrencyCloud.Entity.Enums;
using Newtonsoft.Json;

namespace CurrencyCloud.Entity
{
    public class BeneficiaryVerification : Entity
    {
        [JsonConstructor]
        public BeneficiaryVerification()
        {
        }

        /// <summary>
        /// The account verification result - 'full_match', 'close_match' or 'no_match'.
        /// </summary>
        public VerificationAnswer Answer { get; set; }

        /// <summary>
        /// The actual name of the account holder.
        /// </summary>
        public string ActualName { get; set; }

        /// <summary>
        /// Encoded reason.
        /// </summary>
        public string ReasonCode { get; set; }

        /// <summary>
        /// Description of the reason.
        /// </summary>
        public string Reason { get; set; }

        /// <summary>
        /// Category of reason - 'okay', 'warning' or 'rejected'.
        /// </summary>
        public VerificationReasonType ReasonType { get; set; }

        public override bool Equals(object obj)
        {
            if (!(obj is BeneficiaryVerification))
            {
                return false;
            }

            var result = obj as BeneficiaryVerification;

            return Answer == result.Answer &&
                   ActualName == result.ActualName &&
                   ReasonCode == result.ReasonCode &&
                   Reason == result.Reason &&
                   ReasonType == result.ReasonType;
        }

        public override int GetHashCode()
        {
            return Answer.GetHashCode();
        }
    }
}