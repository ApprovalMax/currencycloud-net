namespace CurrencyCloud.Entity
{
    public class AddFundsParameters : FindParameters
    {
        
        ///<summary>
        /// Unique value in UUID format, user-generated with each request
        ///</summary>
        [Param]
        public string Id { get; set; }
        ///<summary>
        /// ID of the Account this SSI is attached to
        ///</summary>
        [Param]
        public string AccountId { get; set; }

        ///<summary>
        /// The currency that should be sent to these account details
        ///</summary>
        [Param]
        public string Currency { get; set; }

        ///<summary>
        /// Amount of the emulated transaction
        ///</summary>
        [Param]
        public decimal Amount { get; set; }
        
        /// <summary>
        /// The sender's name
        /// </summary>
        [Param]
        public string SenderName { get; set; }
        
        /// <summary>
        /// The sender's address
        /// </summary>
        [Param]
        public string SenderAddress { get; set; }
        
        /// <summary>
        /// Two-digit sender country
        /// </summary>
        [Param]
        public string SenderCountry { get; set; }
        
        /// <summary>
        /// Sender reference
        /// </summary>
        [Param]
        public string SenderReference { get; set; }
        
        /// <summary>
        /// Sender account number
        /// </summary>
        [Param]
        public string SenderAccountNumber { get; set; }
        
        /// <summary>
        /// Sender routing code
        /// </summary>
        [Param]
        public string SenderRoutingCode{ get; set; }
        
        /// <summary>
        /// A client's virtual account number or its sub-account (as seen in Find Funding Accounts)
        /// </summary>
        [Param]
        public string ReceiverAccountNumber { get;  set;  }
        
        /// <summary>
        /// A client's virtual account routing code or its sub-account (as seen in Find Funding Accounts)
        /// </summary>
        [Param]
        public string ReceiverRoutingCode { get;  set;  }
        
        /// <summary>
        /// Allows you to trigger approval or rejection behaviour
        /// </summary>
        [Param]
        public string Action { get;  set;  }
        
    }
}