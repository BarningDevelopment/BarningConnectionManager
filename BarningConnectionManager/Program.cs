﻿using System;
using System.Linq;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using System.Xml;
using MbnApi;

namespace BarningConnectionManager
{
    class Program
    {
        public static string DataFileMobiel = "C:/batchfiles/mobielconnect_netsh_VRH/MBNProfile2.xml";
        public static string DataFile = "C:/batchfiles/wlanconnectVRH/wlanconnectWlanprofile.xml";
        public static string Content = "(Empty File)";
        private static string SIMNumber;

        static void ColorText(string Message)
        {
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.BackgroundColor = ConsoleColor.White;
            Console.WriteLine(Message);
            Console.ResetColor();
        }

        private static void ColorTextQuestion(string message)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(message);
            Console.ResetColor();
        }
        static string WlanState()
        {
            var wlans = new Process
            {
                StartInfo =
                {
                    FileName ="netsh.exe",
                    Arguments = "interface show interface name=\"Wi-Fi\" ",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow =true
                }
            };
            wlans.Start();
            var wlanOut = wlans.StandardOutput.ReadToEnd();
            var AdminState = wlanOut.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault(l => l.Contains("Administrative state"));

            return AdminState;
        }

        static void Main(string[] args)
        {
            var process = new Process
            {
                StartInfo =
                      {
                          FileName = "netsh.exe",
                          Arguments = "wlan show interfaces ",
                          UseShellExecute = false,
                          RedirectStandardOutput = true,
                          CreateNoWindow = true
                      }
            };

            process.Start();
            var output = process.StandardOutput.ReadToEnd();

            var MProcess = new Process
            {
                StartInfo =
                      {
                          FileName = "netsh.exe",
                          Arguments = "mbn show interface",
                          UseShellExecute = false,
                          RedirectStandardOutput = true,
                          CreateNoWindow = true
                      }
            };
            MProcess.Start();
            var MOutput = MProcess.StandardOutput.ReadToEnd();

            var profileProcess = new Process
            {
                StartInfo =
                      {
                          FileName = "netsh.exe",
                          Arguments = "mbn show interface BarningMobiel",
                          UseShellExecute = false,
                          RedirectStandardOutput = true,
                          CreateNoWindow = true
                      }
            };
            profileProcess.Start();
            var profileOutput = profileProcess.StandardOutput.ReadToEnd();

            var wlanEnabled = output.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault(l => l.Contains("There is no wireless interface on the system."));
            var wlanState = output.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault(l => l.Contains("State"));
            var mobileState = MOutput.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault(l => l.Contains("State"));
            var mobileEnabled = MOutput.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault(l => l.Contains("There is no Mobile Broadband interface"));
            var mobilePaused = MOutput.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault(l => l.Contains("Provider Name"));

            //get the simcard information
            try
            {
                MbnInterfaceManager mbnInfMgr = new MbnInterfaceManager();
                IMbnInterfaceManager mbnInfMgrInterface = mbnInfMgr as IMbnInterfaceManager;
                if (mbnInfMgrInterface != null)
                {
                    IMbnInterface[] mobileInterfaces = mbnInfMgrInterface.GetInterfaces() as IMbnInterface[];
                    if (mobileInterfaces != null && mobileInterfaces.Length > 0)
                    {
                        // Just use the first interface
                        IMbnSubscriberInformation subInfo = mobileInterfaces[0].GetSubscriberInformation();

                        Console.WriteLine(subInfo);

                        if (subInfo != null)
                        {
                            SIMNumber = subInfo.SimIccID;

                            Console.WriteLine(SIMNumber);
                            // Get the connection profile
                            MbnConnectionProfileManager mbnConnProfileMgr = new MbnConnectionProfileManager();
                            IMbnConnectionProfileManager mbnConnProfileMgrInterface = mbnConnProfileMgr as IMbnConnectionProfileManager;
                            if (mbnConnProfileMgrInterface != null)
                            {
                                bool connProfileFound = false;
                                string profileName = String.Empty;
                                Console.WriteLine(profileOutput);
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                // Console.WriteLine(e);
            }

            //check if there is a mobile device
            if (!MOutput.Contains("Mobile Broadband Service (wwansvc) is not running."))
            {
                //if there is a mobile interface enabled
                if (!MOutput.Contains("There is no Mobile Broadband interface"))
                {
                    //check connection status
                    if (mobileState != null)
                    {
                        //check if sim is already known bij the system
                        //compare the device id from the system with the simsccid(emei) of the profile

                        //get the value from the string
                        string deviceIdstring = Regex.Match(SIMNumber, @"\d+").Value;
                        //convert to decimal becouse the value is more than the int.max number(9digits)
                        decimal deviceIdInt = 0;
                        //parse
                        deviceIdInt = Decimal.Parse(deviceIdstring);
                        //do the same with the xml number
                        decimal xml_subscriberIdInt = 0;
                        xml_subscriberIdInt = decimal.Parse(xml_subscriberId());
                        //if there is a match the continue else create the profile                          
                        if (deviceIdInt != xml_subscriberIdInt)
                        {
                            ColorTextAlert("simiccid number from mbnapi =" + deviceIdInt + "simicc number from profile =" + xml_subscriberIdInt + " are not the same");
                            createMobileProfile();
                        }
                        else
                        {
                            // ColorTextAlert("deviceid number from netsh =" + deviceIdInt + "Imei number from profile =" + xml_subscriberIdInt + "are the same");
                            //dont change the words
                            if (mobileState.Contains("Not connected"))
                            {
                                ColorTextAlert("Connecting to mobile.......");
                                connectToMobiel(false);
                            }
                            else if (mobileState.Contains("connected"))
                            {
                                ColorText("Connected to mobiel ,you`re welcome. ;-)");
                            }
                        }
                    }
                }
                else if (MOutput.Contains("There is no Mobile Broadband interface"))
                {
                    ColorTextAlert("enable mobiel interface...");
                    enableMobiel();
                }

            }



            //check if there is a wifi connection interface 
            if (output.Contains("There is no wireless interface on the system"))
            {
                enableWiFi();
            }
            else
            {
                try
                {
                    //dont change the words
                    if (wlanState.Contains("disconnected"))
                    {
                        ColorTextAlert("connecting to Wi-Fi......");
                        connectToWifi(false);
                    }
                    else if (wlanState.Contains("connected"))
                    {
                        ColorText("Connected to Wifi ,you re welcome. ;-)");
                    }
                }
                catch (NullReferenceException e)
                {
                    ColorTextAlert("No Wlan interface found on the system" + e);
                }
            }

            filesystemwatcher_keepconnected();
            Console.Read();


        }

        private static string xml_subscriberId()
        {
            //read date from file
            var input = File.ReadAllText(DataFileMobiel);
            //the replacement string
            string replacement = "MBNProfileExt";
            //the regreplacement string
            //http://regexstorm.net/tester
            Regex rgx = new Regex(@"MBNProfileExt\s(\w{5})(\W{4})(\w{4}\W{3}\w{3}\W{1}\w{9}\W{1}\w{3}\W{1}\w{10}\W{1}\w{4}\W{1}\w{7}\W{1}\w{2}\W{1})");
            //replace the input with the one you want
            string result = rgx.Replace(input, replacement);
            //load xml doc
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(result);
            //find the node you want
            XmlNode node = doc.DocumentElement.SelectSingleNode("SimIccID");
            //convert to string
            string text = node.InnerText;
            return text;
        }

        private static void ColorTextAlert(string message)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.BackgroundColor = ConsoleColor.White;
            Console.WriteLine(message);
            Console.ResetColor();
        }
        private static void enableMobiel()
        {
            Process proc = new Process();
            proc.StartInfo.FileName = "C:\\batchfiles\\mobielconnect_netsh_VRH\\connectMobiel.cmd";
            proc.StartInfo.WorkingDirectory = "C:\\batchfiles";
            proc.Start();
            ColorText("mobiel network enabled)");
        }
        private static void connectToMobiel(bool connected)
        {
            if (connected == false)
            {
                if (File.Exists("c:\\batchfiles\\mobielconnect_netsh_VRH\\MBNProfile2.xml"))
                {
                    Process proc = new Process();
                    proc.StartInfo.FileName = "C:\\batchfiles\\mobielconnect_netsh_VRH\\connectMobiel.cmd";
                    proc.StartInfo.WorkingDirectory = "C:\\batchfiles";
                    proc.Start();
                    ColorText("connected to the Mobiel internet, your welcome. ;-)");
                }
                else
                {
                    createMobileProfile();
                }
            }
        }
        private static void createMobileProfile()
        {

            // Connects to a known network with no security
            ColorTextQuestion("put in youre mobile profile name ");
            string profileName = Console.ReadLine();
            string profileNameEncoded = WebUtility.HtmlEncode(profileName);

            ColorTextQuestion("put in you`r IMSI number");
            string DeviceId = Console.ReadLine();

            ColorTextQuestion("put in you`re SimICCD number, enter to confirm");
            string Simiccd = Console.ReadLine();
            //string Simiccd = "8931087115077657088";

            string profileXml = string.Format("<?xml version=\"1.0\"?><MBNProfileExt xmlns = \"http://www.microsoft.com/networking/WWAN/profile/v4\"><Name>{0}</Name><Description>ModemProvisionedProfile##vzwinternet</Description><IsDefault>true</IsDefault><ProfileCreationType>DeviceProvisioned</ProfileCreationType><SubscriberID>{1}</SubscriberID><SimIccID>{2}</SimIccID><HomeProviderName>KPN</HomeProviderName><AutoConnectOnInternet>true</AutoConnectOnInternet><ConnectionMode>auto</ConnectionMode><Context><AccessString>vzwinternet</AccessString><Compression>DISABLE</Compression><AuthProtocol>NONE</AuthProtocol></Context><IsBasedOnModemProvisionedContext xmlns =\"http://www.microsoft.com/networking/WWAN/profile/v7\">true</IsBasedOnModemProvisionedContext></MBNProfileExt>", profileNameEncoded, DeviceId, Simiccd);

            File.WriteAllText(DataFileMobiel, profileXml);
            ColorText("Mobile profile was succesfully updated (enter to confirm)");
        }
        private static void enableWiFi()
        {
            string adaptor_state = WlanState();
            System.Diagnostics.Process proc = new System.Diagnostics.Process();
            proc.StartInfo.FileName = "C:\\batchfiles\\wlanconnectVRH\\installwlan.cmd";
            proc.StartInfo.WorkingDirectory = "C:\\batchfiles";
            proc.Start();
        }
        private static void connectToWifi(bool connected)
        {
            if (connected == false)
            {
                if (File.Exists("C:\\batchfiles\\wlanconnectVRH\\wlanconnectWlanprofile.xml"))
                {
                    System.Diagnostics.Process proc = new System.Diagnostics.Process();
                    proc.StartInfo.FileName = "C:\\batchfiles\\wlanconnectVRH\\installwlan.cmd";
                    proc.StartInfo.WorkingDirectory = "C:\\batchfiles";
                    proc.Start();
                    ColorText("connected to the WIFI internet, your welcome. ;-)");
                }
                else
                {
                    createWifiProfile();
                }
            }
        }
        public static void createWifiProfile()
        {
            // Connects to a known network with hashed password security
            ColorTextQuestion("put in youre SSID name.");
            string profileName = Console.ReadLine();
            string profileNameEncoded = WebUtility.HtmlEncode(profileName);

            ColorTextQuestion("put in youre HEX key.");
            string hex = Console.ReadLine();
            //string hex = "44264D";

            ColorTextQuestion("put in you`re wifi  Password, enter to confirm");
            string key = Console.ReadLine();

            string profileXml = string.Format("<?xml version=\"1.0\"?><WLANProfile xmlns =\"http://www.microsoft.com/networking/WLAN/profile/v1\"><name>{0}</name><SSIDConfig><SSID><hex>{2}</hex><name>{0}</name></SSID></SSIDConfig><connectionType>ESS</connectionType><connectionMode>auto</connectionMode><autoSwitch>false</autoSwitch><MSM><security><authEncryption><authentication>WPA2PSK</authentication><encryption>AES</encryption><useOneX>false</useOneX></authEncryption><sharedKey><keyType>passPhrase</keyType><protected>false</protected><keyMaterial>{1}</keyMaterial></sharedKey></security></MSM><MacRandomization xmlns=\"http://www.microsoft.com/networking/WLAN/profile/v3\"><enableRandomization>false</enableRandomization></MacRandomization></WLANProfile>", profileNameEncoded, key, hex);

            File.WriteAllText(DataFile, profileXml);
            ColorText("Wlan profile was succesfully updated.");
        }
        private static void filesystemwatcher_keepconnected()
        {
            System.Diagnostics.Process proc = new System.Diagnostics.Process();
            proc.StartInfo.FileName = "C:\\batchfiles\\mobielconnect_netsh_VRH\\installmnb_VRH.cmd";
            proc.StartInfo.WorkingDirectory = "C:\\batchfiles";
            proc.Start();
        }
    }
}
