using Microsoft.Bot.Builder.FormFlow;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using PromptDialogs.DBModels;
using System.Linq;
using Microsoft.Bot.Builder.Luis.Models;
using Microsoft.Bot.Builder.Dialogs;
using System.ComponentModel.DataAnnotations;

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

        [Prompt("Please Provide Your MiddleName ?")] 
        public string MiddleName { get; set; }

        [Prompt("Please Provide Your LastName ?")]
        public string LastName { get; set; }

        [Prompt("Please Provide Your Personal Email ID ?")]
        [Pattern(@"[a-z0-9!#$%&'*+/=?^_`{|}~-]+(?:\.[a-z0-9!#$%&'*+/=?^_`{|}~-]+)*@(?:[a-z0-9](?:[a-z0-9-]*[a-z0-9])?\.)+[a-z0-9](?:[a-z0-9-]*[a-z0-9])?")]
        public string Email { get; set; } 

        [Prompt("Please Provide Your Personal Phone No. ?")] [Optional]
        [Pattern(@"^\(?([0-9]{3})\)?[-. ]?([0-9]{3})[-. ]?([0-9]{4})$")]
        public string PhoneNo { get; set; }

        [Prompt("Please Select Highest Degree you have? {||}", ChoiceStyle = ChoiceStyleOptions.Auto)]
        public Degree? HighestDegree { get; set; }

        [Prompt("Please Provide CGPA in your HighestDegree on a scale of 10 ")]
        //[Numeric(0, 10)]
        [Range(0, double.MaxValue, ErrorMessage = "Please enter valid float Number")]
        public double CGPA { get; set; }

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
                        validate: async (s1, value) =>
                        {
                            ValidateResult result = new ValidateResult();
                            if (validateEmail(value.ToString()))
                            {
                                RecruitBOTEntities dbcontext = new RecruitBOTEntities();
                                var isExists = dbcontext.UserProfiles.Any(s2 => s2.EmailID == value.ToString());
                                if (isExists)
                                {
                                    result.Feedback = $"This {value} is already registered please give Email ID.";
                                    result.IsValid = false;
                                    result.Value = null;
                                }
                                else
                                {
                                    result.IsValid = true;
                                    result.Value = value;
                                }
                            }
                            else
                            {
                                result.Feedback = $"This is not a valid email format, please provide correct";
                                result.IsValid = false;
                                result.Value = null;
                            }
                            return result;

                        })
                    .Field(nameof(PhoneNo),

                        validate: async (s1, value) =>
                        {
                            ValidateResult result = new ValidateResult();
                            if (validatePhone(value.ToString()))
                            {
                                RecruitBOTEntities dbcontext = new RecruitBOTEntities();
                                var isExists = dbcontext.UserProfiles.Any(s2 => s2.PhoneNumber == value.ToString());
                                if (isExists)
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
                            }
                            else
                            {
                                result.Feedback = $"This is not a valid mobile format, please provide correct";
                                result.IsValid = false;
                                result.Value = null;
                            }
                            return result;

                        })
                    .AddRemainingFields()

                    .Build();
            

            

        }

        public static bool validateEmail(string email)
        {
            if (email == string.Empty || string.IsNullOrEmpty(email))
                return true;
            Regex regex = new Regex(@"^([\w\.\-]+)@([\w\-]+)((\.(\w){2,3})+)$");
            Match match = regex.Match(email);
            return match.Success;
        }

        public static bool validatePhone(string phone)
        {
            if (phone == string.Empty || string.IsNullOrEmpty(phone))
                return true;
            Regex regex = new Regex(@"^([0]|\+91)?\d{10}$");
            Match match = regex.Match(phone);
            return match.Success;

        }

       
    }
}