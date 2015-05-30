using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class MatchMaker : MonoBehaviour
{

    public string GameTypeName = "MatchMaker_testGame";
    public int MaxPlayers = 4;
    public int Port = 25000;
    public bool EnableLogging = true;

    private bool _autoRetry;
    private bool _stop = false;

    private readonly List<Action<HostData[]>> _onHostListReceivedActions = new List<Action<HostData[]>>();
    private readonly object _onHostListReceivedActionsLock = new object();

    /// <summary>
    /// Quick host a game with a newly generated GUID as game name
    /// </summary>
    /// <returns>true if a game was created, false if not</returns>
    public bool CreateGame()
    {
        return CreateGame(Guid.NewGuid().ToString());
    }

    /// <summary>
    /// Host a game with the given game name and comment
    /// </summary>
    /// <param name="gameName">The game name</param>
    /// <param name="comment">Comment used to describe the game/host</param>
    /// <returns>true if a game was created, false if not</returns>
    public bool CreateGame(string gameName, string comment=null)
    {
        Log("Creating game..");
        var error = Network.InitializeServer(MaxPlayers - 1, Port, !Network.HavePublicAddress());
        if (error != NetworkConnectionError.NoError)
        {
            Log("Failed to create game.");
            return false;
        }
        if (comment != null)
        {
            MasterServer.RegisterHost(GameTypeName, gameName, comment);
        }
        else
        {
            MasterServer.RegisterHost(GameTypeName, gameName);
        }
        return true;
    }

    private HostData[] GetAvailableHosts()
    {
        MasterServer.RequestHostList(GameTypeName);
        return MasterServer.PollHostList();
    }

    private void GetAvailableHostsAsync(Action<HostData[]> a)
    {
        Log("Requesting host list from master server..");
        MasterServer.RequestHostList(GameTypeName);
        lock (_onHostListReceivedActionsLock)
        {
            _onHostListReceivedActions.Add(a);
        }
    }

    void OnMasterServerEvent(MasterServerEvent e)
    {
        if (e == MasterServerEvent.HostListReceived)
        {
            Log("Received host list from master server.");
            lock (_onHostListReceivedActionsLock)
            {
                var hostData = MasterServer.PollHostList();
                var temp = _onHostListReceivedActions.GetRange(0, _onHostListReceivedActions.Count);
                _onHostListReceivedActions.Clear();
                foreach (var action in temp)
                {
                    action(hostData);
                }
            }
        }
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
    public void JoinGame(HostPreference preference = HostPreference.FirstAvailable, bool autoRetry = true, float retryTimeout = 1f)
    {
        JoinGameHelper(preference, autoRetry, retryTimeout);
    }

    private HostData SelectHost(HostData[] hosts, HostPreference preference)
    {
        Log("Found " + hosts.Length + " host(s).");
        foreach (var host in hosts)
        {
            Log("Checking game [gameName=" + host.gameName + ", connectedPlayers=" + host.connectedPlayers +
                ", comment=" + host.comment + ", playerLimit=" + host.playerLimit + "]..");
            if (host.connectedPlayers < MaxPlayers && preference == HostPreference.FirstAvailable)
            {
                return host;
            }
        }
        return null;
    }

    private void JoinGameHelper(HostPreference preference, bool autoRetry, float retryTimeout, Action<bool> onDone=null)
    {
        if (_stop) return;
        _autoRetry = autoRetry;
        Log("Searching for games..");
        GetAvailableHostsAsync(hosts =>
        {
            var host = SelectHost(hosts, preference);
            if (onDone != null)
            {
                onDone(host != null);
            }
            if (host != null)
            {
                Log("Found suitable game. Joining..");
                StopAutoRetry();
                Network.Connect(host);
            }
            if (autoRetry && _autoRetry)
            {
                StartCoroutine(AutoRetryHelper(preference, retryTimeout));
            }
        });
    }

    private IEnumerator AutoRetryHelper(HostPreference preference, float retryTimeout)
    {
        Log("Auto retry in " + retryTimeout);
        yield return new WaitForSeconds(retryTimeout);
        JoinGameHelper(preference, true, retryTimeout);
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

    /// <summary>
    /// Attempts to join a game, and if no suitable game is found, hosts a new game.
    /// </summary>
    public void JoinOrCreateGame()
    {
        JoinGameHelper(HostPreference.FirstAvailable, false, 0, didJoin =>
        {
            if (didJoin) return;
            Log("Did not find any games.");
            var gameWasCreated = CreateGame();
            if (!gameWasCreated)
            {
                Log("Failed to create game. Perhaps a server is already running on this machine. Trying to join again..");
                JoinGame(); // Note: Will auto retry
            }
        });
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
