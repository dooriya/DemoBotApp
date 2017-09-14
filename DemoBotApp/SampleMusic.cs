namespace DemoBotApp
{
    using System.IO;
    using System.Web.Hosting;

    public class SampleMusic
    {
        private static MemoryStream audioStream = null;

        static SampleMusic()
        {
            audioStream = new MemoryStream();
            string sampleMusicFilePath = HostingEnvironment.MapPath("~/musicsample.wav");

            if (File.Exists(sampleMusicFilePath))
            {
                using (FileStream fs = new FileStream(sampleMusicFilePath, FileMode.Open))
                {
                    fs.CopyTo(audioStream);
                    audioStream.Position = 0;
                }
            }           
        }

        public static Stream GetStream()
        {
            return audioStream;
        }

        ~SampleMusic()
        {
            audioStream.Dispose();
        }
    }
}