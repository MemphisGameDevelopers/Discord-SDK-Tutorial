using Discord.Sdk;
using System.Linq;
using TMPro;
using Unity.Services.Authentication;
using UnityEngine;
using UnityEngine.UI;

public class DiscordManager : MonoBehaviour
{
    public Client client;
    public ulong clientId = 1221942619483934801;//Discord App Id
    public Button loginButton;
    public TMP_Text statusText;
    public LoggingSeverity logLevel;

    
    string codeVerifier;
    Activity activity;

    private void Awake()
    {
      
    }
     async void Start()
    {
        client = new Client();
        client.AddLogCallback(OnLog, logLevel);
        client.SetStatusChangedCallback(OnStatusChanged);
        client.SetTokenExpirationCallback(OnTokenExpiration);

        loginButton.onClick.AddListener(StartOAuthFlow);

        statusText.text = "Ready to login";

        while (!AuthenticationService.Instance.IsSignedIn)
        {
            await Awaitable.NextFrameAsync();
        }
        ProvisionalAuthUser();

    }

    bool IsLoggedIn()
    {
        var token = PlayerPrefs.GetString("DiscordToken", string.Empty);
        if (!string.IsNullOrEmpty(token))
        {
            print("Already Signed into Discord...");
            client.UpdateToken(AuthorizationTokenType.Bearer, token, (ClientResult result) => { client.Connect(); });
            return true;
        }
        return false;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void ProvisionalAuthUser()
    {
        if(IsLoggedIn()) return;

        var unityToken = AuthenticationService.Instance.AccessToken;
       
        client.GetProvisionalToken(clientId, AuthenticationExternalAuthType.UnityServicesIdToken, unityToken, exchangeCallback);

        statusText.text = $"Signed in as Provisional user: {client.GetCurrentUser().DisplayName()}";

    }

    private void StartOAuthFlow()
    {
        if (IsLoggedIn()) return;

        var authorizationVerifier = client.CreateAuthorizationCodeVerifier();
        codeVerifier = authorizationVerifier.Verifier();

        var args = new AuthorizationArgs();
        args.SetClientId(clientId);
        args.SetScopes(Client.GetDefaultCommunicationScopes());
        args.SetCodeChallenge(authorizationVerifier.Challenge());
               
        client.Authorize(args, OnAuthorizeResult);
    }

    private void OnAuthorizeResult(ClientResult result, string code, string redirectUri)
    {
        Debug.Log($"Authorization result: [{result.Error()}] [{code}] [{redirectUri}]");
        if (!result.Successful())
        {
            return;
        }
        GetTokenFromCode(code, redirectUri);
    }

    private void GetTokenFromCode(string code,string redirectUri)
    {
        if (client.GetCurrentUser().IsProvisional())
        {
            client.GetTokenFromProvisionalMerge(clientId, code, codeVerifier, redirectUri, AuthenticationExternalAuthType.UnityServicesIdToken, AuthenticationService.Instance.AccessToken, exchangeCallback);
        }
        else
        {
            client.GetToken(clientId, code, codeVerifier, redirectUri, exchangeCallback);
        }
    }
           

    private void OnReceivedToken(string token)
    {
        Debug.Log("Token received: " + token);
        client.UpdateToken(AuthorizationTokenType.Bearer, token, (ClientResult result) => { client.Connect(); });
    }

    void ClientReady()
    {
        print($"Friend Count: {client.GetRelationships().Count()}");
        foreach (var friend in client.GetRelationships()) 
        {
            //print(friend.User().DisplayName());
        }
        activity = new Activity();
        activity.SetType(ActivityTypes.Playing);
        activity.SetState("Mucking about in Unity");
        activity.SetDetails("Testing new Discord Social SDK in Unity");
        client.UpdateRichPresence(activity, (ClientResult result) => {
            if (result.Successful())
            {
                Debug.Log("Rich presence updated!");
            }
            else
            {
                Debug.LogError("Failed to update rich presence");
            }
        });
               
    }

    void OnLog(string message, LoggingSeverity severity)
    {
        print($"Log: {severity} - {message}");
    }

    void OnStatusChanged(Client.Status status, Client.Error error, int errorCode)
    {
        print($"Status changed: {status}");
        statusText.text = status.ToString();
        if (error != Client.Error.None)
        {
            Debug.LogError($"Error: {error}, code: {errorCode}");
        }

        if (status == Client.Status.Ready)
        {
            ClientReady();
        }
    }

    void OnTokenExpiration()
    {
        client.RefreshToken(clientId, PlayerPrefs.GetString("DiscordRefresh"), exchangeCallback);
    }
    void exchangeCallback(ClientResult result, string token, string refreshToken, AuthorizationTokenType tokenType, int expiresIn, string scope)
    {
        if (!string.IsNullOrEmpty(token))
        {
            OnReceivedToken(token);
            PlayerPrefs.SetString("DiscordToken", token);
            PlayerPrefs.SetString("DiscordRefresh", refreshToken);
        }
        else
        {
            statusText.text = "Failed to retrieve token";
            PlayerPrefs.SetString("DiscordToken", string.Empty);
            PlayerPrefs.SetString("DiscordRefresh", string.Empty);
        }
    }

    [ContextMenu("Clear Player Prefs")]
    void ClearPlayerPrefs()
    {
        PlayerPrefs.DeleteAll();
    }
    private void OnDisable()
    {
        client.ClearRichPresence();
        client.Disconnect();
    }
}
