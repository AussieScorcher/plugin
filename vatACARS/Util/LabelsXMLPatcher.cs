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

                // Select all Label elements
                XmlNodeList labels = doc.SelectNodes("//Label");
                int patchedCount = 0;

                foreach (XmlElement label in labels)
                {
                    // Check if the label is not of type 'Limited' or 'Quicktag'
                    if (label.GetAttribute("Type") != "Limited" && label.GetAttribute("Type") != "Quicktag")
                    {
                        // Select all Item elements of type 'LABEL_ITEM_ACID' in this label
                        XmlNodeList acidItems = label.SelectNodes(".//Item[@Type='LABEL_ITEM_ACID']");
                        foreach (XmlElement acidItem in acidItems)
                        {
                            acidItem.SetAttribute("Type", "LABEL_ITEM_ACARS_ACID");
                            acidItem.SetAttribute("LeaderLineAnchor", "True");
                            patchedCount++;
                        }

                        XmlNodeList cpdlcItems = label.SelectNodes(".//Item[@Type='LABEL_ITEM_CPDLC']");
                        foreach (XmlElement cpdlcItem in cpdlcItems)
                        {
                            cpdlcItem.SetAttribute("Type", "LABEL_ITEM_ACARS_CPDLC");
                            cpdlcItem.SetAttribute("LeftClick", "");
                            cpdlcItem.SetAttribute("RightClick", "");
                            cpdlcItem.SetAttribute("BackgroundColour", "");
                            patchedCount++;
                        }

                    }
                }

                if (patchedCount <= 0)
                {
                    logger.Log("No acid items found or patched");
                }
                else
                {
                    logger.Log($"Patched {patchedCount} items");
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