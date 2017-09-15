using Microsoft.Bing.Speech;
using Microsoft.Bot.Connector.DirectLine;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using System.Web.WebSockets;
using System.Net.WebSockets;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http.Headers;
using NAudio.Wave;

namespace DemoBotApp.Controllers
{
    [RoutePrefix("ws")]
    public class WebsocketController : ApiController
    {


        public async Task ProcessRequest(AspNetWebSocketContext context)
        {
            var socket = context.WebSocket;//传入的context中有当前的web socket对象
            // _sockets.Add(socket);//此处将web socket对象加入一个静态列表中

            //进入一个无限循环，当web socket close是循环结束
            while (true)
            {
                var buffer = new ArraySegment<byte>(new byte[1024]);
                var receivedResult = await socket.ReceiveAsync(buffer, CancellationToken.None);//对web socket进行异步接收数据
                if (receivedResult.MessageType == WebSocketMessageType.Close)
                {
                    await socket.CloseAsync(WebSocketCloseStatus.Empty, string.Empty, CancellationToken.None);//如果client发起close请求，对client进行ack
                    break;
                }
                
                if (socket.State == System.Net.WebSockets.WebSocketState.Open)
                {

                    string recvMsg = "server response" + Encoding.UTF8.GetString(buffer.Array, 0, receivedResult.Count);
                    var recvBytes = Encoding.UTF8.GetBytes(recvMsg);
                    var sendBuffer = new ArraySegment<byte>(recvBytes);
                    
                            await socket.SendAsync(sendBuffer, WebSocketMessageType.Text, true, CancellationToken.None);
                }
            }
        }

        [Route("ws")]
        [HttpGet]
        public HttpResponseMessage Connect(string nickName)
        {
            HttpContext.Current.AcceptWebSocketRequest(ProcessRequest);

            return Request.CreateResponse(HttpStatusCode.SwitchingProtocols);
        }
    }
}
