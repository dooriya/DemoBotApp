namespace DemoBotApp.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Net;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Web;
    using System.Web.Http;
    using DemoBotApp.WebSocket;
    using Microsoft.Bing.Speech;
    using Microsoft.Bot.Connector.DirectLine;

    [RoutePrefix("chat")]
    public class WebsocketController : ApiController
    {
        private static readonly Uri ShortPhraseUrl = new Uri(@"wss://speech.platform.bing.com/api/service/recognition");
        private static readonly Uri LongDictationUrl = new Uri(@"wss://speech.platform.bing.com/api/service/recognition/continuous");
        private static readonly Uri SpeechSynthesisUrl = new Uri("https://speech.platform.bing.com/synthesize");
        private static readonly string CognitiveSubscriptionKey = "16a433c4b68241dbb136447a324be771";

        private TTSClient ttsClient;
        private string speechLocale = "en-US";

        private DirectLineClient directLineClient;
        private static readonly string DirectLineSecret = "HY6eguA5VH8.cwA.Hs0.36pUL-4FWqmTNcckkkJ75_QIF_MjKizLN3zQVSpGO_8";
        private static readonly string BotId = "demobotservice-eas";
        private static readonly string FromUserId = "TestUser";

        private WebSocketHandler defaultHandler = new WebSocketHandler();
        private static Dictionary<string, WebSocketHandler> handlers = new Dictionary<string, WebSocketHandler>();

        public WebsocketController()
        {
            // Setup bot client
            this.directLineClient = new DirectLineClient(DirectLineSecret);

            // Setup speech synthesis client
            SynthesisOptions synthesisOption = new SynthesisOptions(SpeechSynthesisUrl, CognitiveSubscriptionKey);
            this.ttsClient = new TTSClient(synthesisOption);
        }

        [Route("")]
        [HttpGet]
        public HttpResponseMessage Connect(string nickName)
        {
            WebSocketHandler webSocketHandler = new WebSocketHandler();
            if (handlers.ContainsKey(nickName))
            {
                WebSocketHandler origHandler = handlers[nickName];
                origHandler.Close().Wait();
            }
            handlers[nickName] = webSocketHandler;

            string conversationId = string.Empty;

            webSocketHandler.OnOpened += (sender, arg) =>
            {
                Conversation conversation = this.directLineClient.Conversations.StartConversation();
                conversationId = conversation.ConversationId;

                //this.webSocketHandler.SendMessage($"{nickName} connected! Conversation Id: {conversationId}").Wait();
                //this.webSocketHandler.SendMessage(nickName + " Connected!").Wait();
            };

            webSocketHandler.OnTextMessageReceived += (sender, message) =>
            {
                //this.webSocketHandler.SendMessage($"{nickName} says: {message}").Wait();
                using (MemoryStream ms = (MemoryStream)SampleMusic.GetStream())
                {
                    byte[] totalBytes = ms.ToArray();
                    webSocketHandler.SendBinary(totalBytes).Wait();
                }
            };

            webSocketHandler.OnClosed += (sender, arg) =>
            {
                webSocketHandler.SendMessage(nickName + " Disconnected!").Wait();
            };

            HttpContext.Current.AcceptWebSocketRequest(webSocketHandler);
            return Request.CreateResponse(HttpStatusCode.SwitchingProtocols);
        }
    }
}
