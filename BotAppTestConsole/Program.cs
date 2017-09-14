using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Media;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using DemoBotApp;
using NAudio.Wave;

namespace BotAppTestConsole
{
    class Program
    {
        private static readonly string DirectLineSecret = "41bYaJO2zw4.cwA.aLs.d5q5HkxjIEGjy8GnYDBjrmdeI1g_e23j7_ABryHdPTA";
        private static readonly string BotId = "doltravelbot";
        private static readonly string FromUserId = "TestUser";

        private static readonly string SpeechLocale = "en-US";
        private static readonly Uri ShortPhraseUrl = new Uri(@"wss://speech.platform.bing.com/api/service/recognition");
        private static readonly Uri LongDictationUrl = new Uri(@"wss://speech.platform.bing.com/api/service/recognition/continuous");
        private static readonly Uri SpeechSynthesisUrl = new Uri("https://speech.platform.bing.com/synthesize");
        private static readonly string CognitiveSubscriptionKey = "0dacf765a706415da303f1615cd467a9";

        private static string audioFile = @"C:\IoT\Voice\STTTest.wav";
        private readonly CancellationTokenSource cts = new CancellationTokenSource();
        private static readonly Task CompletedTask = Task.FromResult(true);

        private static Stopwatch watch = new Stopwatch();

        static void Main(string[] args)
        {
            DevKitDemoAppTest().Wait();
        }

        private static async Task DevKitDemoAppTest()
        {
            using (var httpclient = new HttpClient())
            {
                string wavFilePath = @"C:\\IoT\\Voice\\TTSResult-3.wav";
                using (var binaryContent = new StreamContent(File.OpenRead(@"C:\\IoT\\Voice\\STTTest-2.wav")))
                {
                    //var response = await httpclient.PostAsync("http://devkitdemobotapp.azurewebsites.net/conversation/JbgF8Sr8VxI2w4sGWM0iab", binaryContent);
                    var response = await httpclient.PostAsync("http://devkitdemobotapp.azurewebsites.net/conversation/test", null);

                    var stream = await response.Content.ReadAsStreamAsync();
                    using (FileStream fs = new FileStream(wavFilePath, FileMode.Create))
                    {
                        stream.CopyTo(fs);
                        stream.Position = 0;
                    }

                    var int16Data = new List<Int16>();
                    using (var reader = new BinaryReader(new FileStream(wavFilePath, FileMode.Open)))
                    {
                        try
                        {
                            while (true)
                            {
                                int16Data.Add(reader.ReadInt16());
                            }
                        }
                        catch
                        {
                        }
                    }

                    var int16Array = int16Data.ToArray();
                    using (var sw = new StreamWriter(@"C:\IoT\Voice\TTSResult-3.txt"))
                    {
                        for (int i = 0; i < int16Array.Length; i++)
                        {
                            if (i % 40 == 0)
                            {
                                sw.WriteLine();
                            }
                            sw.Write($"{int16Array[i].ToString()},");
                        }
                    }

                    
                    SoundPlayer player = new SoundPlayer(stream);
                    player.PlaySync();

                    stream.Dispose();
                }
            }
        }

        private static byte[] StreamToBytes(Stream input)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                input.CopyTo(ms);
                byte[] bytes = ms.ToArray();
                return bytes;
            }
        }

    }
}
