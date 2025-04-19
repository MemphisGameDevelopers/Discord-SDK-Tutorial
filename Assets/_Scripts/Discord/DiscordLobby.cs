using Discord.Sdk;
using Newtonsoft.Json;
using System.Linq;
using Unity.Services.Authentication;
using UnityEngine;
using UnityEngine.Networking;

public class DiscordLobby : MonoBehaviour
{
    Client client;
    Call activeCall;
    ulong lobbyId = ulong.MaxValue;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        client = GetComponent<DiscordManager>().client;

    }

    // Update is called once per frame
    void Update()
    {
        client = GetComponent<DiscordManager>().client;
        if (client?.GetStatus() == Client.Status.Ready && lobbyId == ulong.MaxValue)
        {
            client.SetInputDevice("default", (result) => { });
            client.SetOutputDevice("default", (result) => { });
            lobbyId = 0;
            CreateorJoinLobby(AuthenticationService.Instance.PlayerId);
            //JoinOrCreateVoiceLobby("VoiceChat", "admin");
            client.SetMessageCreatedCallback(messageId =>
            {
                MessageHandle message = client.GetMessageHandle(messageId);
                print($"Recieved Message: {message.Content()}");
            });

            
        }
    }

    void CreateorJoinLobby(string secret)
    {
        client.CreateOrJoinLobby(secret,(result, lobbyId) =>
        {
            if (result.Successful())
            {
                print($"🎮 Lobby created or joined successfully! Lobby Id: {lobbyId}");
                this.lobbyId = lobbyId;

                UpdateLobbyPlayer(lobbyId);

                client.GetUserGuilds((result,guilds) =>
                {
                    var server = guilds.First(s => s.Name() == "Memphis Game Dev");
                    client.GetGuildChannels(server.Id(), (result, channels) =>
                    {                        var channel = channels.First(c => c.Name() == "general");

                        client.LinkChannelToLobby(lobbyId, channel.Id(), (result) => 
                        {
                            if (result.Successful())
                            {
                                print("Channel Linked!!!");
                                SendLobbyMessage(lobbyId, "Hello To Discord from Unity");
                            }
                            else
                            {
                                SendLobbyMessage(lobbyId, "Hello To Discord from Unity");
                            }
                        });
                    });
                });

                
            }
            else
            {
                this.lobbyId = ulong.MinValue;
                print("❌ Failed to Create Lobby...");
            }

        });
    }

    void SendLobbyMessage(ulong lobbyId,  string message)
    {
        client.SendLobbyMessage(lobbyId, message, (result, messageId) =>
        {
            if (result.Successful())
            {
                print($"📨 Message sent successfully!");
                this.lobbyId = lobbyId;
            }

        });
    }

    void JoinOrCreateVoiceLobby(string voiceSecret, string channelName)
    {
        client.CreateOrJoinLobby(voiceSecret, (result, lobbyId) =>
        {
            if (result.Successful())
            {
                print($"🎤 Voice Lobby created or joined successfully! Lobby Id: {lobbyId}");

                UpdateLobbyPlayer(lobbyId);
                client.GetUserGuilds((result, guilds) =>
                {
                    var server = guilds.First(s => s.Name() == "Memphis Game Dev");
                    client.GetGuildChannels(server.Id(), (result, channels) =>
                    {
                        var channel = channels.First(c => c.Name() == channelName);

                        client.LinkChannelToLobby(lobbyId, channel.Id(), (result) =>
                        {
                            if (result.Successful())
                            {
                                print("Voice Channel Linked!!!");

                                activeCall= client.StartCall(lobbyId);                                

                                if (activeCall == null)
                                {
                                    Debug.Log("Failed to create discord call.");
                                    return;
                                }
                              activeCall.SetStatusChangedCallback((status, error, errDetail) =>
                                {
                                    print($"Call Status: {status}");
                                });
                            }
                        });

                        
                    });
                });

            }
        });
    }

    //This Needs to only run on the Server/Cloud Code
    string botToken = "Discord Bot Token";
    async void UpdateLobbyPlayer(ulong lobbyId)
    {
        string url = $"https://discord.com/api/v10/lobbies/{lobbyId}/members/{client.GetCurrentUser().Id()}";

        string json = JsonConvert.SerializeObject( new{ flags = 1 } );

        var request = UnityWebRequest.Put(url, json);
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Authorization", botToken);
        request.SetRequestHeader("User-Agent", "DiscordBot(https://discord.com/api, 10)");

        await request.SendWebRequest();

        if(request.result != UnityWebRequest.Result.Success)
            print($"{request.error}");
        else 
            print($"Updated User: {request.result}");

    }

    private void OnDisable()
    {
        client.EndCalls(() => { });
        activeCall?.Dispose();
    }

}
