using System;
using System.Xml;

namespace vatACARS.Util
{
    public static class LabelsXMLPatcher
    {
        private static string hardcodedFilePath = $"{Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)}\\vatSys Files\\Profiles\\Australia\\Labels.xml"; // TODO: Dynamically yoink this somehow
        private static Logger logger = new Logger("LabelsXMLPatcher");

        public static void Patch()
        {
            logger.Log($"Patching {hardcodedFilePath}");
            try
            {
                XmlDocument doc = new XmlDocument();
                doc.Load(hardcodedFilePath);

                XmlNodeList acidItems = doc.SelectNodes("//Item[@Type='LABEL_ITEM_ACID']");
                foreach (XmlElement acidItem in acidItems)
                {
                    acidItem.SetAttribute("Type", "LABEL_ITEM_ACARS_ACID");
                    acidItem.SetAttribute("LeaderLineAnchor", "True");
                }

                if (acidItems.Count <= 0) 
                {
                    logger.Log("No acid items found");
                }
                else
                {
                    logger.Log($"Patched {acidItems.Count} acid items");
                    ErrorHandler.GetInstance().AddError("vatACARS has updated Labels.xml. Please restart vatSys.");
                }

                doc.Save(hardcodedFilePath);
                logger.Log("Patch complete!");
            }
            catch (Exception e)
            {
                logger.Log($"Oops: {e}");
            }
        }
    }
}