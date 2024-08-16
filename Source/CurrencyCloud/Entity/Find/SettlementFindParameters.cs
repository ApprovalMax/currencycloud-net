using System;

namespace CurrencyCloud.Entity
{
    public class SettlementFindParameters : FindParameters
    {
        ///<summary>
        /// Unique human readable identifier
        ///</summary>
        [Param]
        public string ShortReference { get; set; }

        ///<summary>
        /// The current status of the settlement
        ///</summary>
        [Param]
        public string Status { get; set; }

        ///<summary>
        /// ISO 8601 Datetime when the settlement was created
        ///</summary>
        [Param]
        public DateTimeOffset? CreatedAtFrom { get; set; }

        ///<summary>
        /// ISO 8601 Datetime when the settlement was created
        ///</summary>
        [Param]
        public DateTimeOffset? CreatedAtTo { get; set; }

        ///<summary>
        /// ISO 8601 Datetime when the settlement was updated
        ///</summary>
        [Param]
        public DateTimeOffset? UpdatedAtFrom { get; set; }

        ///<summary>
        /// ISO 8601 Datetime when the settlement was updated
        ///</summary>
        [Param]
        public DateTimeOffset? UpdatedAtTo { get; set; }

        ///<summary>
        /// ISO 8601 Datetime when the settlement was updated
        ///</summary>
        [Param]
        public DateTimeOffset? ReleasedAtFrom { get; set; }

        ///<summary>
        /// ISO 8601 Datetime when the settlement was updated
        ///</summary>
        [Param]
        public DateTimeOffset? ReleasedAtTo { get; set; }
    }
}