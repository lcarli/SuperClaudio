using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Description;
using Microsoft.Bot.Connector;
using Newtonsoft.Json;
using System.IO;
using System.Web;
using System.Collections.Generic;
using Microsoft.ApplicationInsights;
using System.Diagnostics;
using Microsoft.WindowsAzure.Storage;
using Microsoft.Azure;
using Microsoft.WindowsAzure.Storage.Blob;

namespace superbotcloudV3
{
    [BotAuthentication]
    public class MessagesController : ApiController
    {
        #region Global Variables
        Random random = new Random();
        TelemetryClient telemetry = new TelemetryClient();
        bool interactive = false;
        string AWSEntity;
        #endregion


        /// <summary>
        /// POST: api/Messages
        /// Receive a message from a user and reply to it
        /// </summary>
        public async Task<HttpResponseMessage> Post([FromBody]Activity activity)
        {
            if (activity.Type == ActivityTypes.Message)
            {
                ConnectorClient connector = new ConnectorClient(new Uri(activity.ServiceUrl));
                Activity reply;

                //sending data to Telemetry
                SendDataTelemetryToTableStorage(activity.From.Name, activity.ChannelId.ToString());

                //test
                //if (activity.Text == "links") 
                //{
                //    await connector.Conversations.ReplyToActivityAsync(reply = activity.CreateReply(GetLinks("Windows|Create").First()));
                //}

                if (Contain("badwords", activity.Text))
                {
                    reply = activity.CreateReply(Random("badwordsanswers"));
                }
                else if (activity.Text.ToLower().Contains("yesaws"))
                {
                    var r = "";
                    AWSEntity = activity.Text.Replace("yesaws ", "");
                    if (overview.TryGetValue(AWSEntity, out r))
                    {
                        reply = activity.CreateReply(Random("standard") + r);
                    }
                    else
                    {
                        AWSEntity = "";
                        reply = activity.CreateReply(Random("missunderstood"));
                    }
                }
                else if (activity.Text == "noaws")
                {
                    AWSEntity = "";
                    reply = activity.CreateReply("Ok. NÃO Vou enviar!");
                }
                else if (Contain("greetings", activity.Text))
                {
                    reply = activity.CreateReply("Olá! Eu sou o Super Claudio. Estou aqui para lhe guiar na nuvem. Vamos começar? Também posso traduzir algum serviço da aws para o Azure, caso deseje.");
                }
                else if (Contain("goodbyes", activity.Text))
                {
                    reply = activity.CreateReply(Random("goodbyesanswers"));
                }
                else if (activity.Text.ToLower().Contains("arquitetura") && activity.Text.ToLower().Contains("bot"))
                {
                    string link = "https://peulfg.by3302.livefilestore.com/y3pYLFDIG22tRzAYUNS--dM4ZIWBpNIYu8vfeeSQ3Fhzl_OjlE7OsrRzX2_J_MSE7R6iRlzjMKOgV2IYyfE6mLkcHaEIfjZO1jwK4PfAh75m7SccSTqlqswQ_ZgGWmox2NWr29XqXkMZwFV9QwRjKn3wL0tJZTGc4HBtgyRuSeZA1c/infra.PNG?psid=1";
                    reply  = activity.CreateReply("Esta é a minha arquitetura");
                    reply.Recipient = activity.From;
                    reply.Type = "message";
                    reply.Attachments = new List<Attachment>();
                    List<CardImage> cardImages = new List<CardImage>();
                    cardImages.Add(new CardImage(url: link));
                    List<CardAction> cardButtons = new List<CardAction>();
                    CardAction plButton = new CardAction()
                    {
                        Value = link,
                        Type = "openUrl",
                        Title = "Ver imagem"
                    };
                    cardButtons.Add(plButton);
                    HeroCard plCard = new HeroCard()
                    {
                        Title = "Desenho da Arquitetura",
                        Images = cardImages,
                        Buttons = cardButtons
                    };
                    Attachment plAttachment = plCard.ToAttachment();
                    reply.Attachments.Add(plAttachment);
                    interactive = true;
                }
                else if (activity.Text.ToLower().Contains("bot") && activity.Text.ToLower().Contains("nasceu"))
                {
                    string link = "https://peulfg.by3302.livefilestore.com/y3p1D5hjNqlIPq_Wm5Z7soOICsfXeER2iA1yDEpUwCb19QFkOnMhzyVl0ifJmh6ijPJLjRzIeGkzolQR22P_kgGusWpx3FC7mSdjWrK1kAyOJM61uljRot-sBW9I-V8BESUrvZK3ra2mPGWOcrS6HPXJylsJwDCAfCW2AYxri_f9_g/flow.PNG?psid=1";
                    reply = activity.CreateReply("Esse assunto é o mais interessante! Este aqui é meu fluxo DevOps.");
                    reply.Recipient = activity.From;
                    reply.Type = "message";
                    reply.Attachments = new List<Attachment>();
                    List<CardImage> cardImages = new List<CardImage>();
                    cardImages.Add(new CardImage(url: link));
                    List<CardAction> cardButtons = new List<CardAction>();
                    CardAction plButton = new CardAction()
                    {
                        Value = link,
                        Type = "openUrl",
                        Title = "Ver imagem"
                    };
                    cardButtons.Add(plButton);
                    HeroCard plCard = new HeroCard()
                    {
                        Title = "DevOps Flow",
                        Images = cardImages,
                        Buttons = cardButtons
                    };
                    Attachment plAttachment = plCard.ToAttachment();
                    reply.Attachments.Add(plAttachment);
                    interactive = true;
                }
                else if (AWStoAzure(activity.Text) == "OK")
                {
                    reply = createReplyAWS(activity);
                    interactive = true;
                }
                else
                {
                    string[] words = activity.Text.Split(default(string[]), StringSplitOptions.RemoveEmptyEntries);
                    string entities = "";
                    foreach (string word in words)
                    {
                        entities = word + "|";
                    }
                    try
                    {
                        var result = GetLinks(entities).First();
                        reply = activity.CreateReply(Random("standard") + result);
                    }
                    catch (Exception)
                    {
                        reply = activity.CreateReply(Random("missunderstood"));
                    }
                    
                }
                //else
                //{
                //    reply = activity.CreateReply(Random("missunderstood"));
                //}

                if (interactive)
                {
                    interactive = !interactive;
                    await connector.Conversations.SendToConversationAsync(reply);
                } else
                {
                    await connector.Conversations.ReplyToActivityAsync(reply);
                }
            }
            else
            {
                HandleSystemMessage(activity);
            }
            var response = Request.CreateResponse(HttpStatusCode.OK);
            return response;
        }

        #region HandleSystemMessage
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
        #endregion

        #region /APIS
        private string[] GetLinks(string keywords)
        {
            telemetry.TrackEvent("GetLinks");
            HttpClient client = new HttpClient();
            HttpResponseMessage response = client.GetAsync("http://dxswarmagents.eastus.cloudapp.azure.com/api/links/" + keywords).Result;
            string[] data = JsonConvert.DeserializeObject<string[]>(response.Content.ReadAsStringAsync().Result);
            Debug.WriteLine(data);
            return data;
        }
        #endregion

        #region Methods Contain and Random
        /// <summary>
        ///     Used to verify each word inside the message sent from user with specific dictionaries 
        /// </summary>
        /// <param name="filename"></param>  -> contain the name of the file .JSON with dictionary
        /// <param name="msg"></param>  -> the message sent from user to verify with the dictionary
        /// <returns></returns>
        private bool Contain(string filename, string msg)
        {
            string path = "~/Docs/" + filename + ".json";
            var json = File.ReadAllText(HttpContext.Current.Server.MapPath(path));

            dynamic array = JsonConvert.DeserializeObject(json);
            foreach (string word in array.data)
            {
                if (msg.ToLower().Contains(word.ToLower()))
                {
                    telemetry.TrackEvent("Contain: " + filename);
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        ///     Used to get a dictionary with sorted response and sort one of them
        /// </summary>
        /// <param name="filename"></param>  -> contain the name of the file .JSON with dictionary
        /// <returns></returns>
        private string Random(string filename)
        {
            string path = "~/Docs/" + filename + ".json";
            var json = File.ReadAllText(HttpContext.Current.Server.MapPath(path));

            telemetry.TrackEvent("Random: " + filename);

            dynamic array = JsonConvert.DeserializeObject(json);
            List<string> x = new List<string>();
            foreach (string word in array.data)
            {
                x.Add(word);
            }
            return x[random.Next(x.Count())];
        }

        private string AWStoAzure(string msg)
        {
            string result = "";

            string path = "~/Docs/AWS2Azure.json";
            var json = File.ReadAllText(HttpContext.Current.Server.MapPath(path));

            dynamic array = JsonConvert.DeserializeObject(json);
            foreach (var word in array)
            {
                var x = msg.ToLower();
                if (msg.ToLower().Contains(word.Name.ToLower()))
                {
                    telemetry.TrackEvent("AWS searched: " + word.Name + ". And Azure found: " + word.First.Value);
                    AWSEntity = word.First.Value;
                    result = "OK";
                }
            }
            return result;
        }
        #endregion

        #region Custom Replies (Carousel)
        private Activity createReplyWithCarousel(Activity message)
        {
            Activity replyToConversation = message.CreateReply(Random("standard"));
            replyToConversation.Recipient = message.From;
            replyToConversation.Type = "message";
            replyToConversation.AttachmentLayout = AttachmentLayoutTypes.Carousel;
            replyToConversation.Attachments = new List<Attachment>();

            Dictionary<string, string> cardContentList = new Dictionary<string, string>();
            cardContentList.Add("PigLatin", "https://<ImageUrl1>");
            cardContentList.Add("Pork Shoulder", "https://<ImageUrl2>");
            cardContentList.Add("Bacon", "https://<ImageUrl3>");

            foreach (KeyValuePair<string, string> cardContent in cardContentList)
            {
                List<CardImage> cardImages = new List<CardImage>();
                cardImages.Add(new CardImage(url: cardContent.Value));

                List<CardAction> cardButtons = new List<CardAction>();

                CardAction plButton = new CardAction()
                {
                    Value = $"https://en.wikipedia.org/wiki/{cardContent.Key}",
                    Type = "openUrl",
                    Title = "WikiPedia Page"
                };
                cardButtons.Add(plButton);

                HeroCard plCard = new HeroCard()
                {
                    Title = $"I'm a hero card about {cardContent.Key}",
                    Subtitle = $"{cardContent.Key} Wikipedia Page",
                    Images = cardImages,
                    Buttons = cardButtons
                };

                Attachment plAttachment = plCard.ToAttachment();
                replyToConversation.Attachments.Add(plAttachment);
            }

            replyToConversation.AttachmentLayout = AttachmentLayoutTypes.Carousel;

            return replyToConversation;
        }

        private Activity createReplyAWS(Activity message)
        {
            Activity replyToConversation = message.CreateReply("Este é um serviço da AWS. " + Random("AWSconvertion") + AWSEntity + ". " + Random("AWSrecomendations"));
            replyToConversation.Recipient = message.From;
            replyToConversation.Type = "message";
            replyToConversation.Attachments = new List<Attachment>();

            List<CardAction> cardButtons = new List<CardAction>();

            CardAction plButtonYES = new CardAction()
            {
                Value = "yesaws " + AWSEntity,
                Type = "postBack",
                Title = "SIM"
            };
            cardButtons.Add(plButtonYES);

            CardAction plButtonNO = new CardAction()
            {
                Value = "noaws",
                Type = "postBack",
                Title = "NÃO"
            };
            cardButtons.Add(plButtonNO);

            HeroCard plCard = new HeroCard()
            {
                Buttons = cardButtons
            };

            Attachment plAttachment = plCard.ToAttachment();
            replyToConversation.Attachments.Add(plAttachment);

            return replyToConversation;
        }
        #endregion

        #region Methods
        public void SendDataTelemetryToTableStorage(string name, string channel)
        {
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(CloudConfigurationManager.GetSetting("StorageConnectionString"));

            // Create the blob client.
            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();

            // Retrieve a reference to a container.
            CloudBlobContainer container = blobClient.GetContainerReference("telemetrycontainer");

            // Create the container if it doesn't already exist.
            container.CreateIfNotExists();

            string n = Guid.NewGuid().ToString();
            CloudBlockBlob blockBlob = container.GetBlockBlobReference(n);

            TelemetryEntity tel = new TelemetryEntity();
            tel.UserName = name;
            tel.UserChannel = channel;

            blockBlob.UploadText(JsonConvert.SerializeObject(tel));
        }
        #endregion

        #region Entity to Blob Storage
        public class TelemetryEntity : Entity
        {
            public TelemetryEntity() {}

            public string UserName { get; set; }

            public string id { get; set; }

            public string UserChannel { get; set; }
        }
        #endregion

        #region LINKS OVERVIEW
        private Dictionary<string, string> overview = new Dictionary<string, string>
        {
            {"Virtual Machines", "https://azure.microsoft.com/pt-br/documentation/services/virtual-machines/"},
            {"Docker Virtual Machine Extension", "https://azure.microsoft.com/pt-br/documentation/articles/virtual-machines-windows-dockerextension/"},
            {"Container Service", "https://azure.microsoft.com/pt-br/documentation/articles/container-service-intro/"},
            {"Azure Web Apps", "https://azure.microsoft.com/pt-br/documentation/articles/app-service-web-overview/"},
            {"Azure Autoscale", "https://azure.microsoft.com/pt-br/documentation/articles/virtual-machine-scale-sets-overview/ e https://azure.microsoft.com/pt-br/documentation/articles/web-sites-scale/"},
            {"WebJobs OU Logic Apps OU Azure Funcions", "https://azure.microsoft.com/pt-br/documentation/articles/websites-webjobs-resources/ e https://azure.microsoft.com/pt-br/services/app-service/logic/ e https://azure.microsoft.com/pt-br/documentation/articles/functions-overview/"},
            {"Azure Blob Storage (Block Blob)", "https://azure.microsoft.com/pt-br/documentation/articles/storage-introduction/"},
            {"Azure Blob Storage (Page Blob)", "https://azure.microsoft.com/pt-br/documentation/articles/virtual-machines-disks-vhds/ e https://azure.microsoft.com/pt-br/services/storage/premium-storage/"},
            {"Azure Import/Export", "https://azure.microsoft.com/pt-br/documentation/articles/storage-import-export-service/"},
            {"Azure SQL Database", "https://azure.microsoft.com/pt-br/services/sql-database/"},
            {"Azure DocumentDB", "https://azure.microsoft.com/pt-br/services/documentdb/"},
            {"Azure Managed Cache (Redis Cache)", "https://azure.microsoft.com/pt-br/services/cache/"},
            {"Azure SQL Data Warehouse", "https://azure.microsoft.com/pt-br/services/sql-data-warehouse/"},
            {"Azure Virtual Network", "https://azure.microsoft.com/pt-br/documentation/services/virtual-network/"},
            {"Azure DNS", "https://azure.microsoft.com/pt-br/services/dns/ e https://azure.microsoft.com/pt-br/services/traffic-manager/"},
            {"Azure Load Balancer/Traffic Manager", "https://azure.microsoft.com/pt-br/services/load-balancer/ e https://azure.microsoft.com/pt-br/services/application-gateway/"},
            {"VPN Gateway", "https://azure.microsoft.com/pt-br/services/virtual-network/"},
            {"HDInsight", "https://azure.microsoft.com/pt-br/services/hdinsight/"},
            {"Machine Learning","https://azure.microsoft.com/pt-br/services/machine-learning/"},
            {"Azure Search", "https://azure.microsoft.com/pt-br/services/search/"},
            {"Stream Analytics", "https://azure.microsoft.com/pt-br/services/stream-analytics/"},
            {"Azure CLI, Azure SDKs, PowerShell, 3rd Party Shell Scripting", "https://azure.microsoft.com/pt-br/documentation/articles/powershell-install-configure/ e https://azure.microsoft.com/pt-br/documentation/articles/xplat-cli-install/"},
            {"VSO", "https://www.visualstudio.com/get-started/overview-of-get-started-tasks-vs"},
            {"Visual Studio Application Insights", "https://azure.microsoft.com/pt-br/services/application-insights/"},
            {"Resource Manager", "https://azure.microsoft.com/pt-br/features/resource-manager/"},
            {"Operational Insights", "https://azure.microsoft.com/pt-br/services/application-insights/ e https://azure.microsoft.com/pt-br/services/operational-insights/"},
            {"Automation", "https://azure.microsoft.com/pt-br/services/automation/"},
            {"Active Directory", "https://azure.microsoft.com/pt-br/documentation/articles/role-based-access-control-configure/"},
            {"Multi-Factor Authentication", "https://azure.microsoft.com/pt-br/services/multi-factor-authentication/"},
            {"Key Vault", "https://azure.microsoft.com/pt-br/services/key-vault/"},
            {"Media Services", "https://azure.microsoft.com/pt-br/services/media-services/encoding/"},
            {"Content Delivery Network", "https://azure.microsoft.com/pt-br/services/cdn/"},
            {"Mobile App", "https://azure.microsoft.com/pt-br/services/app-service/mobile/"},
            {"Notification Hubs", "https://azure.microsoft.com/pt-br/services/notification-hubs/"},
            {"Mobile Engagement", "https://azure.microsoft.com/pt-br/services/mobile-engagement/"},
            {"API Apps | API Management", "https://azure.microsoft.com/pt-br/services/api-management/"},
            {"IoT Suite", "https://azure.microsoft.com/pt-br/services/iot-hub/ e https://azure.microsoft.com/en-us/solutions/iot-suite/"},
            {"Templates", "https://azure.microsoft.com/pt-br/documentation/templates/"},
            {"Azure Marketplace", "https://azure.microsoft.com/pt-br/marketplace/"},
            {"Blob Storage", "https://azure.microsoft.com/pt-br/services/storage/blobs/"},
            {"File Storage", "https://azure.microsoft.com/pt-br/services/storage/files/"},
            {"StorSimple" , "https://azure.microsoft.com/pt-br/services/storsimple/"},
            {"Express Route", "https://azure.microsoft.com/pt-br/services/expressroute/"},
            {"SQL Database Migration","https://azure.microsoft.com/pt-br/documentation/articles/sql-database-cloud-migrate/ e https://azure.microsoft.com/pt-br/documentation/articles/sql-database-cloud-migrate/"},
            {"Data Factory", "https://azure.microsoft.com/pt-br/services/data-factory/"},
            {"Power BI", "https://powerbi.microsoft.com/"},
            {"Security Center", "https://azure.microsoft.com/pt-br/services/security-center/"},
            {"Azure AD", "https://azure.microsoft.com/pt-br/services/active-directory/ e https://azure.microsoft.com/pt-br/services/active-directory-ds/"},
            {"Queue Storage", "https://azure.microsoft.com/pt-br/services/storage/queues/"},
            {"Logic Apps", "https://azure.microsoft.com/pt-br/services/app-service/logic/"},
            {"Remote App", "https://azure.microsoft.com/pt-br/services/remoteapp/"},
            {"Dev-Test Lab", "https://azure.microsoft.com/pt-br/services/devtest-lab/"},
        };
        #endregion
    }
}