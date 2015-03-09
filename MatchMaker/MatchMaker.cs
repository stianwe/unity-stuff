using System;
using UnityEngine;
using System.Collections;

public class MatchMaker : MonoBehaviour
{

    public string GameTypeName = "MatchMaker_testGame";
    public int MaxPlayers = 4;
    public int Port = 25000;
    public bool EnableLogging = true;

    private bool _autoRetry;
    private bool _stop = false;

    /// <summary>
    /// Quick host a game with a newly generated GUID as game name
    /// </summary>
    public void HostGame()
    {
        HostGame(Guid.NewGuid().ToString());
    }

    /// <summary>
    /// Host a game with the given game name and comment
    /// </summary>
    /// <param name="gameName">The game name</param>
    /// <param name="comment">Comment used to describe the game/host</param>
    public void HostGame(string gameName, string comment=null)
    {
        Log("Hosting game..");
        Network.InitializeServer(MaxPlayers - 1, Port, !Network.HavePublicAddress());
        if (comment != null)
        {
            MasterServer.RegisterHost(GameTypeName, gameName, comment);
        }
        else
        {
            MasterServer.RegisterHost(GameTypeName, gameName);
        }
    }

    private HostData[] GetAvailableHosts()
    {
        MasterServer.RequestHostList(GameTypeName);
        return MasterServer.PollHostList();
    }

    /// <summary>
    /// Join a host.
    /// </summary>
    /// <param name="preference">
    /// Indicates which hosts should be preferred over others
    /// </param>
    /// <param name="autoRetry">
    /// Will periodically auto retry join host until successful or canceled if autoRetry is true
    /// </param>
    /// <param name="retryTimeout">
    /// The time between each auto retry
    /// </param>
    public void JoinHost(HostPreference preference = HostPreference.FirstAvailable, bool autoRetry = true, float retryTimeout = 1f)
    {
        StartCoroutine(JoinHostHelper(preference, autoRetry, retryTimeout));
    }

    private IEnumerator JoinHostHelper(HostPreference preference, bool autoRetry, float retryTimeout)
    {
        if (_stop) yield break;
        _autoRetry = autoRetry;
        Log("Searching for hosts..");
        var hosts = GetAvailableHosts();
        Log("Found " + hosts.Length + " hosts.");
        foreach (var host in hosts)
        {
            Log("Checking host [gameName=" + host.gameName + ", connectedPlayers=" + host.connectedPlayers +
                ", comment=" + host.comment + ", playerLimit=" + host.playerLimit + "]..");
            if (host.connectedPlayers < MaxPlayers && preference == HostPreference.FirstAvailable)
            {
                Log("Found suitable host. Joining..");
                Network.Connect(host);
                _autoRetry = false;
                break;
            }
        }
        if (_autoRetry)
        {
            Log("Auto retry in " + retryTimeout);
            yield return new WaitForSeconds(retryTimeout);
            StartCoroutine(JoinHostHelper(preference, true, retryTimeout));
        }
    }

    /// <summary>
    /// Stops MatchMaker from auto retrying to join again
    /// </summary>
    public void StopAutoRetry()
    {
        Log("Stopping auto retry.");
        _autoRetry = false;
        _stop = true;
    }

    #region EventLogging

    void OnPlayerConnected(NetworkPlayer player)
    {
        Log("Player connected: [IP=" + player.ipAddress + "].");
    }

    void OnPlayerDisconnected(NetworkPlayer player)
    {
        Log("Player disconnected: [IP=" + player.ipAddress + "].");
    }

    void OnServerInitialized()
    {
        Log("Host successfully set up.");
    }

    void OnConnectedToServer()
    {
        Log("Successfully joined host.");
    }

    #endregion

    private void Log(string msg)
    {
        if (EnableLogging)
        {
            Debug.Log("[MatchMaker]: " + msg);
        }
    }

    public enum HostPreference
    {
        FirstAvailable,
        //MostConnectedPlayers,
    }
}
