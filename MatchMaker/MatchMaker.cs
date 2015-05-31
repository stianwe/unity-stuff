using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class MatchMaker : MonoBehaviour
{

    public string GameTypeName = "MatchMaker_testGame";
    /// <summary>
    /// MinPlayers indicates the minimum number of players which must be connected
    /// for a JoinOrCreateGame() call to stop looking for other hosts (than itself)
    /// </summary>
    public int MinPlayers = 2;
    public int MaxPlayers = 4;
    public int Port = 25000;
    public bool EnableLogging = true;

    /// <summary>
    /// The timeout between each join game retry when hosting a game
    /// as a result of a JoinOrCreateGame() call, while there are
    /// less players in the game than MinPlayers
    /// </summary>
    public int JoinOrCreateGameMinPlayersEnsuringTimeOut = 1;
    /// <summary>
    /// The max number of times it will be attempted to join a game
    /// as a result of a JoinOrCreateGame() call, while there are less
    /// players in the game than MinPlayers
    /// </summary>
    public int JoinOrCreateGameMinPlayersEnsuringMaxAttempts = 10;
    private bool _ensuringMinPlayers = false;

    private bool _autoRetry;
    private bool _stop = false;

    /// <summary>
    /// The number of connected players (not including the host)
    /// </summary>
    private int _numberOfConnectedPlayers = 0; 

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
            if (host.connectedPlayers < MaxPlayers && preference == HostPreference.FirstAvailable &&
                (!_ensuringMinPlayers || (_ensuringMinPlayers && SelectHostWhileEnsuringMinPlayers(host))))
            {
                return host;
            }
        }
        return null;
    }

    private bool SelectHostWhileEnsuringMinPlayers(HostData host)
    {
        // Just need som rule to decide who shall join who, as both hosts will be doing this at the same time
        // The rule, as of now, is the host with the greater guid joins the one with the lesser guid(?)
        return _ensuringMinPlayers && host.guid != Network.player.guid && String.Compare(host.guid, Network.player.guid) > 0;
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
            else
            {
                EnsureMinPlayers();
            }
        });
    }

    private void EnsureMinPlayers()
    {
        Log("Ensuring min players (" + MinPlayers + ")..");
        _ensuringMinPlayers = true;
        StartCoroutine(EnsureMinPlayersHelper());
    }

    private IEnumerator EnsureMinPlayersHelper()
    {
        int counter = 0;
        while (_numberOfConnectedPlayers < MinPlayers - 1 && counter++ < JoinOrCreateGameMinPlayersEnsuringMaxAttempts)
        {
            yield return new WaitForSeconds(JoinOrCreateGameMinPlayersEnsuringTimeOut);
            JoinGame(autoRetry: false);
        }
        Log("No longer looking for other hosts.");
    }

    #region EventLogging

    void OnPlayerConnected(NetworkPlayer player)
    {
        _numberOfConnectedPlayers++;
        Log("Player connected: [IP=" + player.ipAddress + "].");
    }

    void OnPlayerDisconnected(NetworkPlayer player)
    {
        _numberOfConnectedPlayers--;
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
