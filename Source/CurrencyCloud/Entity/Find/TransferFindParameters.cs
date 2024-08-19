using System;

namespace CurrencyCloud.Entity
{
    public class TransferFindParameters : FindParameters
    {
        ///<summary>
        /// Short reference code
        ///</summary>
        [Param]
        public string ShortReference { get; set; }

        ///<summary>
        /// Account UUID of the paying account
        ///</summary>
        [Param]
        public string SourceAccountId { get; set; }

        ///<summary>
        /// Account UUID of the receiving account
        ///</summary>
        [Param]
        public string DestinationAccountId { get; set; }

        ///<summary>
        /// Transfer status
        ///</summary>
        [Param]
        public string Status { get; set; }

        ///<summary>
        /// Three-digit currency code
        ///</summary>
        [Param]
        public string Currency { get; set; }

        ///<summary>
        /// Minimum amount
        ///</summary>
        [Param]
        public decimal? AmountFrom { get; set; }

        ///<summary>
        /// Maximum amount
        ///</summary>
        [Param]
        public decimal? AmountTo { get; set; }

        ///<summary>
        /// Any valid ISO 8601 format, eg. "2017-12-31T23:59:59Z"
        ///</summary>
        [Param]
        public DateTimeOffset? CreatedAtFrom { get; set; }

        ///<summary>
        /// Any valid ISO 8601 format, eg. "2017-12-31T23:59:59Z"
        ///</summary>
        [Param]
        public DateTimeOffset? CreatedAtTo { get; set; }

        ///<summary>
        /// Any valid ISO 8601 format, eg. "2017-12-31T23:59:59Z"
        ///</summary>
        [Param]
        public DateTimeOffset? UpdatedAtFrom { get; set; }

        ///<summary>
        /// Any valid ISO 8601 format, eg. "2017-12-31T23:59:59Z"
        ///</summary>
        [Param]
        public DateTimeOffset? UpdatedAtTo { get; set; }

        ///<summary>
        /// Any valid ISO 8601 format, eg. "2017-12-31T23:59:59Z"
        ///</summary>
        [Param]
        public DateTimeOffset? CompletedAtFrom { get; set; }

        ///<summary>
        /// Any valid ISO 8601 format, eg. "2017-12-31T23:59:59Z"
        ///</summary>
        [Param]
        public DateTimeOffset? CompletedAtTo { get; set; }

        ///<summary>
        /// Contact UUID of transfer instructor
        ///</summary>
        [Param]
        public string CreatorAccountId { get; set; }

        ///<summary>
        /// Account UUID of transfer instructor
        ///</summary>
        [Param]
        public string CreatorContactId { get; set; }
    }
}