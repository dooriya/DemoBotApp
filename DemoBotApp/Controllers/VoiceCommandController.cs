using Microsoft.Bing.Speech;
using Microsoft.Bot.Connector.DirectLine;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using System.Linq;
using System.Net;
using System.Net.Http.Headers;
using NAudio.Wave;
using System.Web;

namespace DemoBotApp.Controllers
{
    [RoutePrefix("conversation")]
    public class VoiceCommandController : ApiController
    {
        private static readonly Uri ShortPhraseUrl = new Uri(@"wss://speech.platform.bing.com/api/service/recognition");
        private static readonly Uri LongDictationUrl = new Uri(@"wss://speech.platform.bing.com/api/service/recognition/continuous");
        private static readonly Uri SpeechSynthesisUrl = new Uri("https://speech.platform.bing.com/synthesize");
        private static readonly string CognitiveSubscriptionKey = "16a433c4b68241dbb136447a324be771";

        private readonly CancellationTokenSource cts = new CancellationTokenSource();
        private readonly Task completedTask = Task.FromResult(true);

        private SpeechClient speechClient;
        private TTSClient ttsClient;
        private string speechLocale = "en-US";
        private string commandText;

        private DirectLineClient directLineClient;
        private static readonly string DirectLineSecret = "HY6eguA5VH8.cwA.Hs0.36pUL-4FWqmTNcckkkJ75_QIF_MjKizLN3zQVSpGO_8";
        private static readonly string BotId = "demobotservice-eas";
        private static readonly string FromUserId = "TestUser";

        public VoiceCommandController()
        {
            // Setup speech recognition client
            Preferences speechPreference = new Preferences(speechLocale, ShortPhraseUrl, new CognitiveTokenProvider(CognitiveSubscriptionKey));
            this.speechClient = new SpeechClient(speechPreference);
            speechClient.SubscribeToRecognitionResult(this.OnRecognitionResult);

            // Setup bot client
            this.directLineClient = new DirectLineClient(DirectLineSecret);

            // Setup speech synthesis client
            SynthesisOptions synthesisOption = new SynthesisOptions(SpeechSynthesisUrl, CognitiveSubscriptionKey);
            this.ttsClient = new TTSClient(synthesisOption);
        }

        [HttpPost]
        [Route("")]
        public async Task<HttpResponseMessage> StartConversation()
        {
            Conversation conversation = await this.directLineClient.Conversations.StartConversationAsync();

            var response = this.Request.CreateResponse(HttpStatusCode.OK);
            response.Content = new StringContent(conversation.ConversationId);
            response.Content.Headers.ContentType = new MediaTypeHeaderValue("text/plain");

            return response;
        }

        [HttpPost]
        [Route("{conversationId}")]
        public async Task<HttpResponseMessage> SendVoiceCommand(string conversationId, string watermark = null)
        {
            if (string.IsNullOrEmpty(conversationId))
            {
                throw new ArgumentNullException(nameof(conversationId));
            }

            // create an audio content and pass it a stream.
            // Option 1: Web Socket
            /*
            using (Stream audio = await Request.Content.ReadAsStreamAsync())
            {
                var deviceMetadata = new DeviceMetadata(DeviceType.Near, DeviceFamily.Desktop, NetworkType.Wifi, OsName.Windows, "N/A", "N/A", "N/A");
                var applicationMetadata = new ApplicationMetadata("SampleApp", "1.0.0");
                var requestMetadata = new RequestMetadata(Guid.NewGuid(), deviceMetadata, applicationMetadata, "SampleAppService");

                await speechClient.RecognizeAsync(new SpeechInput(audio, requestMetadata), this.cts.Token).ConfigureAwait(false);
            }
            */

            // Option 2: REST API
            // Convert speech to text
            this.commandText = null;
            using (Stream audio = await Request.Content.ReadAsStreamAsync())
            {
                this.commandText = await ConvertSpeechToTextAsync(audio);
            }

            // Send text message to bot service
            if (!string.IsNullOrEmpty(this.commandText))
            {
                Activity userMessage = new Activity
                {
                    From = new ChannelAccount(FromUserId),
                    Text = this.commandText,
                    Type = ActivityTypes.Message
                };

                await directLineClient.Conversations.PostActivityAsync(conversationId, userMessage);
                var botResult = await ReceiveBotMessagesAsync(this.directLineClient, conversationId, watermark);

                PostVoiceCommandResponse botResponse = new PostVoiceCommandResponse
                {
                    Command = this.commandText,
                    Text = botResult.Text,
                    Watermark = botResult.Watermark
                };

                // Convert text to speech
                HttpResponseMessage response = this.Request.CreateResponse(HttpStatusCode.OK);
                MemoryStream outStream = new MemoryStream();
                //response.Content = new StringContent(JsonConvert.SerializeObject(botResponse));

                if (botResponse.Text.Contains("Music.Play"))
                {
                    outStream = (MemoryStream)SampleMusic.GetStream();
                }
                else
                {
                    this.ttsClient.OnAudioAvailable += (sender, stream) =>
                    {
                        WaveFormat target = new WaveFormat(8000, 16, 2);
                        using (WaveFormatConversionStream conversionStream = new WaveFormatConversionStream(target, new WaveFileReader(stream)))
                        {
                            WaveFileWriter.WriteWavFileToStream(outStream, conversionStream);
                            outStream.Position = 0;
                        }

                        stream.Dispose();
                    };

                    await ttsClient.SynthesizeTextAsync(botResponse.Text, CancellationToken.None);
                }                

                response.Content = new StreamContent(outStream);
                response.Content.Headers.ContentLength = outStream.Length;
                response.Content.Headers.ContentType = new MediaTypeHeaderValue("audio/x-wav");

                return response;
            }
            else
            {
                throw new InvalidOperationException("Voice command cannot be recognized.");
            }
        }

        [HttpPost]
        [Route("test")]
        public async Task<HttpResponseMessage> Test()
        {
            // Convert text to speech
            HttpResponseMessage response = this.Request.CreateResponse(HttpStatusCode.OK);

            MemoryStream outStream = new MemoryStream();
            /*
            this.ttsClient.OnAudioAvailable += (sender, stream) =>
            {
                WaveFormat target = new WaveFormat(8000, 16, 2);
                using (WaveFormatConversionStream conversionStream = new WaveFormatConversionStream(target, new WaveFileReader(stream)))
                {
                    WaveFileWriter.WriteWavFileToStream(outStream, conversionStream);
                    outStream.Position = 0;
                }

                stream.Dispose();
            };

            await ttsClient.SynthesizeTextAsync("Hello", CancellationToken.None);

            response.Content = new StreamContent(outStream);
            response.Content.Headers.ContentLength = outStream.Length;
            response.Content.Headers.ContentType = new MediaTypeHeaderValue("audio/x-wav");
            */

           
            string message = System.Web.Hosting.HostingEnvironment.MapPath("~/musicsample.wav");
            response.Content = new StringContent($"Current dir: {message}");

            return response;

        }

        /// <summary>
        /// Invoked when the speech client receives a phrase recognition result(s) from the server.
        /// </summary>
        /// <param name="args">The recognition result.</param>
        /// <returns>
        /// A task
        /// </returns>
        private Task OnRecognitionResult(RecognitionResult args)
        {
            var response = args;

            if (response.RecognitionStatus == RecognitionStatus.Success)
            {
                this.commandText = response.Phrases[0].DisplayText;
            }

            return this.completedTask;
        }

        public async Task<string> ConvertSpeechToTextAsync(Stream contentStream)
        {
            string requestUri = @"https://speech.platform.bing.com/speech/recognition/interactive/cognitiveservices/v1?language=en-US";

            using (var client = new HttpClient())
            {
                CognitiveTokenProvider tokenProvider = new CognitiveTokenProvider(CognitiveSubscriptionKey);
                string token = await tokenProvider.GetAuthorizationTokenAsync();

                client.DefaultRequestHeaders.Add("Authorization", "Bearer " + token);
                client.DefaultRequestHeaders.TryAddWithoutValidation("Content-type", @"audio/wav; codec=""audio/pcm""; samplerate=8000");
                client.DefaultRequestHeaders.TryAddWithoutValidation("Accept", "application/json;text/xml");
                client.DefaultRequestHeaders.TryAddWithoutValidation("Host", "speech.platform.bing.com");
                client.DefaultRequestHeaders.TryAddWithoutValidation("Transfer-Encoding", "chunked");
                client.DefaultRequestHeaders.TryAddWithoutValidation("Expect", "100-continue");

                using (var binaryContent = new StreamContent(contentStream))
                {
                    var response = await client.PostAsync(requestUri, binaryContent);
                    var responseString = await response.Content.ReadAsStringAsync();

                    try
                    {
                        SpeechRecognitionResult result = JsonConvert.DeserializeObject<SpeechRecognitionResult>(responseString);
                        return result.DisplayText;
                    }
                    catch (JsonReaderException ex)
                    {
                        throw new Exception(responseString, ex);
                    }
                }
            }
        }

        private async Task<BotMessage> ReceiveBotMessagesAsync(DirectLineClient client, string conversationId, string watermark)
        {
            bool messageReceived = false;
            BotMessage result = new BotMessage();

            while (!messageReceived)
            {
                var activitySet = await client.Conversations.GetActivitiesAsync(conversationId, watermark);
                result.Watermark = activitySet?.Watermark;

                var activities = from x in activitySet.Activities
                                 where x.From.Id == BotId
                                 select x;

                if (activities.Count() > 0)
                {
                    messageReceived = true;
                }

                /*
                foreach (Activity activity in activities)
                {
                    result.Text += activity.Text;
                }
                */

                // ONLY return the latest message from bot service here
                result.Text = activities.Last().Text;

                await Task.Delay(TimeSpan.FromSeconds(1)).ConfigureAwait(false);
            }

            return result;
        }
    }
}
