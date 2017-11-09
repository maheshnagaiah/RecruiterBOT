using Microsoft.Bot.Builder.FormFlow;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace PublishBot.Dialogs
{
    public enum InterviewOptions
    {
        [Display(Name = "Apply New")]
        ApplyNew,
        [Display(Name = "Cancel Appointment")]
        CancelAppointment,
        [Display(Name = "Reschedule Appointment")]
        RescheduleAppointment
    };

    [Serializable]
    public class StartForm
    {

        public InterviewOptions? InterviewOptns;
        public static IForm<StartForm> BuildForm()
        {

            return new FormBuilder<StartForm>()
                        .Message("Welcome to the Interview Scheduling bot!")
                        .Build();
        }

    }
}