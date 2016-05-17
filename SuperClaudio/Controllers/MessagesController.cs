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
using Microsoft.Bot.Builder.Luis;
using Microsoft.Bot.Builder.FormFlow;
using System.Collections.Generic;

namespace SuperClaudio
{
    [BotAuthentication]
    public class MessagesController : ApiController
    {
        /// <summary>
        /// POST: api/Messages
        /// Receive a message from a user and reply to it
        /// </summary>
        public async Task<Message> Post([FromBody]Message message)
        {
            if (message.Type == "Message")
            {
                Util.listOfKeywords.Clear();
                // calculate something for us to return
                int length = (message.Text ?? string.Empty).Length;
                //return message.CreateReplyMessage("Olá, eu consigo ler.", "pt");

                //waitingAnswer
                if (Util.waitingAnswer)
                {
                    if (message.Text.ToLower() == "sim")
                    {
                        var r = "";
                        if (overview.TryGetValue(Util.subject, out r))
                        {
                            Util.waitingAnswer = false;
                            Util.subject = "";
                            return message.CreateReplyMessage("Ok. Você pode começar por aqui: " + r);
                        }
                    }
                    else
                    {
                        Util.waitingAnswer = false;
                        Util.subject = "";
                        return message.CreateReplyMessage("Ok.");
                    }
                }

                if (message.Text.ToLower().Contains("amazon") || message.Text.ToLower().Contains("aws"))
                {
                    var result = "";
                    foreach(KeyValuePair < string, string > entry in AWSToAzure)
                    {
                        if (message.Text.ToLower().Contains(entry.Key.ToLower()))
                        {
                            result = entry.Value;
                        }
                    }
                    if (result == "")
                    {
                        return message.CreateReplyMessage("Não entendi.");
                    }
                    else
                    {
                        Util.waitingAnswer = true;
                        Util.subject = result;
                        return message.CreateReplyMessage("Entendi. No Azure nos chamamos este serviço de: " + result + ". Posso lhe ajudar indicando uma introdução sobre o assunto?");
                    }
                }
                else
                {
                    var entity = "nenhuma";
                    var action = "nada";
                    //verifico a entidade
                    foreach (KeyValuePair<string, string> entry in EntitiesOfAzure)
                    {
                        if (message.Text.ToLower().Contains(entry.Key))
                        {
                            entity = entry.Value;
                            Util.listOfKeywords.Add(entry.Value);
                        }
                    }
                    //verifico a ação
                    foreach (KeyValuePair<string, string> entry in ActionsOfAzure)
                    {
                        if (message.Text.ToLower().Contains(entry.Key))
                        {
                            action = entry.Value;
                            Util.listOfKeywords.Add(entry.Value);
                        }
                    }

                    //verifico se o bot entendeu
                    if (action != "nada" && entity != "nenhuma")
                    {
                        //return message.CreateReplyMessage("Olá, eu consegui entender que você quer saber como " + action + " sobre " + entity, "pt");
                        var result = "";
                        //foreach (var item in links)
                        //{
                        //    if (item.Contains(action.ToLower()) && item.Contains(entity.ToLower()))
                        //    {
                        //        result = item;
                        //    }
                        //}


                        //test using list of
                        var max = 0;
                        foreach (KeyValuePair<string[], string> entry in linksKeywords)
                        {
                            var counter = 0;
                            foreach (var item in entry.Key)
                            {
                                foreach (var key in Util.listOfKeywords)
                                {
                                    if (key.ToLower() == item.ToLower())
                                    {
                                        counter += 1;
                                    }
                                }
                                if (counter > max)
                                {
                                    max = counter;
                                    result = entry.Value;
                                }
                            }
                        }
                        if (result == "")
                        {
                            return message.CreateReplyMessage("Estou em dúvida. Seria melhor envolver alguém...");
                        }
                        else
                        {
                            return message.CreateReplyMessage("Acredito que esse link possa lhe ajudar: " + result);
                        }

                    }
                    else if (entity == "nenhuma" && action != "nada")
                    {
                        return message.CreateReplyMessage("Entendi que você quer " + action + " algo, mas não entendi exatamente o quê. Pode melhorar a pergunta?", "pt");
                    }
                    else if (action == "nada" && entity != "nenhuma")
                    {
                        return message.CreateReplyMessage("Vejo que você quer saber sobre " + entity + " mas não entendi muito bem o que você quer fazer. Pode reformular?", "pt");
                    }
                    else
                    {
                        return message.CreateReplyMessage("Não consegui compreender. Você pode melhorar a pergunta?", "pt");
                    }
                }

                //testing Bot.BUILDER
                //return await Conversation.SendAsync(message, () => new ClaudioCore());
            }
            else
            {
                return HandleSystemMessage(message);
            }
        }

        private Message HandleSystemMessage(Message message)
        {
            if (message.Type == "Ping")
            {
                Message reply = message.CreateReplyMessage();
                reply.Type = "Ping";
                return reply;
            }
            else if (message.Type == "DeleteUserData")
            {
                // Implement user deletion here
                // If we handle user deletion, return a real message
            }
            else if (message.Type == "BotAddedToConversation")
            {
                return message.CreateReplyMessage("Olá! Eu sou o Super Claudio. Estou aqui para lhe guiar na nuvem. Vamos começar? Se quiser ajuda sobre algo da AWS, digite 'Amazon' no final da sua pergunta.");
            }
            else if (message.Type == "BotRemovedFromConversation")
            {
            }
            else if (message.Type == "UserAddedToConversation")
            {
                return message.CreateReplyMessage("Vejo que alguém entrou na conversa. Seja bem vindo!");
            }
            else if (message.Type == "UserRemovedFromConversation")
            {
            }
            else if (message.Type == "EndOfConversation")
            {
                return message.CreateReplyMessage("Obrigado por me consultar. Espero ter ajudado.");
            }

            return null;
        }

        #region AWS TO AZURE
        private Dictionary<string, string> AWSToAzure = new Dictionary<string, string>
        {
            {"EC2","Virtual Machines"},
            {"Container Registry","Docker Virtual Machine Extension"},
            {"Container Service","Container Service"},
            {"Elastic Beanstalk","Azure Web Apps"},
            {"Beanstalk","Azure Web Apps"},
            {"Auto Scaling","Azure Autoscale"},
            {"Lambda","WebJobs OU Logic Apps OU Azure Funcions"},
            {"Simple Storage","Azure Blob Storage (Block Blob)"},
            {"Elastic Block Storage","Azure Blob Storage (Page Blob)"},
            {"Amazon Import/Export","Azure Import/Export"},
            {"Import/Export","Azure Import/Export"},
            {"Amazon Relational Database Service (RDS)","Azure SQL Database"},
            {"Relational Database Service (RDS)","Azure SQL Database"},
            {"Amazon Relational Database Service","Azure SQL Database"},
            {"Relational Database Service","Azure SQL Database"},
            {"RDS","Azure SQL Database"},
            {"Amazon Dynamo DB","Azure DocumentDB"},
            {"Dynamo DB","Azure DocumentDB"},
            {"Amazon Elastic Cache","Azure Managed Cache (Redis Cache)"},
            {"Elastic Cache","Azure Managed Cache (Redis Cache)"},
            {"Amazon Redshift","Azure SQL Data Warehouse"},
            {"Redshift","Azure SQL Data Warehouse"},
            {"Amazon VPC","Azure Virtual Network"},
            {"VPC","Azure Virtual Network"},
            {"Amazon Route 53","Azure DNS"},
            {"Route 53","Azure DNS"},
            {"Load Balancing","Azure Load Balancer/Traffic Manager"},
            {"Load Balance","Azure Load Balancer/Traffic Manager"},
            {"Virtual Private Gateway","VPN Gateway"},
            {"VPN","VPN Gateway"},
            {"Amazon EMR","HDInsight"},
            {"EMR","HDInsight"},
            {"Elastic Map Reduce","HDInsight"},
            {"Machine Learning","Machine Learning"},
            {"Amazon Elasticsearch Service","Azure Search"},
            {"Elasticsearch Service","Azure Search"},
            {"Elasticsearch","Azure Search"},
            {"Amazon Kinesis ","Stream Analytics"},
            {"Kinesis ","Stream Analytics"},
            {"Command Line Interface","Azure CLI, Azure SDKs, PowerShell, 3rd Party Shell Scripting"},
            {"CLI","Azure CLI, Azure SDKs, PowerShell, 3rd Party Shell Scripting"},
            {"CodeCommit","VSO"},
            {"CodeDeploy","VSO"},
            {"CloudWatch","Visual Studio Application Insights"},
            {"CloudFormation","Resource Manager"},
            {"Cloud Watch","Visual Studio Application Insights"},
            {"Cloud Formation","Resource Manager"},
            {"Amazon CloudTrail","Operational Insights"},
            {"CloudTrail","Operational Insights"},
            {"OpsWorks","Automation"},
            {"Ops Works","Automation"},
            {"IAM","Active Directory"},
            {"Multi-Factor Authentication","Multi-Factor Authentication"},
            {"MultiFactor Authentication","Multi-Factor Authentication"},
            {"Multi Factor Authentication","Multi-Factor Authentication"},
            {"Multi-Factor Authenticator","Multi-Factor Authentication"},
            {"MultiFactor Authenticator","Multi-Factor Authentication"},
            {"Multi Factor Authenticator","Multi-Factor Authentication"},
            {"Key Management Service","Key Vault"},
            {"KMS","Key Vault"},
            {"Elastic Transcoder ","Media Services"},
            {"CloudFront","Content Delivery Network"},
            {"Cloud Front","Content Delivery Network"},
            {"AWS Mobile Hub","Mobile App"},
            {"Mobile Hub","Mobile App"},
            {"Cognito","Mobile App"},
            {"SNS","Notification Hubs"},
            {"Mobile Analytics","Mobile Engagement"},
            {"Amazon API Gateway","API Apps | API Management"},
            {"API Gateway","API Apps | API Management"},
            {"IoT","IoT Suite"},
            {"AWS Quick Start","Template"},
            {"Quick Start","Template"},
            {"AWS Marketplace","Azure Marketplace"},
            {"Marketplace","Azure Marketplace"},
            {"S3", "Blob Storage"},
            {"Elastic File System", "File Storage"},
            {"Storage Gateway", "StorSimple"},
            {"Direct Connect", "Express Route"},
            {"Database Migration Service", "SQL Database Migration"},
            {"DMS", "SQL Database Migration"},
            {"Data Pipeline", "Data Factory"},
            {"QuickSight", "Power BI"},
            {"Quick Sight", "Power BI"},
            {"Inspector", "Security Center"},
            {"Directory Service", "Azure AD"},
            {"Simple Queue Service", "Queue Storage"},
            {"Simple Workflow Service", "Logic Apps"},
            {"AppStream", "Remote App"},
            {"Device Farm", "Dev-Test Lab"},
        };
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

        #region LINKS AZURE
        private List<string> links = new List<string>
        {
            "https://azure.microsoft.com/pt-br/documentation/learning-paths/virtual-machines/",
            "https://azure.microsoft.com/pt-br/documentation/articles/virtual-machines-windows-hero-tutorial/",
            "https://azure.microsoft.com/pt-br/documentation/articles/virtual-machines-windows-ps-create/",
            "https://azure.microsoft.com/pt-br/documentation/articles/virtual-machines-windows-connect-logon/",
            "https://azure.microsoft.com/pt-br/documentation/articles/virtual-machines-windows-creation-choices/",
            "https://azure.microsoft.com/pt-br/documentation/articles/virtual-machines-windows-about/",
        };
        #endregion

        #region LINKS AZURE WITH KEYWORD
        private Dictionary<string[], string> linksKeywords = new Dictionary<string[], string>
        {
            {new[] {"Windows","Virtual Machines", "Learning Path"}, "https://azure.microsoft.com/pt-br/documentation/learning-paths/virtual-machines/" },
            {new[] { "Windows", "Create", "Virtual Machine", "Portal" }, "https://azure.microsoft.com/pt-br/documentation/articles/virtual-machines-windows-hero-tutorial/" },
            {new[] { "Windows", "PowerShell", "Virtual Machine", "Create" }, "https://azure.microsoft.com/pt-br/documentation/articles/virtual-machines-windows-ps-create/" },
            {new[] { "Windows", "Connect", "Login", "Virtual Machine" }, "https://azure.microsoft.com/pt-br/documentation/articles/virtual-machines-windows-connect-logon/" },
            {new[] { "Windows", "Create", "Virtual Machine", "ARM" }, "https://azure.microsoft.com/pt-br/documentation/articles/virtual-machines-windows-ps-template/" },
            {new[] { "Windows", "Sobre", "Virtual Machine" }, "https://azure.microsoft.com/pt-br/documentation/articles/virtual-machines-windows-about/" },
            {new[] { "Windows", "Virtual Machine", "Compare"}, "https://azure.microsoft.com/pt-br/documentation/articles/virtual-machines-windows-compare-deployment-models/" },
            {new[] { "Windows", "Virtual Machine", "Extension"}, "https://azure.microsoft.com/pt-br/documentation/articles/virtual-machines-windows-extensions-features/" },
            { new[] { "Windows", "Container", "Virtual Machine"}, "https://azure.microsoft.com/pt-br/documentation/articles/virtual-machines-windows-containers/" },
            { new[] { "Windows", "Scale", "Virtual Machine"}, "https://azure.microsoft.com/pt-br/documentation/articles/virtual-machine-scale-sets-overview/" },
            { new[] { "Windows", "Size", "Virtual Machine"}, "https://azure.microsoft.com/pt-br/documentation/articles/virtual-machines-windows-sizes/" },
            { new[] { "Windows", "PowerShell", "Virtual Machine", "Create", "ARM"}, "https://azure.microsoft.com/pt-br/documentation/articles/virtual-machines-windows-create-powershell/" },
            { new[] { "Windows", "Virtual Machine", "Capture"}, "https://azure.microsoft.com/pt-br/documentation/articles/virtual-machines-windows-capture-image/" },
            { new[] { "Windows", "Upload", "Virtual Machine", "Image"}, "https://azure.microsoft.com/pt-br/documentation/articles/virtual-machines-windows-upload-image/" },
            { new[] { "Windows", "Copy", "Virtual Machine", "ARM"}, "https://azure.microsoft.com/pt-br/documentation/articles/virtual-machines-windows-specialized-image/" },
            { new[] { "Windows", "Samples", "Example", "Virtual Machine", "ARM"}, "https://azure.microsoft.com/pt-br/documentation/articles/virtual-machines-windows-extensions-configuration-samples/" },
            { new[] { "Windows", "Chef", "Virtual Machine", "Automation"}, "https://azure.microsoft.com/pt-br/documentation/articles/virtual-machines-windows-chef-automation/" },
            { new[] { "Windows", "ARM", "ASM", "Virtual Network", "Virtual Machine", "Connect"}, "https://azure.microsoft.com/pt-br/documentation/articles/virtual-networks-arm-asm-s2s-howto/" },
            { new[] {"Windows","Virtual Machine", "PowerShell", "Manage"}, "https://azure.microsoft.com/pt-br/documentation/articles/virtual-machines-windows-ps-manage/" },
            { new[] {"Manage", "Windows", "Virtual Machine", "C#"}, "https://azure.microsoft.com/pt-br/documentation/articles/virtual-machines-windows-csharp-manage/" },
            { new[] {"Create", "Virtual Machine", "Windows", "Script", "Custom", "Extension"}, "https://azure.microsoft.com/pt-br/documentation/articles/virtual-machines-windows-extensions-customscript/" },
            { new[] {"Windows", "Tag", "Virtual Machine"}, "https://azure.microsoft.com/pt-br/documentation/articles/virtual-machines-windows-tag/" },
            { new[] {"Windows", "Virtual Machine", "Disk", "Size", "Expand", "Image"}, "https://azure.microsoft.com/pt-br/documentation/articles/virtual-machines-windows-expand-os-disk/" },
            { new[] {"Linux", "CLI", "Virtual Machine", "Create" }, "https://azure.microsoft.com/pt-br/documentation/articles/virtual-machines-windows-cli-deploy-templates/" },
            { new[] {"Windows", "Virtual Machine", "About", "Disk"}, "https://azure.microsoft.com/pt-br/documentation/articles/virtual-machines-windows-about-disks-vhds/" },
            { new[] {"Windows", "Attach", "Virtual Machine", "Disk", "Portal"}, "https://azure.microsoft.com/pt-br/documentation/articles/virtual-machines-windows-attach-disk-portal/" },
            { new[] {""}, "" },
            { new[] {""}, "" },
            { new[] {""}, "" },
            { new[] {""}, "" },
            { new[] {""}, "" },
            { new[] {""}, "" },

            { new[] {""}, "" },
        };
        #endregion

        #region ACTIONS LIST
        private Dictionary<string, string[]> ActionsListOfAzure = new Dictionary<string, string[]>
        {
            {"create", new[] { "Create"} },
            {"criar", new[] { "Create" } },
            {"initiate", new[] { "Create" } },
            {"iniciar", new[] { "Create" } },
            {"deploy", new[] { "Create", "Deploy" } },
            {"deploiar", new[] { "Create" } },
            {"deployar", new[] { "Create" } },
            {"instanciar", new[] { "Create" } },
            {"new", new[] { "Create" } },
            {"nova", new[] { "Create" } },
            {"levantar", new[] { "Create" } },
            {"comecar", new[] { "Create" } },
            {"começar", new[] { "Create" } },
            {"começo", new[] { "Create" } },
            {"comeco", new[] { "Create" } },
            {"crio", new[] { "Create" } },
            {"levanto", new[] { "Create" } },
            {"instancio", new[] { "Create" } },
            {"inicio", new[] { "Create" } },
            {"start", new[] { "Create" } },
            {"aprender", new[] { "Learning Path" } },
            {"curso", new[] { "Learning Path" } },
            {"estudar", new[] { "Learning Path" } },
            {"entender", new[] { "Overview", "sobre" } },
            {"overview", new[] { "overview", "sobre" } },
            {"conhecer", new[] { "overview", "sobre" } },
            {"sobre", new[] { "overview", "sobre" } },
            {"about", new[] { "overview", "sobre" } },
            {"como funciona", new[] { "overview", "Sobre" } },
            {"how works", new[] { "overview", "sobre"} },
            {"how it works", new[] { "overview", "sobre"} },
        };
        #endregion

        #region ACTIONS
        private Dictionary<string, string> ActionsOfAzure = new Dictionary<string, string>
        {
            {"create", "Create"},
            {"criar", "Create" },
            {"initiate", "Create" },
            {"iniciar", "Create" },
            {"subir", "Create" },
            {"deploy", "Create" },
            {"deploiar", "Create" },
            {"deployar", "Create" },
            {"instanciar", "Create" },
            {"new", "Create" },
            {"nova", "Create" },
            {"levantar", "Create" },
            {"comecar", "Create" },
            {"começar", "Create" },
            {"começo", "Create" },
            {"comeco", "Create" },
            {"crio", "Create" },
            {"levanto", "Create" },
            {"instancio", "Create" },
            {"inicio", "Create" },
            {"start", "Create" },
            {"aprender", "Learning Path" },
            {"curso", "Learning Path" },
            {"estudar", "learning-paths" },
            {"entender", "Overview" },
            {"overview", "overview"},
            {"conhecer", "overview"},
            {"sobre", "overview"},
            {"about", "overview"},
            {"como funciona", "overview"},
            {"how works", "overview"},
            {"how it works", "overview"},
            {"comparar", "Compare"},
            {"compare", "Compare"},
            {"diferença entre", "Compare"},
            {"escalar", "Scale"},
            {"escala", "Scale"},
            {"scale", "Scale"},
            {"scaleset", "Scale"},
            {"conjunto de disponibilidade", "Scale"},
            {"escalonamento", "Scale"},
            {"tamanho", "size" },
            {"tamanhos", "size" },
            {"size", "size" },
            {"sizes", "size" },
            {"dimensionamento", "size" },
            {"arm", "ARM"},
            {"resource manager", "ARM"},
            {"gerenciador de recurso", "ARM"},
            {"gerenciador de recursos", "ARM"},
            {"sysprep", "capture"},
            {"capture", "capture"},
            {"capturar", "capture"},
            {"imagem", "capture"},
            {"capturo", "capture"},
            {"image", "Image"},
            {"vhd", "Image"},
            {"vhdx", "Image"},
            {"vhds", "Disk"},
            {"virtual disk", "Image"},
            {"virtual hard disk", "Image"},
            {"disk", "Disk"},
            {"disco", "Disk"},
            {"upload", "upload"},
            {"copia", "Copy"},
            {"copy", "Copy"},
            {"copiar", "Copy"},
            {"copio", "Copy"},
            {"cópia", "Copy"},
            {"example", "example"},
            {"exemplo", "example"},
            {"exemplos", "example"},
            {"sample", "example"},
            {"samples", "example"},
            {"modelos", "example"},
            {"modelo", "example"},
            {"examples", "example"},
            {"Automation", "Automation"},
            {"automação", "Automation"},
            {"automatizar", "Automation"},
            {"automaticamente", "Automation"},
            {"automático", "Automation"},
            {"Connect", "Connect"},
            {"conectar", "Connect"},
            {"conecto", "Connect"},
            {"conexão", "Connect"},
            {"login", "Connect"},
            {"logon", "Connect"},
            {"logar", "Connect"},
            {"manage", "Manage"},
            {"gerenciar", "Manage"},
            {"gerir", "Manage"},
            {"gestão", "Manage"},
            {"Tag", "Tag"},
            {"tagear", "Tag"},
            {"marcar", "Tag"},
            {"Expand", "Expand"},
            {"expandir", "Expand"},
            {"aumentar", "Size"},
            {"redefinir", "Redefine"},
            {"redefine", "Redefine"},
            {"recuperar", "Redefine"},
            {"trocar", "Redefine"},
            {"change", "Redefine"},
            {"mudar", "Redefine"},
            {"password", "Password"},
            {"senha", "Password"},
            {"pass", "Password"},
            {"sobre", "About"},
            {"about", "About"},
            {"atachar", "Attach"},
            {"attachar", "Attach"},
            {"adicionar", "Attach"},
            {"add", "Attach"},
            {"portal", "Portal"},
        };
        #endregion

        #region ENTITIES
        private Dictionary<string, string> EntitiesOfAzure = new Dictionary<string, string>
        {
            {"script", "Script"},
            {"cli", "CLI"},
            {"command line interface", "CLI"},
            {"custom", "Custom"},
            {"customizada", "Custom"},
            {"customizar", "Custom"},
            {"extension", "Extension"},
            {"extensions", "Extension"},
            {"extensão", "Extension"},
            {"extensao", "Extension"},
            {"ps", "PowerShell" },
            {"power shell", "PowerShell" },
            {"powershell", "PowerShell" },
            {"vm", "Virtual Machines" },
            {"mv", "Virtual Machines" },
            {"virtualmachine", "Virtual Machines" },
            {"virtual machine", "Virtual Machines" },
            {"máquina virtual", "Virtual Machines" },
            {"maquina virtual", "Virtual Machines" },
            {"maquinavirtual", "Virtual Machines" },
            {"servico de aplicativo", "AppServices" },
            {"servicode aplicativo", "AppServices" },
            {"servico deaplicativo", "AppServices" },
            {"servicodeaplicativo", "AppServices" },
            {"servicos de aplicativo", "AppServices" },
            {"servicosde aplicativo", "AppServices" },
            {"servicos deaplicativo", "AppServices" },
            {"servicosdeaplicativo", "AppServices" },
            {"serviço de aplicativo", "AppServices" },
            {"serviçode aplicativo", "AppServices" },
            {"serviço deaplicativo", "AppServices" },
            {"serviçodeaplicativo", "AppServices" },
            {"serviçosde aplicativo", "AppServices" },
            {"serviços deaplicativo", "AppServices" },
            {"serviçosdeaplicativo", "AppServices" },
            {"serviços de aplicativo", "AppServices" },
            {"serviços de aplicação", "AppServices" },
            {"serviçosde aplicação", "AppServices" },
            {"serviços deaplicação", "AppServices" },
            {"serviçosdeaplicação", "AppServices" },
            {"serviço de aplicação", "AppServices" },
            {"serviçode aplicação", "AppServices" },
            {"serviço deaplicação", "AppServices" },
            {"serviçodeaplicação", "AppServices" },
            {"servicosde aplicação", "AppServices" },
            {"servicos deaplicação", "AppServices" },
            {"servicosdeaplicação", "AppServices" },
            {"servico de aplicação", "AppServices" },
            {"servicode aplicação", "AppServices" },
            {"servico deaplicação", "AppServices" },
            {"servicodeaplicação", "AppServices" },
            {"app services", "AppServices" },
            {"app service", "AppServices" },
            {"appservices", "AppServices" },
            {"site", "WebApp" },
            {"sites", "WebApp" },
            {"web app", "WebApp" },
            {"web apps", "WebApp" },
            {"webapp", "WebApp" },
            {"webapps", "WebApp" },
            {"aplicação web", "WebApp" },
            {"aplicaçãoweb", "WebApp" },
            {"aplicaçao web", "WebApp" },
            {"aplicaçaoweb", "WebApp" },
            {"aplicacão web", "WebApp" },
            {"aplicacãoweb", "WebApp" },
            {"aplicacao web", "WebApp" },
            {"aplicacaoweb", "WebApp" },
            {"mobile", "MobileApp" },
            {"mobileapp", "MobileApp" },
            {"mobile app", "MobileApp" },
            {"aplicativo móvel", "MobileApp" },
            {"aplicativo movel", "MobileApp" },
            {"aplicativomovel", "MobileApp" },
            {"aplicativomóvel", "MobileApp" },
            {"dispositivo móvel", "MobileApp" },
            {"dispositivomóvel", "MobileApp" },
            {"dispositivomovel", "MobileApp" },
            {"dispositivo movel", "MobileApp" },
            {"celular", "MobileApp" },
            {"cellphone", "MobileApp" },
            {"aplicativos móveis", "MobileApp" },
            {"aplicativos moveis", "MobileApp" },
            {"aplicativosmoveis", "MobileApp" },
            {"aplicativosmóveis", "MobileApp" },
            {"aplicação móvel", "MobileApp" },
            {"aplicação movel", "MobileApp" },
            {"aplicaçãomóvel", "MobileApp" },
            {"aplicaçãomovel", "MobileApp" },
            {"aplicaçao móvel", "MobileApp" },
            {"aplicaçao movel", "MobileApp" },
            {"aplicaçaomóvel", "MobileApp" },
            {"aplicaçaomovel", "MobileApp" },
            {"aplicacão móvel", "MobileApp" },
            {"aplicacão movel", "MobileApp" },
            {"aplicacãomóvel", "MobileApp" },
            {"aplicacãomovel", "MobileApp" },
            {"aplicacao móvel", "MobileApp" },
            {"aplicacao movel", "MobileApp" },
            {"aplicacaomóvel", "MobileApp" },
            {"aplicacaomovel", "MobileApp" },
            {"aplicações móveis", "MobileApp" },
            {"aplicações moveis", "MobileApp" },
            {"aplicaçõesmóveis", "MobileApp" },
            {"aplicaçõesmoveis", "MobileApp" },
            {"aplicaçoes móveis", "MobileApp" },
            {"aplicaçoes moveis", "MobileApp" },
            {"aplicaçoesmóveis", "MobileApp" },
            {"aplicaçoesmoveis", "MobileApp" },
            {"aplicacões móveis", "MobileApp" },
            {"aplicacões moveis", "MobileApp" },
            {"aplicacõesmóveis", "MobileApp" },
            {"aplicacõesmoveis", "MobileApp" },
            {"aplicacoes móveis", "MobileApp" },
            {"aplicacoes moveis", "MobileApp" },
            {"aplicacoesmóveis", "MobileApp" },
            {"aplicacoesmoveis", "MobileApp" },
            {"dispositivos móveis", "MobileApp" },
            {"dispositivos moveis", "MobileApp" },
            {"dispositivossmoveis", "MobileApp" },
            {"dispositivossmóveis", "MobileApp" },
            {"logic apps", "LogicApps" },
            {"logicapps", "LogicApps" },
            {"logic app", "LogicApps" },
            {"logicapp", "LogicApps" },
            {"aplicativos lógicos", "LogicApps" },
            {"aplicativoslógicos", "LogicApps" },
            {"aplicativos logicos", "LogicApps" },
            {"aplicativoslogicos", "LogicApps" },
            {"aplicativo lógico", "LogicApps" },
            {"aplicativológico", "LogicApps" },
            {"aplicativo logico", "LogicApps" },
            {"aplicativologico", "LogicApps" },
            {"apiapp", "APIApps" },
            {"apiapps", "APIApps" },
            {"api app", "APIApps" },
            {"api apps", "APIApps" },
            {"notification", "Notification Hub" },
            {"notificationhub", "Notification Hub" },
            {"notification hub", "Notification Hub" },
            {"notifications", "Notification Hub" },
            {"notificacao", "Notification Hub" },
            {"notificaçao", "Notification Hub" },
            {"notificacão", "Notification Hub" },
            {"notificação", "Notification Hub" },
            {"notificações", "Notification Hub" },
            {"notificaçoes", "Notification Hub" },
            {"notificacões", "Notification Hub" },
            {"notificacoes", "Notification Hub" },
            {"gerenciamento de API", "API Management" },
            {"gerenciamento API", "API Management" },
            {"gestão de API", "API Management" },
            {"gestao de API", "API Management" },
            {"api management", "API Management" },
            {"apimanagement", "API Management" },
            {"functions", "Azure Functions" },
            {"function", "Azure Functions" },
            {"função", "Azure Functions" },
            {"funcão", "Azure Functions" },
            {"funcao", "Azure Functions" },
            {"funçao", "Azure Functions" },
            {"sql azure", "SQL Azure" },
            {"sqlazure", "SQL Azure" },
            {"sql paas", "SQL Azure" },
            {"storage", "Storage" },
            {"armazenamento", "Storage" },
            {"table", "Storage" },
            {"queue", "Storage" },
            {"blob", "Storage" },
            {"arquivo de dados", "Storage" },
            {"tabelas", "Storage" },
            {"files", "Storage" },
            {"iot hub", "IoT Hub" },
            {"iothub", "IoT Hub" },
            {"internetdas coisas", "IoT Hub" },
            {"internet dascoisas", "IoT Hub" },
            {"internetdascoisas", "IoT Hub" },
            {"internet das coisa", "IoT Hub" },
            {"internet of things", "IoT Hub" },
            {"internetof things", "IoT Hub" },
            {"internet ofthings", "IoT Hub" },
            {"internetofthings", "IoT Hub" },
            {"container", "Container"},
            {"docker", "Container"},
            {"contâiner", "Container"},
            {"virtual network", "Virtual Network"},
            {"asm", "Virtual Network"},
            {"virtual private network", "Virtual Network"},
            {"vpn", "Virtual Network"},
            //Languages
            {"php", "PHP"},
            {"ruby", "Ruby"},
            {"python", "python"},
            {"C#", "C#"},
            {"C sharp", "C#"},
            {"C-sharp", "C#"},
            {"java", "java"},
            {"js", "JS"},
            {"node", "JS"},
            {"node.js", "JS"},
            {"JavaScript", "JS"},
            {"java script", "JS"},
            {"C", "C"},
            {"C++", "C++"},
            {"C ++", "C++"},
            {".net", "C#"},
            {"Chef", "Chef"},
            {"Puppet", "Puppet"},
            {"Windows", "Windows"},
            {"Linux", "Linux"},
            {"Unix", "Linux"},
        };
        #endregion
    }
}