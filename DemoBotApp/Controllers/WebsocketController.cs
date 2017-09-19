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

            var buffer = new ArraySegment<byte>(new byte[1024]);
            var receivedResult = await socket.ReceiveAsync(buffer, CancellationToken.None);//对web socket进行异步接收数据
            if (receivedResult.MessageType == WebSocketMessageType.Close)
            {
                await socket.CloseAsync(WebSocketCloseStatus.Empty, string.Empty, CancellationToken.None);//如果client发起close请求，对client进行ack
            }
            //进入一个无限循环，当web socket close是循环结束
            Stream s = SampleMusic.GetStream();
            int chunk = 4096;
            int sendTime = (int)((s.Length - 1) / chunk + 1);
            byte[] bytes = new byte[chunk];
            while (true)
            {
                
                if (socket.State == System.Net.WebSockets.WebSocketState.Open)
                {
                    for (int send = 0; send < sendTime; ++send)
                    {
                        int sz = s.Read(bytes, 0, chunk);
                        var sendBuffer = new ArraySegment<byte>(bytes, 0, sz);
                        await socket.SendAsync(sendBuffer, WebSocketMessageType.Binary, send == sendTime - 1, CancellationToken.None);
                    }
                }
                buffer = new ArraySegment<byte>(new byte[1024]);
                receivedResult = await socket.ReceiveAsync(buffer, CancellationToken.None);//对web socket进行异步接收数据
                if (receivedResult.MessageType == WebSocketMessageType.Close)
                {
                    await socket.CloseAsync(WebSocketCloseStatus.Empty, string.Empty, CancellationToken.None);//如果client发起close请求，对client进行ack
                    break;
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
