using System;

namespace CurrencyCloud.Entity
{
    public class ReportRequestFindParameters : FindParameters
    {
        ///<summary>
        /// Your unique short reference
        ///</summary>
        [Param]
        public string ShortReference { get; set; }

        ///<summary>
        /// Description provided during the report creation process
        ///</summary>
        [Param]
        public string Description { get; set; }

        ///<summary>
        /// Start date (for range) when the reports were created
        ///</summary>
        [Param]
        public DateOnly? CreatedAtFrom { get; set; }

        ///<summary>
        /// End date (for range) when the reports were created
        ///</summary>
        [Param]
        public DateOnly? CreatedAtTo { get; set; }
        
        /// <summary>
        /// Start date (for range), in ISO 8601 format, for when the reports were updated.
        /// </summary>
        [Param]
        public DateOnly? UpdatedAtFrom { get; set; }
        
        /// <summary>
        /// End date (for range), in ISO 8601 format, for when the reports were updated.
        /// </summary>
        [Param]
        public DateOnly? UpdatedAtTo { get; set; }

        ///<summary>
        /// Start date (for range) when the report will be (was) expired
        ///</summary>
        [Param]
        public DateOnly? ExpirationDateFrom { get; set; }

        ///<summary>
        /// End date (for range) when the report will be (was) expired
        ///</summary>
        [Param]
        public DateOnly? ExpirationDateTo { get; set; }

        ///<summary>
        /// Status of the report
        ///</summary>
        [Param]
        public string Status { get; set; }

        ///<summary>
        /// Type of reports expected to return in search results
        ///</summary>
        [Param]
        public string ReportType { get; set; }
    }
}