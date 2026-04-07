using System;
using System.IO;
using System.Net.Http;
using Steamworks;
using UnityEngine;

namespace LCVR.Managers;

/// <summary>
/// Automatically share log files in dev builds
/// </summary>
public class LogSharingManager : MonoBehaviour
{
    private static LogSharingManager _instance;
    
    private AuthTicket authTicket;
    
    private void Awake()
    {
        if (_instance != null)
        {
            Destroy(gameObject);
            return;
        }
        
#if DEBUG
        _instance = this;
        DontDestroyOnLoad(this);

        authTicket = SteamUser.GetAuthSessionTicket();
        
        Application.quitting += OnGameClosing;
#else
        Destroy(gameObject);
#endif
    }

    private void OnGameClosing()
    {
        var authData = $"{SteamClient.SteamId.Value}.{Convert.ToBase64String(authTicket.Data)}";
        var client = new HttpClient();
        client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", authData);

        var data = File.Open(Application.consoleLogPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        if (data.Length > 25 * 1024 * 1024)
            return;

        var buffer = new byte[data.Length];
        var read = data.Read(buffer, 0, buffer.Length);
        
        var formData = new MultipartFormDataContent();
        formData.Add(new ByteArrayContent(buffer[..read]), "log");

        Logger.LogInfo("Sending log file to log collector");

        var url = Environment.GetEnvironmentVariable("LCVR_LOGS_UPLOAD_URL") ?? "https://lcvr-logs.daxcess.io/upload";
        _ = client.PostAsync(url, formData).Result;
    }
}