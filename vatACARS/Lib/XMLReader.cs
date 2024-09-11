using System;
using System.IO;
using System.Xml.Serialization;
using vatACARS.Util;

namespace vatACARS.Lib
{
    public static class XMLReader
    {
        public static XMLInterface uplinks;
        private static string dirPath = $"{Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}\\vatACARS";
        private static Logger logger = new Logger("XMLReader");

        public static void MakeUplinks()
        {
            try
            {
                logger.Log("Reading uplinks...");
                // Default uplinks
                string uplinksRaw = File.ReadAllText($"{dirPath}\\data\\uplinks.xml");
                if (Properties.Settings.Default.p_uplinkfile == null)
                {
                    logger.Log("No profile loaded. Using Default Uplinks");
                    return;
                }
                else
                {
                    uplinksRaw = File.ReadAllText(Properties.Settings.Default.p_uplinkfile);
                }

                logger.Log("Deserializing...");
                using (TextReader reader = new StringReader(uplinksRaw))
                {
                    XmlSerializer serializer = new XmlSerializer(typeof(XMLInterface));
                    uplinks = serializer.Deserialize(reader) as XMLInterface;
                }
                logger.Log("Done!");
            }
            catch (Exception ex)
            {
                logger.Log($"Something went wrong!\n{ex.ToString()}");
            }
        }
    }
}