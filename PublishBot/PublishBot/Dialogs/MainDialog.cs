using Microsoft.Bot.Builder.Dialogs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Threading.Tasks;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Builder.FormFlow;

namespace PublishBot.Dialogs
{
    [Serializable]
    public class MainDialog : IDialog<IMessageActivity>
    {
        public async Task StartAsync(IDialogContext context)
        {
            await context.PostAsync("Welcome to Interview Scheduler");
          

            context.Wait(MessageRecieveAsync);
        }

        private async Task MessageRecieveAsync(IDialogContext context, IAwaitable<IMessageActivity> result)
        {
            var message = await result;
            var StartFrm = new FormDialog<StartForm>(new StartForm(), StartForm.BuildForm, FormOptions.PromptInStart, null);
            context.Call<StartForm>(StartFrm, afterResume);
            //context.Done(message);
            
        }

        private async Task afterResume(IDialogContext context, IAwaitable<StartForm> result)
        {
            var message = await result;
            if(message.InterviewOptns.ToString() == "ApplyNew")
            {
                var NewJobForm = new FormDialog<NewJob>(new NewJob(), NewJob.BuildForm, FormOptions.PromptInStart, null);
                context.Call<NewJob>(NewJobForm, ProcessJobInfo);
            }
        }

        private async Task ProcessJobInfo(IDialogContext context, IAwaitable<NewJob> result)
        {
            var message = await result;
            await context.PostAsync("Thanks for choosing Interview Bot we are procesing your Interview");
            //processing Interview Schedule
            context.Done(message);
        }
    }
}