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
    [Serializable]
    public partial class UserProfile
    {
        public int UserID { get; set; }
        public string UserChannelId { get; set; }
        public string ChannelId { get; set; }
        public string UserName { get; set; }
        public string FirstName { get; set; }
        public string MiddleName { get; set; }
        public string LastName { get; set; }
        public string EmailID { get; set; }
        public string PhoneNumber { get; set; }
        public string Degree { get; set; }
        public Nullable<double> CGPA { get; set; }
        public string ExpertiseInTechnology { get; set; }
        public string CurrentCompany { get; set; }
        public Nullable<int> Experience { get; set; }
        public byte[] Resume { get; set; }
    }
}
