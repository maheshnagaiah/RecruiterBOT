using Microsoft.Bot.Builder.FormFlow;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Threading.Tasks;

namespace PromptDialogs.Dialogs
{
    public enum Degree
    {
       BTech,
       MTech,
       MSC,
       BSC,
       PHD,
       BCom
    }
    [Serializable]
    public class BuildUserProfile
    {


        [Prompt("Please Provide Your FirstName ?")]
        public string FirstName { get; set; }

        [Prompt("Please Provide Your MiddleName ?")] [Optional]
        public string MiddleName { get; set; }

        [Prompt("Please Provide Your LastName ?")]
        public string LastName { get; set; }

        [Prompt("Please Provide Your Personal Email ID ?")] 
        [Pattern(@"[a-z0-9!#$%&'*+/=?^_`{|}~-]+(?:\.[a-z0-9!#$%&'*+/=?^_`{|}~-]+)*@(?:[a-z0-9](?:[a-z0-9-]*[a-z0-9])?\.)+[a-z0-9](?:[a-z0-9-]*[a-z0-9])?")]
        public string Email { get; set; }

        [Prompt("Please Provide Your Personal Phone No. ?")]
        [Pattern(@"^\(?([0-9]{3})\)?[-. ]?([0-9]{3})[-. ]?([0-9]{4})$")]
        public string PhoneNo { get; set; }

        [Prompt("Please Select Highest Degree you have? {||}", ChoiceStyle = ChoiceStyleOptions.Auto)]
        public Degree? HighestDegree { get; set; }

        [Prompt("Please Provide CGPA in your HighestDegree on a scale of 10 ")]
        [Numeric(0, 10)]
        public int CGPA { get; set; }

        [Prompt("Please Provide your Expertise Technology? like: .NET/JAVA/AngularJS/PHP/Sharepoint")]
        public string ExpertiseInTechnology;

        [Prompt("How many years of you have in that technology ?")]
        [Numeric(0, 20)]
        public int TechnologyExpereince;

        [Prompt("Please Provide Your Current Working Company ?")]
        public string CurrentCompany;

        public static IForm<BuildUserProfile> BuildForm()
        {
            return new FormBuilder<BuildUserProfile>()
                .Message("Please Fill your Profile")
                .Field(nameof(FirstName))
                .Field(nameof(MiddleName))
                .Field(nameof(LastName))
                .Field(nameof(Email), 
                    validate: async (s1,value) => {
                        ValidateResult result = new ValidateResult();
                        bool isValuePresent = false;
                        if (isValuePresent)
                        {
                            result.Feedback = $"This {value} is already registered please give different Email ID";
                            result.IsValid = false;
                            result.Value = null;
                        }
                        else
                        {
                            result.IsValid = true;
                            result.Value = value;
                        }
                        return result;

                    })
                .Field(nameof(PhoneNo), 
                
                    validate: async (s1,value) => {
                    ValidateResult result = new ValidateResult();
                    bool isValuePresent = false;
                    if (isValuePresent)
                    {
                        result.Feedback = $"This {value} is already registered please give different phone no.";
                        result.IsValid = false;
                        result.Value = null;
                    }
                    else
                    {
                        result.IsValid = true;
                        result.Value = value;
                    }
                    return result;
                     })
                .Field(nameof(HighestDegree))
                .Field(nameof(CGPA))
                .AddRemainingFields()
                .Confirm(async (state) => {
                    var message = $"are you sure you want to proceed with following options {state.CGPA}";
                    return new PromptAttribute(message);
                })
                .Build();
        }
    }
}