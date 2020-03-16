using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;

namespace CsGo_Server
{
    class Program
    {
        static void Main(string[] args)
        {        

            //the list of all servers created
            List<Process> servers = new List<Process>();

            //main loop 
            //first run we check if the server needs to update
            while (true)
            {
                //retives the location of the Steam cmd
                var serverLocation = GetSteamCmd();

                //setup a wait for all servers to closed 
                foreach (var server in servers)
                {
                    if (server == null)
                        continue;
                    server.WaitForExit();
                }

                //make sure that all servers are closed and ready for update
                foreach (var server in servers)
                {
                    if (server == null)
                        continue;
                    server.Close();
                }

                //clear list since we don't need the information from the closed servers
                servers = new List<Process>();

                //update all server files
                UpdateClient(serverLocation);

                //setup all servers with the information from the external file
                foreach (var item in GetServerSettings())
                {
                    servers.Add(StartClient(item, serverLocation));
                }

                //trigger GC 
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
                
        }

        /// <summary>
        /// Opens a CS GO server using the information given
        /// </summary>
        /// <param name="gameSettings">Tell what kind of server we need to create</param>
        /// <param name="steamcmd">The location of the steam server</param>
        /// <returns>Returns an instance of the server</returns>
        public static Process StartClient(string gameSettings, string steamcmd)
        {
            Process CSStart = new Process();
            //opens the steam cmd from the server location 
            try
            {
                CSStart.StartInfo.FileName = steamcmd + @"\steamapps\common\Counter-Strike Global Offensive Beta - Dedicated Server\srcds.exe";
                CSStart.StartInfo.WindowStyle = ProcessWindowStyle.Normal;
                //use the gamesettings to open the server with the right information
                CSStart.StartInfo.Arguments = gameSettings;
                //setup asynchronous console output
                CSStart.StartInfo.RedirectStandardOutput = true;
                CSStart.StartInfo.RedirectStandardError = true;
                //* Set output and error (asynchronous) handlers
                CSStart.OutputDataReceived += new DataReceivedEventHandler(OutputHandler);
                CSStart.ErrorDataReceived += new DataReceivedEventHandler(OutputHandler);
                //starts the server and tell it to output to our console
                CSStart.Start();
                CSStart.BeginOutputReadLine();
                CSStart.BeginErrorReadLine();
                //retrun the instance of the server
                return CSStart;
            }
            catch (Exception)
            {
                Console.WriteLine("Couldn't start server from steam cmd location or server settings");
                Console.WriteLine("Please check that the files are setup corretly");
                Console.ReadLine();
            }
            return null;
        }

        /// <summary>
        /// Reads the output from the external consoles
        /// </summary>
        /// <param name="sendingProcess"></param>
        /// <param name="outLine"></param>
        public static void OutputHandler(object sendingProcess, DataReceivedEventArgs outLine)
        {
            //using an empty try catch to prevent error when process closes
            try
            {
                //print all the output from the consoles in this console
                Console.WriteLine(outLine.Data);

                //check if the server asks for an update if so close all servers
                if (outLine.Data.Equals("Your server is out of date.  Please update and restart."))
                    StopClient();
            }
            catch { }
        }

        /// <summary>
        /// Closes all instances of cs go servers
        /// </summary>
        public static void StopClient()
        {

            using Process CSStop = new Process();
            //opens cmd to enter the kill server command
            try
            {
                CSStop.StartInfo.FileName = "cmd.exe";
                CSStop.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                CSStop.StartInfo.Arguments = "cmd.exe" + String.Format("/k {0} & {1}", "TASKKILL /IM srcds.exe", "exit");
                CSStop.Start();
            }
            catch (Exception)
            {
                throw;

            }
        }

        /// <summary>
        /// Opens the steam cmd to update all server information
        /// </summary>
        public static void UpdateClient(string serverLocation)
        {
            using Process CSUpdate = new Process();
            //opens the steam cmd from the server location to update the server
            try
            {
                CSUpdate.StartInfo.FileName = serverLocation + @"\steamcmd.exe";
                CSUpdate.StartInfo.WindowStyle = ProcessWindowStyle.Normal;
                CSUpdate.StartInfo.Arguments = "+login anonymous +app_update 740 +quit";
                CSUpdate.Start();
                CSUpdate.WaitForExit();
            }
            catch (Exception)
            {
                Console.WriteLine("Couldn't update server from steam cmd location");
                Console.WriteLine("Please update the location in the Steam_Server_Location file");
                Console.ReadLine();
            }
        }

        /// <summary>
        /// Reads from a file where the steam cmd is located
        /// </summary>
        /// <returns>returns a string with the location</returns>
        public static string GetSteamCmd()
        {
            //create the location that the file should be in
            var fileLocation = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location) + @"\Steam_Server_Location.txt";

            //check if the file already exists or is empty if then create the file
            if (!File.Exists(fileLocation) || new FileInfo(fileLocation).Length <= 0)
                File.WriteAllText(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location) + @"\Steam_Server_Location.txt", @"C:\SteamServers\Server");

            //return the text from the file
            return File.ReadAllText(fileLocation);
        }

        /// <summary>
        /// read from a file all the server setiings we need to setup
        /// </summary>
        /// <returns>retuns a list with all the settings</returns>
        public static List<string> GetServerSettings()
        {
            //create the location that the file should be in
            var fileLocation = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location) + @"\CSGO_Server_settings.txt";

            //check if the file already exists or is empty if then create the file
            if (!File.Exists(fileLocation) || new FileInfo(fileLocation).Length <= 0)
                File.WriteAllText(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location) + @"\CSGO_Server_settings.txt", 
                    $"-game csgo -console -usercon -tickrate 128 -maxplayers_override 10 +game_type 0 +game_mode 1 +mapgroup mg_active +map de_inferno{Environment.NewLine}" +
                    $"-game csgo -console -usercon -tickrate 128 -maxplayers_override 10 +game_type 0 +game_mode 1 +mapgroup mg_active +map de_dust2");

            var text = File.ReadAllText(fileLocation);

            //return the text from the file
            return text.Split(Environment.NewLine).ToList();
        }

    }
}
