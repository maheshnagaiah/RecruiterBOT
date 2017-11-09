using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Description;
using Microsoft.Bot.Connector;
using Newtonsoft.Json;
using Microsoft.Bot.Builder.Dialogs;
using System.Collections.Generic;
using Microsoft.Bot.Builder.FormFlow;
using PublishBot.Dialogs;
using PublishBot.Models;

namespace PublishBot
{
    [BotAuthentication]
    public class MessagesController : ApiController
    {
        /// <summary>
        /// POST: api/Messages
        /// Receive a message from a user and reply to it
        /// </summary>
        public async Task<HttpResponseMessage> Post([FromBody]Activity activity)
        {
            if (activity.Type == ActivityTypes.Message)
            {
                //if (activity.Text == "1")
                //{
                //    await Conversation.SendAsync(activity, () => MakeNewInterviewDialog());
                //}

               // else
                    await Conversation.SendAsync(activity, () => new MainDialog());

                //Sarath - 11/9/2017 --> Below lines of code will make a call to Document DB

                //var text = "Sarath-Test";
                //dynamic res2 = await DocumentDBRepository<Item>.ExecuteSPByName<string>("SearchData", text);
                //if (res2 != null)
                //{
                //    var ssd = res2.Response;
                //}
            }
            else
            {
                HandleSystemMessage(activity);
            }
            var response = Request.CreateResponse(HttpStatusCode.OK);
            return response;
        }

        internal static IDialog<NewJob> MakeNewInterviewDialog()
        {

            return Chain.From(() => FormDialog.FromForm(NewJob.BuildForm))

                  .Do(async (context, order) =>
                  {
                      try
                      {

                          #region buttons
                          List<CardAction> cardbuttons = new List<CardAction>();

                          CardAction btn1 = new CardAction()
                          {

                              Value = "JOBID1234",
                              Type = "imBack",
                              Title = "JOBID1234"
                          };
                          CardAction btn2 = new CardAction()
                          {
                              Value = "http://google.com",
                              Type = "openUrl",
                              Title = "Deatils",

                          };
                          //CardAction btn3 = new CardAction()
                          //{
                          //    Value = "btn3",
                          //    Type = "imBack",
                          //    Title = "cancel appiontement",


                          //};

                          cardbuttons.Add(btn1);
                          cardbuttons.Add(btn2);

                          HeroCard hcard = new HeroCard();

                          hcard.Title = "Please see in Deatils for Job Description";
                          hcard.Text = "Job based on Sharepoint Technology need 4 to 5 years expr in Dot Net";
                          hcard.Images = new List<CardImage> { new CardImage(url: "http://clipart-library.com/img/2071327.jpg") };
                          hcard.Buttons = cardbuttons;
                          #endregion
                          var completed = await order;
                          var replyMessage = context.MakeMessage();
                          replyMessage.AttachmentLayout = "carousel";
                          Attachment attachment = hcard.ToAttachment();
                          replyMessage.Attachments = new List<Attachment> { attachment, attachment, attachment, attachment };
                          await context.PostAsync(replyMessage);
                          // Actually process the sandwich order...
                          //  await context.PostAsync("Processed your order!");
                      }
                      catch (FormCanceledException<NewJob> e)
                      {
                          string reply;
                          if (e.InnerException == null)
                          {
                              reply = $"You quit on {e.Last} -- maybe you can finish next time!";
                          }
                          else
                          {
                              reply = "Sorry, I've had a short circuit. Please try again.";
                          }
                          await context.PostAsync(reply);
                      }
                  });
        }

        private Activity HandleSystemMessage(Activity message)
        {
            if (message.Type == ActivityTypes.DeleteUserData)
            {
                // Implement user deletion here
                // If we handle user deletion, return a real message
            }
            else if (message.Type == ActivityTypes.ConversationUpdate)
            {
                // Handle conversation state changes, like members being added and removed
                // Use Activity.MembersAdded and Activity.MembersRemoved and Activity.Action for info
                // Not available in all channels
            }
            else if (message.Type == ActivityTypes.ContactRelationUpdate)
            {
                // Handle add/remove from contact lists
                // Activity.From + Activity.Action represent what happened
            }
            else if (message.Type == ActivityTypes.Typing)
            {
                // Handle knowing tha the user is typing
            }
            else if (message.Type == ActivityTypes.Ping)
            {
            }

            return null;
        }
    }
}