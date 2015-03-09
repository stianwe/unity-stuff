MatchMaker
===========

MatchMaker handles everything you need to connect players in a multiplayer game. 
All you need to do is attach the MatchMaker script to a game object, and then make it host or connect to a game for you.

Instructions:
<ol>
  <li>Download MatchMaker.cs and import it into your Unity project.</li>
  <li>Attach the MatchMaker script to a game object.</li>
  <li>Tweak the settings (such as max players per game) by modifying public variables through the Unity interface by selecting the game object you attached the script to.</li>
  <li>Depending on if you want the player to be a client or a server, you call
    <ol>
      <li>CreateGame() to create a game, or</li>
      <li>JoinGame() to join a game, or</li>
      <li>JoinOrCreateGame() if you want to join a game if possible, but create a game if none are available.</li>
    </ol>
  </li>
</ol>

Tips:
<ol>
<li>If you need to run some code when joining a game, you can override void OnConnectedToServer() in any MonoBehaviour script.</li>
<li>If you need to run some code when startubg a game, you can override void OnServerInitialized() in any MonoBehaviour script.</li/>
<li>If you need to run some code when a player connects or disconnects to your game, you can override void OnPlayerConnected(NetworkPlayer) or void OnPlayerDisconnected(NetworkPlayer) in any MonoBehaviour script.</li> 
</ol>
