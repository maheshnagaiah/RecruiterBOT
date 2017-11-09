using Microsoft.Bot.Builder.FormFlow;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace PublishBot.Dialogs
{
        public enum TypeOfJob
        {
            [Terms("M")]

            Finance,
            Engineering, Professor, Services, SoftwareEngineer, HardWareEngineer
        }
        public enum technologies
        {
            dotNET, JAVA, SharePoint, MVC, AngularJS, SFB
        }

        [Serializable]
        public class NewJob
        {
            [Prompt("Please select the following Job Category? {||}", ChoiceStyle = ChoiceStyleOptions.Auto)]
            [Describe(Description = "{&}")]
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


            public static IForm<NewJob> BuildForm()
            {
                    return new FormBuilder<NewJob>()
                            .Message("Welcome to the new Interview Scheduling bot!")
                            //.Field(nameof(JobCategory))
                            //.Field(nameof(Technology), validate: async (state, value) =>
                            //{
                            //    var result = new ValidateResult();
                            //    if (value.ToString()== "dotNET")
                            //    {
                            //        result.Value = "hjkf";
                            //        result.IsValid = true;
                            //    }
                            //    return result;
                            //})
                            // .Field(nameof(Experience))

                            .Build();
            }


        }
    }