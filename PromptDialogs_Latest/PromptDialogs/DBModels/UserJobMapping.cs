//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated from a template.
//
//     Manual changes to this file may cause unexpected behavior in your application.
//     Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace PromptDialogs.DBModels
{
    using System;
    using System.Collections.Generic;
    
    public partial class UserJobMapping
    {
        public long UserJobMappingID { get; set; }
        public Nullable<long> UserID { get; set; }
        public Nullable<long> JobListings { get; set; }
        public string InterviewStatus { get; set; }
        public Nullable<System.DateTime> InterviewScheduledDate { get; set; }
        public string PointOfContact { get; set; }
    }
}