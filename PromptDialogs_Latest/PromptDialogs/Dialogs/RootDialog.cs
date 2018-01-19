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
using System.Text.RegularExpressions;

namespace PromptDialogs.Dialogs
{
    [Serializable]
    public class RootDialog : IDialog<IMessageActivity>
    {
        private static int EmailCount = 0;
        public Task StartAsync(IDialogContext context)
        {
            context.Wait(MessageReceivedAsync);

            return Task.CompletedTask;
        }

        private async Task MessageReceivedAsync(IDialogContext context, IAwaitable<IMessageActivity> result)
        {
            var activity = await result;
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
                    display = new string[4] { "Apply for New Appointment", "Cancel Appointment", "ReschuleAppointment", "Profile Update" };

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
                //CGPA = message.CGPA,
                ExpertiseInTechnology = message.ExpertiseInTechnology,
                CurrentCompany = message.CurrentCompany,
                Experience = message.TechnologyExpereince,

            };
            context.UserData.SetValue<UserProfile>("UserInfo", userProfile);
            RecruitBOTEntities dbcontext = new RecruitBOTEntities();
            dbcontext.UserProfiles.Add(userProfile);
            dbcontext.SaveChanges();
            await context.PostAsync("Profile Updated Successfully, go ahead and apply for JOBS");
            context.Wait(MessageReceivedAsync);
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
            var message = await result;
            long id = Convert.ToInt64(message.Text);
            RecruitBOTEntities dbcntx = new RecruitBOTEntities();
            var output = (from db in dbcntx.UserJobMappings where db.JobListings == id select db).FirstOrDefault();
            if (output != null)
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
            RecruitBOTEntities dbcntx = new RecruitBOTEntities();
            UserJobMapping userappiontments = new UserJobMapping()
            {
                UserID = userID,
                JobListings = Convert.ToInt64(message.Text),
                InterviewStatus = "Active",
                InterviewScheduledDate = null,
                PointOfContact = ""
            };
            dbcntx.UserJobMappings.Add(userappiontments);
            dbcntx.SaveChanges();
            await context.PostAsync("succesfully applied for job,please type something ..to start  ");
            context.Done(message);


            string response = "";
            //string FromEmail = "Mahesh.Nagaiah@microsoft.com";
            //string ToEmail = "absrin@microsoft.com";
            DateTime current_date = DateTime.Now;
            current_date.AddDays(7);
            string FromEmail = "prkakani@microsoft.com";
            string ToEmail = "alijain@microsoft.com";
            string json = "{\"organizer\":{\"name\":\"recruitername\",\"emailAddress\":\"" + FromEmail + "\"}, \"attendees\": [{\"name\": \"toEmailFirstName\",\"emailAddress\":\"" + ToEmail + "\" }], \"subject\":\"Regarding requirements gathering " + DateTime.Now.ToLongTimeString() + "\",\"utterance\":\"JAN30 3PM\" }";


            using (var client = new WebClient())
            {
                client.Headers[HttpRequestHeader.ContentType] = "application/json";
                response = client.UploadString("http://moneypenny-prod-api.azurewebsites.net/v1.0/requests", "POST", json);
            }


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
            CloudBlockBlob blockBlob = container.GetBlockBlobReference(BlobName);
            using (var stream = new MemoryStream(data, writable: false))
            {
                blockBlob.UploadFromStream(stream);
            }

        }

      
        private List<Attachment> getHerocard(IDialogContext cntxt, string type)
        {
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
                foreach (JobListing prof in Profiles)
                {
                    if (userJobMappings.Any(s1 => s1.JobListings == prof.JobListings))
                    {
                        Profiles.Remove(prof);
                    }
                }

                if (Profiles != null)
                {

                    foreach (JobListing profile in Profiles)
                    {
                        #region ByteConverstion    
                        string dirPath = @"C:\temp\";
                        string Filpath = dirPath + string.Format(@"{0}.txt", DateTime.Now.Ticks);
                        FileInfo fi = new FileInfo(Filpath);
                        DirectoryInfo di = new DirectoryInfo(dirPath);
                        // Byte[] bytData = (byte[]) profile.JobSummary;

                        if (!di.Exists)
                        {
                            di.Create();
                        }

                        if (!fi.Exists)
                        {
                            fi.Create().Dispose();
                        }

                        TextWriter txt = new StreamWriter(Filpath);
                        txt.Write(profile.JobSummary);
                        txt.Close();
                        //FileStream fs = new System.IO.FileStream(Filpath, FileMode.OpenOrCreate, FileAccess.Write);
                        //StreamWriter br = new StreamWriter(Filpath);
                        //br.Write(profile.JobSummary);
                        //fs.Dispose();

                        #endregion     // converst byte[] format to Char 

                        #region cardActions
                        CardAction cardaction1 = new CardAction()
                        {
                            Title = "Apply Job",
                            Value = profile.JobID,
                            Type = ActionTypes.ImBack
                        };

                        CardAction cardaction2 = new CardAction()
                        {
                            Title = profile.JobID + " Job Detail",
                            Value = Filpath,
                            Type = "downloadFile", // openUrl
                        };
                        #endregion

                        HeroCard herocd = new HeroCard();
                        herocd.Text = $"Standard Tilte is {profile.StandardTitle}, {profile.EmploymentType} employement must have minimum of {profile.Experience} and CTC is {profile.CostToCompany} for more deatils click on Job Details button ";
                        herocd.Buttons = new List<CardAction> { cardaction1, cardaction2 };
                        attachements.Add(herocd.ToAttachment());

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
    }

}