using System;
using System.Collections.Generic;
using System.IO;
using vatACARS.Util;

namespace vatACARS.Lib
{
    public class ProfileManager
    {
        private static string dirPath = $"{Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}\\vatACARS";
        private static Logger logger = new Logger("ProfileManager");
        public static List<Profile> Profiles { get; set; } = new List<Profile>();

        public static void LoadProfiles()
        {
            if (Directory.Exists($"{dirPath}\\profiles"))
            {
                logger.Log("Loading profiles...");

                // Only get files with .config extension
                string[] profileFiles = Directory.GetFiles($"{dirPath}\\profiles", "*.config");

                if (profileFiles.Length == 0)
                {
                    logger.Log("No profile files found in the directory.");
                    return;
                }

                foreach (string file in profileFiles)
                {
                    string profileName = Path.GetFileNameWithoutExtension(file);
                    Profile profile = new Profile { Name = profileName };

                    // Open the file using StreamReader to read line by line
                    using (StreamReader reader = new StreamReader(file))
                    {
                        string line;
                        string section = null;

                        while ((line = reader.ReadLine()) != null)
                        {
                            string trimmedLine = line.Trim();

                            // Ignore empty lines and comments
                            if (string.IsNullOrEmpty(trimmedLine) || trimmedLine.StartsWith("#"))
                            {
                                continue;
                            }

                            // Check for section headers
                            if (trimmedLine.StartsWith("[", StringComparison.OrdinalIgnoreCase) &&
                                trimmedLine.EndsWith("]", StringComparison.OrdinalIgnoreCase))
                            {
                                section = trimmedLine.Trim('[', ']').Trim();
                                continue;
                            }

                            // Parse key-value pairs based on the current section
                            if (section == "GeneralSettings")
                            {
                                if (trimmedLine.StartsWith("CallsignBracket", StringComparison.OrdinalIgnoreCase))
                                {
                                    profile.CallsignBracket = trimmedLine.Split('=')[1].Trim().ToLower() == "true";
                                }
                                else if (trimmedLine.StartsWith("PDC", StringComparison.OrdinalIgnoreCase))
                                {
                                    profile.PDC = trimmedLine.Split('=')[1].Trim().ToLower() == "true";
                                }
                                else if (trimmedLine.StartsWith("AutoAccept", StringComparison.OrdinalIgnoreCase))
                                {
                                    profile.AutoAccept = trimmedLine.Split('=')[1].Trim().ToLower() == "false";
                                }
                            }
                            else if (section == "HandoverATSU")
                            {
                                if (trimmedLine.StartsWith("ATSUs", StringComparison.OrdinalIgnoreCase))
                                {
                                    profile.HandoverATSU = new List<string>(trimmedLine.Split('=')[1].Trim().Split(','));
                                }
                            }
                        }
                    }

                    Profiles.Add(profile);
                    logger.Log($"Loaded profile: {profileName}");
                }
            }
            else
            {
                logger.Log("Profiles directory does not exist, creating...");
                Directory.CreateDirectory($"{dirPath}\\profiles");
            }

            logger.Log($"Loaded {Profiles.Count} profiles.");
        }
    }

    public class Profile
    {
        public string Name { get; set; }
        public bool CallsignBracket { get; set; }
        public List<string> HandoverATSU { get; set; } = new List<string>();
        public bool PDC { get; set; }
        public bool AutoAccept { get; set; }
    }
}