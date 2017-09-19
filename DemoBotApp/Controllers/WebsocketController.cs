namespace DemoBotApp.Controllers
{
    using System.IO;
    using System.Net;
    using System.Net.Http;
    using System.Web;
    using System.Web.Http;
    using DemoBotApp.WebSocket;

    [RoutePrefix("chat")]
    public class WebsocketController : ApiController
    {
        private WebSocketHandler webSocketHandler = new WebSocketHandler();
        
        [Route("")]
        [HttpGet]
        public HttpResponseMessage Connect(string nickName)
        {
            this.webSocketHandler.OnOpened += (sender, arg) =>
            {
                //this.webSocketHandler.SendMessage(nickName + " Connected!").Wait();
            };

            this.webSocketHandler.OnMessageReceived += (sender, message) =>
            {
                //this.webSocketHandler.SendMessage($"{nickName} says: {message}").Wait();

                using (MemoryStream ms = (MemoryStream)SampleMusic.GetStream())
                {
                    byte[] totalBytes = ms.ToArray();
                    this.webSocketHandler.SendBinary(totalBytes).Wait();
                }
            };

            this.webSocketHandler.OnClosed += (sender, arg) =>
            {
                this.webSocketHandler.SendMessage(nickName + " Disconnected!").Wait();
            };

            HttpContext.Current.AcceptWebSocketRequest(this.webSocketHandler);

            return Request.CreateResponse(HttpStatusCode.SwitchingProtocols);
        }
    }
}
