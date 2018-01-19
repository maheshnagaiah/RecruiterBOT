using Microsoft.Bot.Builder.FormFlow;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.Bot.Builder.Dialogs;
using System.Threading.Tasks;
using Microsoft.Bot.Connector;

namespace PromptDialogs.Dialogs
{
    public enum TypeOfJob
    {

        Finance, Engineering, Professor, Services, SoftwareEngineer, HardWareEngineer
    }
    public enum technologies
    {
        dotNET, JAVA, SharePoint, MVC, AngularJS, SFB
    }

    [Serializable]
    public class ApplyForm
    {


        [Prompt("Please select the following Job Category? {||}", ChoiceStyle = ChoiceStyleOptions.Auto)]
        public TypeOfJob? JobCategory;
        [Prompt("In What {&} you want to apply? {||} ", ChoiceStyle = ChoiceStyleOptions.Auto)]
        public technologies? Technology;
        [Numeric(1, 10)]
        [Prompt("How many {&} you have in that technology?  ", ChoiceStyle = ChoiceStyleOptions.Default)]
        public int Experience;
        [Numeric(1, 10)]
        [Prompt("Provide your highest degree {&}", ChoiceStyle = ChoiceStyleOptions.Inline)]
        public float CGPA;
        [Prompt("Provide your current {&}", ChoiceStyle = ChoiceStyleOptions.Inline)]
        public double CTC;
        [Prompt("Provide your current working company?")]
        public string CurrentCompany;

        private List<string> jobcategory;

        public static IForm<ApplyForm> BuildForm()
        {
            return new FormBuilder<ApplyForm>()
                    .Message("Welcome to the new Interview Scheduling bot!")
                    .Build();
        }

    }

}