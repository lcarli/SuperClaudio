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
        Random random = new Random();
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
    }
}