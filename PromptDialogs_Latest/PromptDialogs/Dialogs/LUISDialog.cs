using Microsoft.Azure;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.FormFlow;
using Microsoft.Bot.Builder.Luis;
using Microsoft.Bot.Builder.Luis.Models;
using Microsoft.Bot.Connector;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using PromptDialogs.DBModels;
using PromptDialogs.Dialogs;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Microsoft.Azure.Search;
using Microsoft.Azure.Search.Models;
using System.Text.RegularExpressions;
using System.Net;

namespace PromptDialogs.Dialogs
{
    [LuisModel("5c72b4c3-f8e5-477b-987f-94696a0173b0", "8446cff3f53c49edba480da797a423e4")]
    [Serializable]
    public class LUISDialog : LuisDialog<BuildUserProfile>
    {
        //private readonly BuildFormDelegate<UserJobMapping> userJobMapping;
        private readonly BuildFormDelegate<BuildUserProfile> buildUserProfile;
        private UserProfile userinfo;
        private static int EmailCount = 0;
        private static string contentIndex = "azureblob";
        private double SCORE_THRESHOLD = 1.0;

        //RecruitBOTEntities DbContext = new RecruitBOTEntities();
        public LUISDialog(BuildFormDelegate<BuildUserProfile> buildUserProfile)
        {
            //this.userJobMapping = userJobMapping;
            this.buildUserProfile = buildUserProfile;
            //DbContext = new RecruitBOTEntities();
            userinfo = new UserProfile();
        }


        [LuisIntent("")]
        public async Task None(IDialogContext context, LuisResult result)
        {
            await context.PostAsync("I'm sorry I don't know what you mean.");
            context.Wait(MessageReceived);
        }

        [LuisIntent("Greetings")]
        public async Task Greeting(IDialogContext context, LuisResult result)
        {
            //await context.PostAsync("Welcome to Interview Bot");
            //context.Wait(MessageReceivedAsync);
            //context.Call<StartForm>(startFormInstance, afterResume);
            //context.Call(MessageReceivedAsync);

            //var faqDialog = new LUISDialog(buildUserProfile);
            //var messageToForward = result;
            ////await context.Forward(faqDialog, AfterFAQDialog, messageToForward, CancellationToken.None);
            //context.Call<BuildUserProfile>(MessageReceivedAsync, routeDialog);

            try
            {
                //var activity = await result;
                RecruitBOTEntities DbContext = new RecruitBOTEntities();

                var userinfo = (from s1 in DbContext.UserProfiles where s1.ChannelId == context.Activity.ChannelId && s1.UserChannelId == context.Activity.From.Id select s1).FirstOrDefault();

                if (userinfo != null)
                {
                    // User already Registered with this channel 
                    string[] display;
                    context.UserData.SetValue<UserProfile>("UserInfo", userinfo);
                    List<UserJobMapping> userjobmappings = new List<UserJobMapping>();
                    var userIds = (from usrprfls in DbContext.UserProfiles where usrprfls.EmailID == userinfo.EmailID select usrprfls.UserID).ToList();
                    List<UserJobMapping> appointments = new List<UserJobMapping>();
                    foreach (int id in userIds)
                    {
                        var Userappointments = (from jobmappings in DbContext.UserJobMappings where jobmappings.UserID == id && jobmappings.InterviewStatus == "active" select jobmappings).ToList();
                        appointments.AddRange(Userappointments);
                    }
                    if (appointments.Count > 0)
                    {
                        //saving UserAppointments to BOT STATE
                        context.UserData.SetValue<List<UserJobMapping>>("UserJobMappings", appointments);
                        display = new string[4] { "Apply for New Appointment", "Cancel Appointment", "RescheduleAppointment", "Profile Update" };

                    }
                    else
                    {
                        display = new string[2] { "Apply for New Appointment", "Profile Update" };
                    }
                    var newdialog = new PromptDialog.PromptChoice<string>(display, "Please select below Options?", "Sorry, that wans't a valid option", 3);
                    context.Call(newdialog, routeDialog);

                }
                else
                {
                    //User is not registered via this channel 
                    //Check Recurring User 
                    string[] options = new string[] { "YES", "NO" };

                    var recurringUser = new PromptDialog.PromptConfirm("Are you an existing User? ", "we didn't find selected option", 3, PromptStyle.Auto, options);
                    context.Call(recurringUser, CheckForRecurringUser);

                }


            }

            catch (Exception ex)
            {

            }
        }

        private async Task CheckForRecurringUser(IDialogContext context, IAwaitable<bool> result)
        {
            bool message = await result;
            if (message)
            {
                await context.PostAsync("Please provide your registred Email ID ");
                context.Wait(askForEmailID);
            }
            else
            {
                //asking new user to complete profile
                await context.PostAsync("Welcome New User,Update your Profile Details");
                var NewProfileBuilder = new FormDialog<BuildUserProfile>(new BuildUserProfile(), BuildUserProfile.BuildForm, FormOptions.PromptInStart);
                context.Call(NewProfileBuilder, SaveProfileToDB);
            }
        }


        private async Task askForEmailID(IDialogContext context, IAwaitable<IMessageActivity> result)
        {
            var message = await result;
            if (ValidateEmail(message.Text))
            {
                // check Email is registered or not
                RecruitBOTEntities DbContext = new RecruitBOTEntities();
                var userInfo = (from x1 in DbContext.UserProfiles where x1.EmailID == message.Text select x1).FirstOrDefault();
                if (userInfo != null)
                {
                    DbContext.UserProfiles.Add(new UserProfile()
                    {
                        UserChannelId = context.Activity.From.Id,
                        ChannelId = context.Activity.ChannelId,
                        UserName = context.Activity.From.Name,
                        FirstName = userInfo.FirstName,
                        LastName = userInfo.LastName,
                        MiddleName = userInfo.MiddleName,
                        EmailID = userInfo.EmailID,
                        PhoneNumber = userInfo.PhoneNumber,
                        Degree = userInfo.Degree,
                        CGPA = userInfo.CGPA,
                        Experience = userInfo.Experience,
                        ExpertiseInTechnology = userInfo.ExpertiseInTechnology,
                        CurrentCompany = userInfo.CurrentCompany,

                    });
                    DbContext.SaveChanges();
                    await context.PostAsync("Welcome " + userInfo.FirstName + " " + userInfo.LastName);
                    context.Wait(MessageReceivedAsync);
                }
                else
                {
                    EmailCount++;
                    if (EmailCount < 3)
                    {
                        await context.PostAsync("We didn't find any registered account with this email, please try again providing correct Email ?");
                        context.Wait(askForEmailID);
                    }
                    else
                    {
                        await context.PostAsync("exceed the limit, please start from first");
                        context.Done(this);
                    }

                }

            }
            else
            {
                if (EmailCount < 2)
                {
                    EmailCount++;
                    await context.PostAsync("Please enter Valid Email Address");
                    context.Wait(askForEmailID);
                }
                else
                {
                    await context.PostAsync("Exceed the Limit, please try again");
                    context.Done(this);
                }

            }
        }

        private bool ValidateEmail(string email)
        {
            Regex regex = new Regex(@"^([\w\.\-]+)@([\w\-]+)((\.(\w){2,3})+)$");
            Match match = regex.Match(email);
            return match.Success;
        }

        //IDialogContext context, IAwaitable<IMessageActivity> item
        private async Task MessageReceivedAsync(IDialogContext context, IAwaitable<IMessageActivity> result)
        {
            //var activity = await result;
            //Query to DB for USER PRESENCE
            RecruitBOTEntities DbContext = new RecruitBOTEntities();
            var userinfo = (from usrs in DbContext.UserProfiles where usrs.UserChannelId == context.Activity.From.Id && usrs.ChannelId == context.Activity.ChannelId select usrs).FirstOrDefault();

            if (userinfo == null)
            {
                await context.PostAsync("Welcome New User,Update your Profile Details");
                var NewProfileBuilder = new FormDialog<BuildUserProfile>(new BuildUserProfile(), BuildUserProfile.BuildForm, FormOptions.PromptInStart);
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

                if (appointments.Count > 0)
                {
                    //saving UserAppointments to BOT STATE
                    context.UserData.SetValue<List<UserJobMapping>>("UserJobMappings", appointments);
                    display = new string[4] { "Apply for New Appointment", "Cancel Appointment", "RescheduleAppointment", "Profile Update" };

                }
                else
                {
                    display = new string[2] { "Apply for New Appointment", "Profile Update" };
                }
                var newdialog = new PromptDialog.PromptChoice<string>(display, "Please select below Options?", "Sorry, that wans't a valid option", 3);
                context.Call(newdialog, routeDialog);
            }
        }

        private async Task routeDialog(IDialogContext context, IAwaitable<string> result)
        {
            var message = await result;
            if (message == "Apply for New Appointment")
            {
                var StartFrm = new FormDialog<ApplyForm>(new ApplyForm(), ApplyForm.BuildForm, FormOptions.PromptInStart, null);
                context.Call<ApplyForm>(StartFrm, askForResume);
            }
            else if (message == "Cancel Appointment")
            {
                // getting user Appointments 
                List<Attachment> attachements = getHerocard(context, "cancel");
                if (attachements.Count > 0)
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
            else if (message == "RescheduleAppointment")
            {
                // getting user Appointments 
                List<Attachment> attachements = getHerocard(context, "cancel");
                if (attachements.Count > 0)
                {
                    await context.PostAsync("Please select job ID which you want reschedule");
                    var replyMessage = context.MakeMessage();
                    replyMessage.AttachmentLayout = AttachmentLayoutTypes.Carousel;
                    replyMessage.Attachments = attachements;
                    await context.PostAsync(replyMessage);
                    context.Wait(rescheduleAppointment);
                }
                else
                {
                    await context.PostAsync($"Sorry No active jobs found");
                    context.Done(this);
                }
            }
            else
            {
                //default case for Profile Building

            }
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
                uploadToBlob("resume", ResumeData, userID,false);
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

        private void uploadToBlob(string ContainerName, byte[] data, string BlobName,bool isProfile)
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
            if (!isProfile)
                BlobName += ".pdf";

            CloudBlockBlob blockBlob = container.GetBlockBlobReference(BlobName);
            using (var stream = new MemoryStream(data, writable: false))
            {
                blockBlob.UploadFromStream(stream);
            }

        }

        private string  GetBlobData(string Blobname)
        {
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(CloudConfigurationManager.GetSetting("StorageConnectionString"));
            // creating blob client
            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
            CloudBlobContainer container = blobClient.GetContainerReference("profiles");
            container.CreateIfNotExists();
            //setting permissions 
            container.SetPermissions(new BlobContainerPermissions { PublicAccess = BlobContainerPublicAccessType.Blob });
            // Retrieve reference to a blob named BlobName.
            // Retrieve reference to a blob named BlobName.
           
            CloudBlockBlob blockBlob = container.GetBlockBlobReference(Blobname);
            return blockBlob.Uri.AbsoluteUri;
        }


        private static SearchServiceClient CreateSearchServiceClient()
        {
            string searchServiceName = CloudConfigurationManager.GetSetting("SearchServiceName");// configuration["SearchServiceName"];
            string adminApiKey = CloudConfigurationManager.GetSetting("SearchServiceAdminApiKey"); //configuration["SearchServiceAdminApiKey"];

            SearchServiceClient serviceClient = new SearchServiceClient(searchServiceName, new SearchCredentials(adminApiKey));
            return serviceClient;
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
                MiddleName = message.LastName,
                LastName = message.LastName,
                EmailID = message.Email,
                PhoneNumber = message.PhoneNo,
                Degree = message.HighestDegree.ToString(),
                CGPA = message.CGPA,
                ExpertiseInTechnology = message.ExpertiseInTechnology,
                CurrentCompany = message.CurrentCompany,
                Experience = message.TechnologyExpereince,

            };

            RecruitBOTEntities dbcontext = new RecruitBOTEntities();
            dbcontext.UserProfiles.Add(userProfile);
            dbcontext.SaveChanges();
            await context.PostAsync("Profile Updated Successfully. Please type something to start applying for Jobs");
            context.Wait(MessageReceivedAsync);
        }

        private async Task cancelAppointment(IDialogContext context, IAwaitable<IMessageActivity> result)
        {
            var message = await result;
            long id = Convert.ToInt64(message.Text);
            RecruitBOTEntities dbcntx = new RecruitBOTEntities();
            var output = (from db in dbcntx.UserJobMappings where db.JobListings == id select db).FirstOrDefault();
            if (output != null)
            {
                output.InterviewStatus = "Inactive";
                dbcntx.SaveChanges();
                await context.PostAsync("Succefully cancelled your appointement");
                //context.Done(message);
                context.Wait(MessageReceivedAsync);
            }
            else
            {
                await context.PostAsync("please provide valid Job refernce or click on button you want to cancel");
                context.Wait(cancelAppointment);
            }
        }

        private async Task saveUserJobDeatils(IDialogContext context, IAwaitable<IMessageActivity> result)
        {
            var message = await result;
            int userID = context.UserData.GetValue<UserProfile>("UserInfo").UserID;
            RecruitBOTEntities dbcntx = new RecruitBOTEntities();
            DateTime current_date = DateTime.Now;
            current_date = current_date.AddDays(7);
            //RecruitBOTEntities dbcntx = new RecruitBOTEntities();
            UserJobMapping userappiontments = new UserJobMapping()
            {
                UserID = userID,
                JobListings = Convert.ToInt64(message.Text),
                InterviewStatus = "Active",
                InterviewScheduledDate = current_date.Date,
                PointOfContact = ""
            };
            dbcntx.UserJobMappings.Add(userappiontments);
            dbcntx.SaveChanges();
            await context.PostAsync("succesfully applied for job!");
            //context.Wait(MessageReceivedAsync);
            //context.Done(this);

            string response = "";
            //string FromEmail = "Mahesh.Nagaiah@microsoft.com";
            //string ToEmail = "absrin@microsoft.com";
            string FromEmail = "prkakani@microsoft.com";
            //string ToEmail = context.UserData.GetValue<UserProfile>("UserInfo").EmailID;

            Random obj = new Random();
            var obj1 = dbcntx.InterviewPanels.Find(obj.Next(1, 3));
            string ToEmail = obj1.EmailID;
            string candidateFirstName = context.UserData.GetValue<UserProfile>("UserInfo").FirstName;
            string candidateLastName = context.UserData.GetValue<UserProfile>("UserInfo").LastName;
            string candidateName = candidateFirstName + " " + candidateLastName;
            string interviewFName = obj1.FirstName;
            string interviewLName = obj1.LastName;
            string interviewerName = interviewFName + interviewLName;
            string month = current_date.ToString("MMM").ToUpper();
            int day = current_date.Day;
            string scheduledDate = day + month;
            string json = "{\"organizer\":{\"name\":\"recruitername\",\"emailAddress\":\"" + FromEmail + "\"}, \"attendees\": [{\"name\": \"" + interviewerName + "\",\"emailAddress\":\"" + ToEmail + "\" }], \"subject\":\"Regarding scheduling of interview for candidate " + candidateName + "\",\"utterance\":\"" + scheduledDate + " 3PM\" }";

            try
            {
                using (var client = new WebClient())
                {
                    client.Headers[HttpRequestHeader.ContentType] = "application/json";
                    response = client.UploadString("http://moneypenny-prod-api.azurewebsites.net/v1.0/requests", "POST", json);
                }
            }
            catch (Exception)
            {

                throw;
            }


            await context.PostAsync("Meeting has been scheduled. You shall receive an invite on your registered email.");
            context.Wait(MessageReceivedAsync);
            //context.Done(new object());
        }

        private async Task rescheduleAppointment(IDialogContext context, IAwaitable<IMessageActivity> result)
        {
            var message = await result;
            int userID = context.UserData.GetValue<UserProfile>("UserInfo").UserID;
            RecruitBOTEntities dbcntx = new RecruitBOTEntities();
            DateTime current_date = DateTime.Now;
            current_date = current_date.AddDays(7);
            var userAppointment = (from db in dbcntx.UserJobMappings where db.UserID == userID select db).FirstOrDefault();
            userAppointment.InterviewScheduledDate = current_date.Date;
            dbcntx.SaveChanges();
            //await context.PostAsync("succesfully rescheduled interview!");
            //context.Wait(MessageReceivedAsync);
            //context.Done(this);

            string response = "";
            //string FromEmail = "Mahesh.Nagaiah@microsoft.com";
            //string ToEmail = "absrin@microsoft.com";
            string FromEmail = "prkakani@microsoft.com";
            //string ToEmail = context.UserData.GetValue<UserProfile>("UserInfo").EmailID;

            Random obj = new Random();
            var obj1 = dbcntx.InterviewPanels.Find(obj.Next(1, 3));
            string ToEmail = obj1.EmailID;
            string candidateFirstName = context.UserData.GetValue<UserProfile>("UserInfo").FirstName;
            string candidateLastName = context.UserData.GetValue<UserProfile>("UserInfo").LastName;
            string candidateName = candidateFirstName + " " + candidateLastName;
            string interviewFName = obj1.FirstName;
            string interviewLName = obj1.LastName;
            string interviewerName = interviewFName + interviewLName;
            string month = current_date.ToString("MMM").ToUpper();
            int day = current_date.Day;
            string scheduledDate = day + month;
            string json = "{\"organizer\":{\"name\":\"recruitername\",\"emailAddress\":\"" + FromEmail + "\"}, \"attendees\": [{\"name\": \"" + interviewerName + "\",\"emailAddress\":\"" + ToEmail + "\" }], \"subject\":\"Regarding scheduling of interview for candidate " + candidateName + "\",\"utterance\":\"" + scheduledDate + " 3PM\" }";

            try
            {
                using (var client = new WebClient())
                {
                    client.Headers[HttpRequestHeader.ContentType] = "application/json";
                    response = client.UploadString("http://moneypenny-prod-api.azurewebsites.net/v1.0/requests", "POST", json);
                }
            }
            catch (Exception)
            {

                throw;
            }
            await context.PostAsync("Meeting has been rescheduled. You shall receive a new invite on your registered email.");
            context.Wait(MessageReceivedAsync);
        }

        private List<Attachment> getHerocard(IDialogContext cntxt, string type)
        {
            SearchServiceClient searchServiceClient = CreateSearchServiceClient();
            ISearchIndexClient searchIndexClient = searchServiceClient.Indexes.GetClient(contentIndex);
            string userID = cntxt.UserData.GetValue<UserProfile>("UserInfo").UserID.ToString();

            RecruitBOTEntities dbcntxt = new RecruitBOTEntities();
            #region RetrieveJobInfoFromDB
            List<JobListing> Profiles = new List<JobListing>();
            List<Attachment> attachements = new List<Attachment>();
            UserProfile userDetails = new UserProfile();
            List<UserJobMapping> userJobMappings = new List<UserJobMapping>();
            cntxt.UserData.TryGetValue<UserProfile>("UserInfo", out userDetails);
            cntxt.UserData.TryGetValue<List<UserJobMapping>>("UserJobMappings", out userJobMappings);


            if (type.ToLower() == "cancel" && userJobMappings.Count > 0)
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
                            Value = profile.JobID.ToString(),
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

                ApplyForm getdetails = new ApplyForm();
                cntxt.UserData.TryGetValue("FormDeatils", out getdetails);
                Profiles = (from profile in dbcntxt.JobListings where profile.JobCategory == getdetails.JobCategory.ToString() select profile).Distinct().ToList();
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
                            if (result.Document["metadata_storage_name"].ToString().Contains(userID) && (result.Score * 100) > SCORE_THRESHOLD) //if current user's resume is present and score is ok
                            {
                                flag = true;
                                break;
                                //give this job as option
                            }
                        }
                        if (flag)
                        {

                            byte[] toBytes = System.Text.Encoding.ASCII.GetBytes(profile.JobSummary);

                            string filname = profile.JobID.ToString() + ".txt";
                            uploadToBlob("profiles", toBytes, filname, true);
                            string Filpath = GetBlobData(filname);



                            #region cardActions
                            CardAction cardaction1 = new CardAction()
                            {
                                Title = "Apply Job",
                                Value = profile.JobID.ToString(),
                                Type = "imBack"
                            };

                            CardAction cardaction2 = new CardAction()
                            {
                                Title = profile.JobID + " Job Detail",
                                Value = Filpath,
                                Type = "openUrl", //  downloadFile
                            };
                            #endregion

                            HeroCard herocd = new HeroCard();
                            herocd.Text = $"Standard Tilte is {profile.StandardTitle}, {profile.EmploymentType} employement must have minimum of {profile.Experience} and CTC is {profile.CostToCompany} for more deatils click on Job Details button ";
                            herocd.Buttons = new List<CardAction> { cardaction1, cardaction2 };
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
            #endregion
        }


        //private async Task afterResume(IDialogContext context, IAwaitable<BuildUserProfile> result)
        //{
        //    var message = await result;
        //    if (message.InterviewOptns.ToString() == "ApplyNew")
        //    {
        //        var NewJobForm = new FormDialog<NewJob>(new NewJob(), NewJob.BuildForm, FormOptions.PromptInStart, null);
        //        context.Call<NewJob>(NewJobForm, ProcessJobInfo);
        //    }
        //}

        //private async Task ProcessJobInfo(IDialogContext context, IAwaitable<NewJob> result)
        //{
        //    var message = await result;
        //    await context.PostAsync("Thanks for choosing Interview Bot. We are procesing your Interview");
        //    //processing Interview Schedule
        //    context.Done(message);
        //}

        //private async Task Callback(IDialogContext context, IAwaitable<object> result)
        //{
        //    context.Wait(MessageReceived);
        //    var message = await result;
        //    await context.PostAsync("Thanks for choosing Interview Bot. We are procesing your Interview");
        //    //processing Interview Schedule
        //    context.Done(message);
        //}

        ////[LuisIntent("Query Interview Options")]
        ////public async Task QueryInterviewOptions(IDialogContext context, LuisResult result)
        ////{
        ////    //var message = await result;
        ////    //if (message.InterviewOptns.ToString() == "ApplyNew")
        ////    //{
        ////        var NewJobForm = new FormDialog<NewJob>(new NewJob(), NewJob.BuildForm, FormOptions.PromptInStart, null);
        ////        context.Call<NewJob>(NewJobForm, ProcessJobInfo);
        ////    //}
        ////}


        [LuisIntent("Apply New")]
        public async Task ApplyNew(IDialogContext context, LuisResult result)
        {
            var StartFrm = new FormDialog<ApplyForm>(new ApplyForm(), ApplyForm.BuildForm, FormOptions.PromptInStart, null);
            context.Call<ApplyForm>(StartFrm, askForResume);
        }

        [LuisIntent("Cancel Interview ")]
        public async Task CancelInterview(IDialogContext context, LuisResult result)
        {
            // getting user Appointments 
            List<Attachment> attachements = getHerocard(context, "cancel");
            if (attachements.Count > 0)
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

        [LuisIntent("Query Job Category")]
        public async Task QueryJobCategory(IDialogContext context, LuisResult result)
        {
            foreach (var entity in result.Entities.Where(Entity => Entity.Type == "Job Category"))
            {
                var value = entity.Entity.ToLower();

                if (Enum.GetNames(typeof(TypeOfJob)).Contains(Enum.Parse(typeof(TypeOfJob), value, true).ToString()))
                {
                    await context.PostAsync("Yes we have that!");
                    context.Wait(MessageReceived);
                    return;
                }
                else
                {
                    await context.PostAsync("I'm sorry we don't have that.");
                    context.Wait(MessageReceived);
                    return;
                }
            }
            await context.PostAsync("I'm sorry we don't have that.");
            context.Wait(MessageReceived);
            return;
        }

        [LuisIntent("Query Technology")]
        public async Task QueryTechnology(IDialogContext context, LuisResult result)
        {
            foreach (var entity in result.Entities.Where(Entity => Entity.Type == "Technology"))
            {
                var value = entity.Entity.ToLower();

                //List<string> list = System.Enum.GetValues(typeof(technologies))
                //.Cast<string>()
                //.ToList<string>();

                // if (list.Contains(value))
                //var tech = Enum.Parse(typeof(technologies), value, true);
                if (Enum.GetNames(typeof(technologies)).Contains(Enum.Parse(typeof(technologies), value, true).ToString()))
                {
                    await context.PostAsync("Yes we have that!");
                    context.Wait(MessageReceived);
                    return;
                }
                else
                {
                    await context.PostAsync("I'm sorry we don't have that.");
                    context.Wait(MessageReceived);
                    return;
                }

                //if (value == "dotNET" || value == "JAVA" || value == "SharePoint" || value == "MVC" || value == "AngularJS" || value == "SFB")
                //{
                //    await context.PostAsync("Yes we have that!");
                //    context.Wait(MessageReceived);
                //    return;
                //}
                //else
                //{
                //    await context.PostAsync("I'm sorry we don't have that.");
                //    context.Wait(MessageReceived);
                //    return;
                //}
            }
            await context.PostAsync("I'm sorry we don't have that.");
            context.Wait(MessageReceived);
            return;
        }

     

    }
}