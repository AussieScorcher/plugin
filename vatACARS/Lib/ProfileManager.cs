using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using vatACARS.Util;

namespace vatACARS.Lib
{
    public class ProfileManager
    {
        private static string dirPath = $"{Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}\\vatACARS";
        private static Logger logger = new Logger("ProfileManager");
        public static List<Profile> Profiles { get; set; } = new List<Profile>();
        public static event EventHandler ProfileSet;

        public static void SetProfile(Profile profile)
        {
            logger.Log($"Setting profile: {profile.Name}");
            string configPath = $"{dirPath}\\profiles\\{profile.Name}.config";
            string uplinkPath = $"{dirPath}\\profiles\\{profile.Name}.xml";
            bool bracket = profile.CallsignBracket;
            bool pdc = profile.PDC;
            bool autoLogon = profile.AutoLogon;
            string atsus = string.Join(",", profile.HandoverATSU);
            int autoLogonLevel = profile.AutoLogonLevel;
            logger.Log($"Config file: {configPath}");
            logger.Log($"Uplink file: {uplinkPath}");
            Properties.Settings.Default.p_configfile = configPath;
            Properties.Settings.Default.p_uplinkfile = uplinkPath;
            Properties.Settings.Default.p_callsignbracket = bracket;
            Properties.Settings.Default.p_pdc = pdc;
            Properties.Settings.Default.p_autologon = autoLogon;
            Properties.Settings.Default.p_handoveratsus = atsus;
            Properties.Settings.Default.p_loadedprofile = profile.Name;
            Properties.Settings.Default.p_autologonlevel = autoLogonLevel;
            Properties.Settings.Default.Save();
            logger.Log("Profile set successfully.");
            logger.Log($"Callsign Bracket: {bracket}");
            logger.Log($"PDC: {pdc}");
            logger.Log($"AutoLogon: {autoLogon}");
            logger.Log($"AutoLogon Level: {autoLogonLevel}");
            logger.Log($"Handover ATSUs: {atsus}");
            ProfileSet?.Invoke(null, EventArgs.Empty);
            XMLReader.MakeUplinks();
        }

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
                    ErrorHandler.GetInstance().AddError("No profiles were found in the folder.");
                    SetDefaultProfile();
                    return;
                }

                foreach (string file in profileFiles)
                {
                    string profileName = Path.GetFileNameWithoutExtension(file);
                    Profile profile = new Profile { Name = profileName };

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
                            if (trimmedLine.StartsWith("[", StringComparison.OrdinalIgnoreCase) &&
                                trimmedLine.EndsWith("]", StringComparison.OrdinalIgnoreCase))
                            {
                                section = trimmedLine.Trim('[', ']').Trim();
                                continue;
                            }
                            if (section == "GeneralSettings")
                            {
                                if (trimmedLine.StartsWith("CallsignBracket", StringComparison.OrdinalIgnoreCase))
                                {
                                    bool callsignBracketValue = bool.Parse(trimmedLine.Split('=')[1].Trim().ToLower());
                                    profile.CallsignBracket = callsignBracketValue;
                                }
                                else if (trimmedLine.StartsWith("PDC", StringComparison.OrdinalIgnoreCase))
                                {
                                    bool pdcValue = bool.Parse(trimmedLine.Split('=')[1].Trim().ToLower());
                                    profile.PDC = pdcValue;
                                }
                                else if (trimmedLine.StartsWith("AutoLogonLevel", StringComparison.OrdinalIgnoreCase))
                                {
                                    int autoLogonLevelValue;
                                    if (int.TryParse(trimmedLine.Split('=')[1].Trim(), out autoLogonLevelValue))
                                    {
                                        profile.AutoLogonLevel = autoLogonLevelValue;
                                    }
                                    else
                                    {
                                        profile.AutoLogonLevel = 0;
                                    }
                                }
                                else if (trimmedLine.StartsWith("AutoLogon", StringComparison.OrdinalIgnoreCase))
                                {
                                    bool autoLogonValue = bool.Parse(trimmedLine.Split('=')[1].Trim().ToLower());
                                    profile.AutoLogon = autoLogonValue;
                                }
                            }
                            else if (section == "HandoverATSU")
                            {
                                if (trimmedLine.StartsWith("ATSUs", StringComparison.OrdinalIgnoreCase))
                                {
                                    profile.HandoverATSU = new List<string>(trimmedLine.Split('=')[1].Trim().Split(',').Select(atsu => atsu.Trim()));
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

            if (Properties.Settings.Default.p_loadedprofile != null)
            {
                Profile loadedProfile = Profiles.Find(p => p.Name == Properties.Settings.Default.p_loadedprofile);
                if (loadedProfile != null)
                {
                    SetProfile(loadedProfile);
                }
                else
                {
                    if (Properties.Settings.Default.p_loadedprofile == null)
                    {
                        logger.Log("No loaded profile found.");
                        SetDefaultProfile();
                    }
                    else 
                    { 
                    ErrorHandler.GetInstance().AddError($"Loaded Profile not found {Properties.Settings.Default.p_loadedprofile}");
                    logger.Log("Loaded profile not found.");
                    SetDefaultProfile();
                    }
                }
            }

            logger.Log($"Loaded {Profiles.Count} profiles.");
        }
        private static void SetDefaultProfile()
        {
            if (Profiles.Count > 0)
            {
                if (Profiles.Any(p => p.Name == "VATPAC"))
                {
                    SetProfile(Profiles.Find(p => p.Name == "VATPAC"));
                }
                else 
                {
                    SetProfile(Profiles[0]);
                }
            }
            else
            {
                ErrorHandler.GetInstance().AddError("No profiles found to set as default.");
                logger.Log("No profiles found to set as default.");
                Properties.Settings.Default.p_loadedprofile = null;
            }
        }
    }

    public class Profile
    {
        public string Name { get; set; }
        public bool CallsignBracket { get; set; }
        public List<string> HandoverATSU { get; set; }
        public bool PDC { get; set; }
        public bool AutoLogon { get; set; }
        public int AutoLogonLevel { get; set; }
    }
}