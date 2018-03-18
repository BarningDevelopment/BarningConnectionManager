using System;
using System.Linq;
using System.Diagnostics;
using System.IO;
using System.Net;

namespace BarningConnectionManager
{
    class Program
    {
            public static string DataFileMobiel = "C:/batchfiles/mobielconnect_netsh_VRH/MBNProfile2.xml";
            public static string DataFile = "C:/batchfiles/wlanconnectVRH/wlanconnectWlanprofile.xml";
            public static string Content = "(Empty File)";
            private static bool profile;

            // Arguments initialization       
            /// <summary>
            /// Converts a 802.11 SSID to a string.
            /// </summary>
            /// 
            static void ColorText(string Message)
            {
                Console.ForegroundColor = ConsoleColor.Blue;
                Console.BackgroundColor = ConsoleColor.White;
                Console.WriteLine(Message);
                Console.ResetColor();
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

                var lanProcess = new Process
                {
                    StartInfo =
                      {
                          FileName = "netsh.exe",
                          Arguments = "interface show interface name=\"Ethernet\" ",
                          UseShellExecute = false,
                          RedirectStandardOutput = true,
                          CreateNoWindow = true
                      }
                };
                lanProcess.Start();
                var lanOutput = lanProcess.StandardOutput.ReadToEnd();

                var MobielProcess = new Process
                {
                    StartInfo =
                      {
                          FileName = "netsh.exe",
                          Arguments = "mbn show interface ",
                          UseShellExecute = false,
                          RedirectStandardOutput = true,
                          CreateNoWindow = true
                      }
                };
                MobielProcess.Start();
                var MobielOutput = MobielProcess.StandardOutput.ReadToEnd();

                var lanState = lanOutput.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault(l => l.Contains("Connect state"));
                var wlanState = output.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault(l => l.Contains("State"));
                var mobielState = MobielOutput.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault(l => l.Contains("State"));


            //als er geen mobiel adaptor loopt
            if (output.Contains("There is no Mobile Broadband interface"))
            {
                createMobileProfile();
                //zet adaptor aan
                enableMobiel();
                return;
            }
            else
            {
                //als de adaptor aan staat
                try
                {
                    //kijk of het profiel aan staat
                    if (!mobielState.Contains("Connected"))
                    {
                        //so profiel is not created make one
                        ColorTextAlert("mobiel not connected");
                        createMobileProfile();
                        connectToMobiel(false);
                    }
                    else if (mobielState.Contains("Connected"))
                    {
                        //connect to profiel   
                        ColorText("Connect everything");
                        filesystemwatcher_keepconnected();
                    }
                }
                catch (NullReferenceException e)
                {
                    ColorTextAlert("No mobile interface found on the system");
                    return;
                }
            }





            //check if there is a wifi connection interface 
            if (output.Contains("There is no wireless interface on the system"))
            {
                //enable wifi interface
                enableWiFi();
                //create profile to make sure
                createWifiProfile();
                return;
            }
            else
            {
                try
                {
                    if (wlanState.Contains("disconnected"))
                    {
                        //so create wlan profiel
                        ColorTextAlert("wifi is not connected");

                        ///createWifiProfile();
                        createWifiProfile();

                        ColorTextAlert("connect to D&M profile");
                        connectToWifi(false);
                    }
                    else if (!wlanState.Contains("disconnected"))
                    {
                        filesystemwatcher_keepconnected();
                    }
                }
                catch (NullReferenceException e)
                {
                    ColorTextAlert("No mobiel interface found on the system");
                    return;
                }
            }

            Console.Read();
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
                System.Diagnostics.Process proc = new System.Diagnostics.Process();
                proc.StartInfo.FileName = "C:\\batchfiles\\mobielconnect_netsh_VRH\\connectMobiel.cmd";
                proc.StartInfo.WorkingDirectory = "C:\\batchfiles";
                proc.Start();
                ColorText("mobiel network enabled)");
            }
        private static void connectToMobiel(bool connected)
            {
                if (connected == false)
                {
                    if (File.Exists("C:\\batchfiles\\mobielconnect_netsh_VRH\\MBNProfile2.xml"))
                    {
                        System.Diagnostics.Process proc = new System.Diagnostics.Process();
                        proc.StartInfo.FileName = "C:\\batchfiles\\mobielconnect_netsh_VRH\\connect.cmd";
                        proc.StartInfo.WorkingDirectory = "C:\\batchfiles";
                        proc.Start();
                        ColorText("connected to the Mobiel internet, your welcome. ;-)");
                    }
               
                }
            }
        private static void createMobileProfile()
            {
                bool connect = false;
                // Connects to a known network with no security
                ColorTextQuestion("put in youre mobile profile name ");
                string profileName = Console.ReadLine();
                string profileNameEncoded = WebUtility.HtmlEncode(profileName);

                Console.WriteLine("put in youre SubscriberID.");
                string SubscriberID = Console.ReadLine();
                //string SubscriberID = "204080806249858";

                Console.WriteLine("put in you`re Simiccd  Password, enter to confirm");
                string Simiccd = Console.ReadLine();
                // string Simiccd = "8931087115077657088";

                string profileXml = string.Format("<?xml version=\"1.0\"?><MBNProfileExt xmlns = \"http://www.microsoft.com/networking/WWAN/profile/v4\"><Name>{0}</Name><Description>ModemProvisionedProfile##vzwinternet</Description><IsDefault>true</IsDefault><ProfileCreationType>DeviceProvisioned</ProfileCreationType><SubscriberID>{1}</SubscriberID><SimIccID>{2}</SimIccID><HomeProviderName>KPN</HomeProviderName><AutoConnectOnInternet>true</AutoConnectOnInternet><ConnectionMode>auto</ConnectionMode><Context><AccessString>vzwinternet</AccessString><Compression>DISABLE</Compression><AuthProtocol>NONE</AuthProtocol></Context><IsBasedOnModemProvisionedContext xmlns =\"http://www.microsoft.com/networking/WWAN/profile/v7\">true</IsBasedOnModemProvisionedContext></MBNProfileExt>", profileNameEncoded, SubscriberID, Simiccd);

                File.WriteAllText(DataFileMobiel, profileXml);
                ColorText("Mobile profile was succesfully updated.");
            }
        private static void ColorTextQuestion(string message)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(message);
            Console.ResetColor();
        }
        private static void enableWiFi()
            {
                System.Diagnostics.Process proc = new System.Diagnostics.Process();
                proc.StartInfo.FileName = "C:\\batchfiles\\wlanconnectVRH\\installwlan.cmd";
                proc.StartInfo.WorkingDirectory = "C:\\batchfiles";
                proc.Start();
                ColorText("WIFI enabled :-)");
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
                        ColorText("connected to the WIFI internet, your welcome. ;=)");
                    }
                }
            }
        public static void createWifiProfile()
            {
                bool connect = false;
                // Connects to a known network with no security
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
