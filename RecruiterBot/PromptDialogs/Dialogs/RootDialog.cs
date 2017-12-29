using System;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using PromptDialogs.Models;
using System.Collections.Generic;
using Microsoft.Bot.Builder.FormFlow;
using PromptDialogs.DBModels;
using System.Linq;
using Microsoft.WindowsAzure.Storage;
using Microsoft.Azure;
using Microsoft.WindowsAzure.Storage.Blob;
using System.Net.Http;
using System.IO;
using System.Net;
using Microsoft.Bot.Builder.Luis.Models;
using Microsoft.Azure.Search;
using Microsoft.Azure.Search.Models;

namespace PromptDialogs.Dialogs
{
    [Microsoft.Bot.Builder.Luis.LuisModel("5c72b4c3-f8e5-477b-987f-94696a0173b0", "8446cff3f53c49edba480da797a423e4")]
    [Serializable]
    public class RootDialog : IDialog<IMessageActivity>
    {
        private static string contentIndex = "azureblob";
        private double SCORE_THRESHOLD = 1.0;

        public  Task StartAsync(IDialogContext context)
        {
            context.Wait(MessageReceivedAsync);

            return Task.CompletedTask;
        }


        private async Task MessageReceivedAsync(IDialogContext context, IAwaitable<IMessageActivity> result)
        {
            var activity = await result ;
            //Query to DB for USER PRESENCE
            InterviewBotEntities DbContext = new InterviewBotEntities();
            var userinfo = (from usrs in DbContext.UserProfiles where usrs.UserChannelId == context.Activity.From.Id && usrs.ChannelId == context.Activity.ChannelId select usrs).FirstOrDefault();
            
            if (userinfo == null)
            {
                await context.PostAsync("Welcome New User,Update your Profile Details");
                var NewProfileBuilder = new FormDialog<BuildUserProfile>(new BuildUserProfile(), BuildUserProfile.BuildForm,FormOptions.PromptInStart);
                context.Call(NewProfileBuilder, SaveProfileToDB);

            }
            else
            {
                await context.PostAsync($"Welcome {userinfo.UserName} ");
                string[] display;
                // need to query to DB to get appointments of User and save to BOT STATE
                context.UserData.SetValue<UserProfile>("UserInfo", userinfo); // saved User Profile Details to BOT 
                List<UserJobMapping> userjobmappings = new List<UserJobMapping>();
                var appointments = (from jobmappings in DbContext.UserJobMappings where jobmappings.UserID == userinfo.UserID && jobmappings.InterviewStatus == "active" select jobmappings).ToList();
                
                if (appointments.Count>0)
                {
                    //saving UserAppointments to BOT STATE
                    context.UserData.SetValue<List<UserJobMapping>>("UserJobMappings", appointments);
                    display = new string[4] { "Apply for New Appointment", "Cancel Appointment", "ReschuleAppointment", "Profile Update" };
                    
                }
                else
                {
                    display = new string[2] { "Apply for New Appointment", "Profile Update" };
                }
                var newdialog = new PromptDialog.PromptChoice<string>(display, "Please select below Options?", "Sorry, that wans't a valid option", 3);
                context.Call(newdialog, routeDialog);
            }
            
        }

        private async Task SaveProfileToDB(IDialogContext context, IAwaitable<BuildUserProfile> result)
        {
            await context.PostAsync("Saving User Profile to Database");
            var message = await result;
            UserProfile userProfile = new UserProfile()
            {
                UserChannelId = context.Activity.From.Id,
                ChannelId = context.Activity.ChannelId,
                UserName = context.Activity.From.Name,
                FirstName = message.FirstName,
                MiddleName =message.LastName,
                LastName= message.LastName,
                EmailID = message.Email,
                PhoneNumber = message.PhoneNo,
                Degree = message.HighestDegree.ToString(),
                CGPA = message.CGPA,
                ExpertiseInTechnology = message.ExpertiseInTechnology,
                CurrentCompany = message.CurrentCompany,
                Experience = message.TechnologyExpereince,

            };

            InterviewBotEntities dbcontext = new InterviewBotEntities();
            dbcontext.UserProfiles.Add(userProfile);
            dbcontext.SaveChanges();
            await context.PostAsync("Profile Updated Successfully, go ahead and apply for JOBS");
            context.Wait(MessageReceivedAsync);
        }

        private async Task routeDialog(IDialogContext context, IAwaitable<string> result)
        {
            var message = await result;
            if(message == "Apply for New Appointment")
            {
                var StartFrm = new FormDialog<ApplyForm>(new ApplyForm(), ApplyForm.BuildForm, FormOptions.PromptInStart, null);
                context.Call<ApplyForm>(StartFrm, askForResume);
            }
            else if (message == "Cancel Appointment")
            {
                // getting user Appointments 
                List<Attachment> attachements = getHerocard(context, "cancel");
                if(attachements.Count >0)
                {
                    await context.PostAsync("Please select job ID which you want cancel");
                    var replyMessage = context.MakeMessage();
                    replyMessage.AttachmentLayout = AttachmentLayoutTypes.Carousel;
                    replyMessage.Attachments = attachements;
                    await context.PostAsync(replyMessage);
                    context.Wait(cancelAppointment);
                }
                else
                {
                    await context.PostAsync($"Sorry No active jobs found");
                    context.Done(this);
                }

            }
            else if (message == "ReschuleAppointment")
            {

            }
            else
            {
                //default case for Profile Building

            }
        }

        private async Task cancelAppointment(IDialogContext context, IAwaitable<IMessageActivity> result)
        {
            var message = await result ;
            long id = Convert.ToInt64(message.Text);
            InterviewBotEntities dbcntx = new InterviewBotEntities();
            var output = (from db in dbcntx.UserJobMappings where db.JobListings == id select db).FirstOrDefault();
            if(output!= null)
            {
                output.InterviewStatus = "Inactive";
                dbcntx.SaveChanges();
                await context.PostAsync("Succefully cancelled your appointement");
                context.Done(message);
            }
            else
            {
                await context.PostAsync("please provide valid Job refernce or click on button you want to cancel");
                context.Wait(cancelAppointment);
            }
            


        }

        private async Task askForResume(IDialogContext context, IAwaitable<ApplyForm> result)
        {
            // saving current input to BOTSTATE 
            var message = await result;
            //storing form Data in BOT Connector
            context.UserData.SetValue<ApplyForm>("FormDeatils", message);
            await context.PostAsync("Please Provide Resume in docx/pdf format ");
            context.Wait(WaitForResume);
        }
        private async Task WaitForResume(IDialogContext context, IAwaitable<IMessageActivity> result)
        {
            var resume = await result;
            if (resume.Attachments.Count > 0 && (resume.Attachments[0].ContentType == "application/pdf" || resume.Attachments[0].ContentType == "application/vnd.openxmlformats-officedocument.wordprocessingml.document"))
            {
                #region UploadUserResumeToBLob
                var attachmentUrl = resume.Attachments[0].ContentUrl;
                var httpClient = new HttpClient();
                var connector = new ConnectorClient(new Uri(resume.ServiceUrl));
                byte[] ResumeData = connector.HttpClient.GetByteArrayAsync(attachmentUrl).Result;
                //Uploading Resume to BLOB
                string userID = context.UserData.GetValue<UserProfile>("UserInfo").UserID.ToString();
                uploadToBlob("resume", ResumeData, userID);
                #endregion

                #region QueryToDBForJobListings

                List<Attachment> getHcard = getHerocard(context, "new");
                if (getHcard.Count > 0)
                {
                    var replyMessage = context.MakeMessage();
                    replyMessage.AttachmentLayout = AttachmentLayoutTypes.Carousel;
                    replyMessage.Attachments = getHcard;
                    await context.PostAsync(replyMessage);
                    context.Wait(saveUserJobDeatils);
                }
                else
                {
                    await context.PostAsync($"Sorry We couldn't find jobs for related {context.UserData.GetValue<ApplyForm>("FormDeatils").Technology} technology");
                    context.Done(this);
                }


                #endregion

            }
            else
            {
                await context.PostAsync("Please provide attachment in either docx or pdf ");
                context.Wait(WaitForResume);
            }
        }

        private async Task saveUserJobDeatils(IDialogContext context, IAwaitable<IMessageActivity> result)
        {
            var message = await result;
            int userID = context.UserData.GetValue<UserProfile>("UserInfo").UserID;
            InterviewBotEntities dbcntx = new InterviewBotEntities();
            UserJobMapping userappiontments = new UserJobMapping()
            {
                UserID = userID,
                JobListings = Convert.ToInt64(message.Text),
                InterviewStatus ="Active",
                InterviewScheduledDate =null,
                PointOfContact = ""
            };
            dbcntx.UserJobMappings.Add(userappiontments);
            dbcntx.SaveChanges();
            await context.PostAsync("succesfully applied for job,please type something ..to start  ");
            context.Done(message);
            /*
            string result = "";
            string FromEmail = "recruiter_email";
            string ToEmail = "InterviewerMailadress";
            string json = "{\"organizer\":{\"name\":\"recruitername\",\"emailAddress\":\"" + FromEmail + "\"}, \"attendees\": [{\"name\": \"toEmailFirstName\",\"" + ToEmail + "\":\"\" }], \"subject\":\"Regarding requirements gathering " + DateTime.Now.ToLongTimeString() + "\",\"utterance\":\"NOV30 3PM\" }";
            using (var client = new WebClient())
            {
                client.Headers[HttpRequestHeader.ContentType] = "application/json";
                result = client.UploadString("http://moneypenny-prod-api.azurewebsites.net/v1.0/requests", "POST", json);
            }
            */

        }

        private void uploadToBlob(string ContainerName, byte[] data, string BlobName)
        {
            // Parse the connection string and return a reference to the storage account.
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(CloudConfigurationManager.GetSetting("StorageConnectionString"));
            // creating blob client
            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
            CloudBlobContainer container = blobClient.GetContainerReference(ContainerName);
            // Create the container if it doesn't already exist.
            container.CreateIfNotExists();
            //setting permissions 
            container.SetPermissions(new BlobContainerPermissions { PublicAccess = BlobContainerPublicAccessType.Blob });

            // Retrieve reference to a blob named BlobName.
            BlobName += ".pdf";
            CloudBlockBlob blockBlob = container.GetBlockBlobReference(BlobName);
            using (var stream = new MemoryStream(data, writable: false))
            {
                blockBlob.UploadFromStream(stream);
            }
            
        }

        private static SearchServiceClient CreateSearchServiceClient()
        {
            string searchServiceName = CloudConfigurationManager.GetSetting("SearchServiceName");// configuration["SearchServiceName"];
            string adminApiKey = CloudConfigurationManager.GetSetting("SearchServiceAdminApiKey"); //configuration["SearchServiceAdminApiKey"];

            SearchServiceClient serviceClient = new SearchServiceClient(searchServiceName, new SearchCredentials(adminApiKey));
            return serviceClient;
        }

        private List<Attachment> getHerocard(IDialogContext cntxt,string type)
        {
            SearchServiceClient searchServiceClient = CreateSearchServiceClient();
            ISearchIndexClient searchIndexClient = searchServiceClient.Indexes.GetClient(contentIndex);
            string userID = cntxt.UserData.GetValue<UserProfile>("UserInfo").UserID.ToString();

            InterviewBotEntities dbcntxt = new InterviewBotEntities();

            List<JobListing> Profiles = new List<JobListing>();
            List<Attachment> attachements = new List<Attachment>();
            UserProfile userDetails = new UserProfile();
            List<UserJobMapping> userJobMappings = new List<UserJobMapping>();
            cntxt.UserData.TryGetValue<UserProfile>("UserInfo",out userDetails);
            cntxt.UserData.TryGetValue<List<UserJobMapping>>("UserJobMappings", out userJobMappings);
           

            if (type.ToLower()=="cancel" && userJobMappings.Count > 0)
            {
                foreach (UserJobMapping job in userJobMappings)
                {
                    var profile = (from db in dbcntxt.JobListings where db.JobID == job.JobListings select db).FirstOrDefault();
                    Profiles.Add(profile);
                }
                if (Profiles != null)
                {
                    foreach (JobListing profile in Profiles)
                    {

                        #region cardActions
                        CardAction cardaction1 = new CardAction()
                        {
                            Title = "JobID " + profile.JobID,
                            Value = profile.JobID,
                            Type = "imBack"
                        };

                        #endregion

                        HeroCard herocd = new HeroCard();
                        herocd.Title = "Click the below JobId button to cancel your apppointment";
                        herocd.Text = $"Standard Tilte is {profile.StandardTitle}, {profile.EmploymentType} employement must have minimum of {profile.Experience} and CTC is {profile.CostToCompany} ";
                        herocd.Buttons = new List<CardAction> { cardaction1 };
                        attachements.Add(herocd.ToAttachment());
                    }
                }
                else
                {
                    return null;
                }
            }
            else
            {
                ApplyForm getdeatils = new ApplyForm();
                cntxt.UserData.TryGetValue("FormDeatils", out getdeatils);
                Profiles = (from profile in dbcntxt.JobListings where profile.JobCategory == getdeatils.JobCategory.ToString() select profile).Distinct().ToList();
                if (Profiles != null)
                {
                    bool flag = false;
                    foreach (JobListing profile in Profiles) //test for all applicable profiles
                    {
                        var searchText = profile.JobSummary; //search using jobsummary text
                        DocumentSearchResult results;
                        results = searchIndexClient.Documents.Search(searchText);//, parameters);
                        foreach (SearchResult result in results.Results) //iterate over all matching resumes
                        {
                            if(result.Document["metadata_storage_name"].ToString().Contains(userID) && (result.Score*100) > SCORE_THRESHOLD) //if current user's resume is present and score is ok
                            {
                                flag = true;
                                break;
                                //give this job as option
                            }
                        }
                        if (flag)
                        {
                            CardAction cardaction1 = new CardAction()
                            {
                                Title = "Apply Job",
                                Value = profile.JobID,
                                Type = ActionTypes.ImBack
                            };

                            HeroCard herocd = new HeroCard();
                            herocd.Text = $"Standard Tilte is {profile.StandardTitle}, {profile.EmploymentType} employement must have minimum of {profile.Experience} and CTC is {profile.CostToCompany} for more deatils click on Job Details button ";
                            herocd.Buttons = new List<CardAction> { cardaction1 }; //, cardaction2 };
                            attachements.Add(herocd.ToAttachment());
                        }
                    }
                }
                else
                {
                    return null;
                }
            }
            return attachements;
        }
    }
}