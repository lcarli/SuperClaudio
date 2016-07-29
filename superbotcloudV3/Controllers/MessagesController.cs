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

namespace superbotcloudV3
{
    [BotAuthentication]
    public class MessagesController : ApiController
    {
        #region Global Variables
        Random random = new Random();
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

                if (Contain("badwords", activity.Text))
                {
                    reply = activity.CreateReply(Random("badwordsanswers"));
                }
                else if (Contain("greetings", activity.Text))
                {
                    reply = activity.CreateReply("Olá! Eu sou o Super Claudio. Estou aqui para lhe guiar na nuvem. Vamos começar? Também posso traduzir algum serviço da aws para o Azure, caso deseje.");
                }
                else if (Contain("goodbyes", activity.Text))
                {
                    reply = activity.CreateReply(Random("goodbyesanswers"));
                }
                //else if (Contain("AWSlist", activity.Text))
                //{
                //    reply = activity.CreateReply(Random("AWSconvertion"));
                //}
                else
                {
                    reply = activity.CreateReply(Random("missunderstood"));
                }

                await connector.Conversations.ReplyToActivityAsync(reply);
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
                if (msg.ToLower().Contains(word.ToLower())) return true;
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

            dynamic array = JsonConvert.DeserializeObject(json);
            List<string> x = new List<string>();
            foreach (string word in array.data)
            {
                x.Add(word);
            }
            return x[random.Next(x.Count())];
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
        #endregion
    }
}