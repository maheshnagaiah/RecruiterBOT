using Microsoft.Bot.Builder.FormFlow;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using PromptDialogs.DBModels;
using System.Linq;

namespace PromptDialogs.Dialogs
{

    public enum Degree1
    {
        BTech,
        MTech,
        MSC,
        BSC,
        PHD,
        BCom
    }

    [Serializable]
    public class SampleProfile
    {
        [Prompt("Please Provide Your FirstName ?")]
        public string FirstName { get; set; }

        [Prompt("Please Provide Your MiddleName ?")]
        public string MiddleName { get; set; }

        [Prompt("Please Provide Your LastName ?")]
        public string LastName { get; set; }

        public static IForm<SampleProfile> BuildForm()
        {


            return new FormBuilder<SampleProfile>()
                .Message("Please Fill your Profile")
                .Field(nameof(FirstName))
                .Field(nameof(MiddleName))
                .Field(nameof(LastName))
                .Confirm()
                .Build();
        }
    }
}