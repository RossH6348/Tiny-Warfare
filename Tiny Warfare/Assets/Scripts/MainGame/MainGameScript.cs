using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;

public class MainGameScript : NetworkBehaviour
{

    [SerializeField] private TitleScreenScript mainMenu;
    [SerializeField] private OptionsScript options;

    [SerializeField] private HouseGenerationScript houseGenerator;
    public GameObject gameCamera;
    [SerializeField] private GameObject PlayerPrefab;

    public GameObject pauseMenu;

    //All match data here
    private Dictionary<ulong, int> totalKills = new Dictionary<ulong, int>();
    private List<ulong> appendingRespawns = new List<ulong>();
    private IEnumerator MatchCountdownLoop = null;

    //All global UI related stuff.
    [SerializeField] public GameObject Overlay;
    [SerializeField] private Text headerText;

    [SerializeField] private GameObject playerHUD;
    [SerializeField] private Text nameText;
    [SerializeField] private Transform healthBar;
    [SerializeField] private Text healthText;

    [SerializeField] private Transform Chat;
    [SerializeField] private GameObject MessagePrefab;
    private class Message
    {
        public GameObject textObj = null;
        public float lifeTime = 10.0f;
    }
    private List<Message> ChatMessages = new List<Message>();

    [SerializeField] public GameObject matchResults;
    [SerializeField] private GameObject matchResultPrefab;
    [SerializeField] private Transform matchResultsLeaderboard;

    public void Update()
    {

        //Only do main game controls if the main menu is DEACTIVATED! (It means the player is in the actual game at that point.)
        if (!mainMenu.titleCamera.activeSelf && !matchResults.activeSelf)
        {

            if (Input.GetKeyDown(KeyCode.Escape))
                pauseMenu.SetActive(!pauseMenu.activeSelf);

        }

        Cursor.lockState = CursorLockMode.None;

        //Handle hud stuff
        NetworkClient localClient  = NetworkManager.LocalClient;
        if (localClient != null)
        {
            NetworkObject localPlayer = localClient.PlayerObject;
            playerHUD.SetActive(localPlayer != null);
            if (localPlayer != null)
            {
                nameText.text = options.playerInputField.text;
                int Health = localPlayer.GetComponent<PlayerScript>().Health.Value;
                healthBar.transform.localScale = new Vector3((float)Health / 100.0f, 1.0f);
                healthText.text = Health.ToString();

                if (!matchResults.activeSelf && !pauseMenu.activeSelf)
                    Cursor.lockState = CursorLockMode.Locked;

            }
        }

        //Handle chat messages
        for(int i = ChatMessages.Count - 1; i > -1; i--)
        {
            Message messageObj = ChatMessages[i];
            messageObj.lifeTime -= Time.deltaTime;
            if(messageObj.lifeTime <= 0.0f)
            {
                Destroy(messageObj.textObj);
                ChatMessages.RemoveAt(i);
                continue;
            }
            messageObj.textObj.transform.localPosition = Vector3.Lerp(messageObj.textObj.transform.localPosition, new Vector3(0.0f, -64.0f * (float)i), 5.0f * Time.deltaTime);
        }

        //Handle all server logic here!
        if (NetworkManager.IsListening && IsHost)
        {
            foreach (NetworkClient client in NetworkManager.ConnectedClientsList)
            {
                NetworkObject playerObj = client.PlayerObject;
                if(playerObj != null)
                {
                    PlayerScript playerData = playerObj.GetComponent<PlayerScript>();
                    if (playerData.Health.Value <= 0)
                        killPlayer(playerObj, playerData.lastShot);
                }
            }
        }
    }

    public void InitializeGame()
    {
        //Move the title screen camera to a position.
        gameCamera.SetActive(true);
        Overlay.SetActive(true);

        //Tell the server that we are ready!
        PlayerReadyServerRpc();
    }
	
	private void StartNewGame(){

        //Tell all clients to clear their house map.
        clearGameStateClientRpc();
		
		//BEGIN HOUSE GENERATION! (On the Host side at least.)
		houseGenerator.createHouse();
		
		//Begin compressing each column of the house and send it to all clients.
		for(int x = 0; x < houseGenerator.houseX; x++)
		{

			string column = "";

			for(int y =0; y < houseGenerator.houseY; y++)
			{

				TileData tile = houseGenerator.houseData[x][y];

				string newTile = x.ToString();
				newTile = newTile + "," + y.ToString();
				newTile = newTile + "," + tile.room.ToString();
				newTile = newTile + "," + tile.north.ToString();
				newTile = newTile + "," + tile.east.ToString();
				newTile = newTile + "," + tile.south.ToString();
				newTile = newTile + "," + tile.west.ToString();

				column = column + newTile;
				if (y < houseGenerator.houseY - 1)
					column = column + " ";

			}

			PlaceHouseColumnClientRpc(column);

		}

        //Furnitures are already compressed by the generator, so we can just send all clients each entry.
        foreach (string furniture in houseGenerator.furnitures)
            PlaceFurnitureClientRpc(furniture);

        //Start all clients with 0 kills.
        foreach (ulong clientId in NetworkManager.ConnectedClientsIds)
			if(!totalKills.ContainsKey(clientId))
				totalKills.Add(clientId, 0);
			else
				totalKills[clientId] = 0;
			
		//Begin match countdown coroutine.
		if (MatchCountdownLoop != null)
			StopCoroutine(MatchCountdownLoop);
		StartCoroutine(MatchCountdownLoop = MatchCountdown());
		
	}

    IEnumerator MatchCountdown()
    {
        int seconds = 10;
        while (seconds > 0)
        {

            setHeaderTextClientRpc("Match begins in " + seconds.ToString());
            seconds--;
            yield return new WaitForSeconds(1);
        }

        foreach (ulong clientId in NetworkManager.ConnectedClientsIds)
            spawnPlayer(clientId);

        seconds = 600;
        while (seconds > 0)
        {

            setHeaderTextClientRpc((seconds / 60).ToString() + ":" + (seconds % 60).ToString("00"));
            seconds--;
            yield return new WaitForSeconds(1);
        }

        //Clear all players and set spectator camera mode for all clients.
		setHeaderTextClientRpc("");
        setSpectateCameraClientRpc(true);
        clearAllPlayers();

        //Get all results and send them to all clients.
        string matchResult = "";
        for(int i = 0; i < NetworkManager.ConnectedClientsIds.Count; i++)
        {
            ulong clientId = NetworkManager.ConnectedClientsIds[i];

            matchResult = matchResult + "<color=#fff000ff>" + ClientGroupScript.clientToName[clientId] + "</color> - " + totalKills[clientId].ToString();
            if (i < NetworkManager.ConnectedClients.Count - 1)
                matchResult = matchResult + ",";
        }
        showMatchResultsClientRpc(matchResult);
		
		Invoke("StartNewGame",10.0f);

    }

    //All network related stuff goes here!
    private void spawnPlayer(ulong clientId)
    {

        ClientRpcParams clientRpcParams = new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = new ulong[] { clientId }
            }
        };
        setSpectateCameraClientRpc(false, clientRpcParams);

        GameObject Soldier = GameObject.Instantiate(PlayerPrefab, transform);

        Vector2Int spawnPos = new Vector2Int(Random.Range(0, houseGenerator.houseX - 1), Random.Range(0, houseGenerator.houseY - 1));
        while(houseGenerator.houseData[spawnPos.x][spawnPos.y].isOccupied)
            spawnPos = new Vector2Int(Random.Range(0, houseGenerator.houseX - 1), Random.Range(0, houseGenerator.houseY - 1));

        Soldier.transform.localPosition = new Vector3((float)spawnPos.x, 0.0f, (float)spawnPos.y);

        NetworkObject networkSoldier = Soldier.GetComponent<NetworkObject>();
        networkSoldier.SpawnAsPlayerObject(clientId);

    }

    private void RespawnPlayer()
    {

        if(appendingRespawns.Count > 0)
        {
            spawnPlayer(appendingRespawns[0]);
            appendingRespawns.RemoveAt(0);
        }

    }

    private void killPlayer(NetworkObject victim, ulong killer)
    {


        messageSentClientRpc("<color=#fff000ff>" + ClientGroupScript.clientToName[victim.OwnerClientId] + "</color> <color=#ff0000ff>was killed by " + ClientGroupScript.clientToName[killer] + "</color>");
        totalKills[killer]++;

        ClientRpcParams clientRpcParams = new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = new ulong[] { victim.OwnerClientId }
            }
        };
        setSpectateCameraClientRpc(true, clientRpcParams);
        messageSentClientRpc("You will respawn in 5 seconds.", clientRpcParams);

        victim.Despawn();

        appendingRespawns.Add(victim.OwnerClientId);
        Invoke("RespawnPlayer", 5.0f);

    }

    private void clearAllPlayers()
    {
        appendingRespawns.Clear();
        foreach (NetworkClient client in NetworkManager.ConnectedClientsList)
            if (client.PlayerObject != null)
                client.PlayerObject.Despawn();

    }

    public void LeaveGame()
    {

        GameDisconnectServerRpc();

    }

    [ServerRpc(RequireOwnership = false)]
    private void PlayerReadyServerRpc(ServerRpcParams serverRpcParams = default)
    {

        ClientGroupScript.clientIsReady[serverRpcParams.Receive.SenderClientId] = true;

        //Check if all clients are ready?
        bool isEveryoneReady = true;
        foreach(ulong clientId in NetworkManager.ConnectedClientsIds)
        {
            if (!ClientGroupScript.clientIsReady[clientId])
            {
                isEveryoneReady = false;
                break;
            }
        }

        if (isEveryoneReady)
			StartNewGame();

    }

    [ServerRpc(RequireOwnership = false)]
    private void GameDisconnectServerRpc(ServerRpcParams serverRpcParams = default)
    {

        ulong senderId = serverRpcParams.Receive.SenderClientId;
        if (senderId == NetworkManager.ServerClientId)
        {

            clearAllPlayers();
            StopAllCoroutines();

            //We will need to tell all other clients before disconnecting them that the host left.
            foreach (ulong clientId in NetworkManager.ConnectedClientsIds)
            {

                if (clientId == senderId)
                    continue; //Host does not need to see the reason.

                ClientRpcParams clientRpcParams = new ClientRpcParams
                {
                    Send = new ClientRpcSendParams
                    {
                        TargetClientIds = new ulong[] { clientId }
                    }
                };

                mainMenu.networkMessageClientRpc("Game Ended", "Host has decided to end the current game!", clientRpcParams);
            }

            ReturnToLobbyClientRpc();
            return;

        }

        //Store the client name for later use.
        string clientName = ClientGroupScript.clientToName[serverRpcParams.Receive.SenderClientId];

        //Remove them from the database.
        ClientGroupScript.nameToClient.Remove(clientName);
        ClientGroupScript.clientToName.Remove(serverRpcParams.Receive.SenderClientId);
        ClientGroupScript.clientIsReady.Remove(serverRpcParams.Receive.SenderClientId);
        NetworkManager.DisconnectClient(serverRpcParams.Receive.SenderClientId);

        //Check if the host is the remaining player, if so send them straight to the menu screen with a reason.
        if(NetworkManager.ConnectedClients.Count == 1)
        {
            clearAllPlayers();
            StopAllCoroutines();
            Overlay.SetActive(false);

            mainMenu.networkMessageClientRpc("Game Ended", "Not enough players to continue the match!");
            NetworkManager.Shutdown();
            ClientGroupScript.clientToName.Clear();
            ClientGroupScript.nameToClient.Clear();
            ClientGroupScript.clientIsReady.Clear();

            mainMenu.titleCamera.SetActive(true);
            gameCamera.SetActive(false);
            pauseMenu.SetActive(false);

            mainMenu.ClearLobbyClientRpc();
            mainMenu.MainScreen();
            return;
        }

        //Announce to everyone that someone have left.
        messageSentClientRpc("<color=#0000ffff>" + clientName + " has left the game!</color>");

        //Also tell them to update their lobby state.
        mainMenu.ClearLobbyClientRpc();
        foreach (KeyValuePair<ulong, string> clientData in ClientGroupScript.clientToName)
            mainMenu.AddLobbyPlayerClientRpc(clientData.Value);

    }

    [ClientRpc]
    private void ReturnToLobbyClientRpc(ClientRpcParams clientRpcParams = default)
    {

        clearGameStateClientRpc(clientRpcParams);
        gameCamera.SetActive(false);
        mainMenu.titleCamera.SetActive(true);
        Overlay.SetActive(false);

    }

    [ClientRpc]
    private void clearGameStateClientRpc(ClientRpcParams clientRpcParams = default)
    {
        //Hide the match results and puase menu (They may be returning to lobby anyway).
        matchResults.SetActive(false);
        pauseMenu.SetActive(false);

        //Clear the house map.
        Transform house = houseGenerator.transform;
        for (int i = house.childCount - 1; i > -1; i--)
            Destroy(house.GetChild(i).gameObject);
    }

    [ClientRpc]
    private void PlaceHouseColumnClientRpc(string column, ClientRpcParams clientRpcParams = default)
    {
        string[] tiles = column.Split(" ");
        foreach(string tile in tiles)
        {

            string[] tileData = tile.Split(",");
            houseGenerator.placeHouseTile(
                int.Parse(tileData[0]),
                int.Parse(tileData[1]),
                int.Parse(tileData[2]),
                int.Parse(tileData[3]),
                int.Parse(tileData[4]),
                int.Parse(tileData[5]),
                int.Parse(tileData[6])
            );

        }

    }

    [ClientRpc]
    private void PlaceFurnitureClientRpc(string furniture, ClientRpcParams clientRpcParams = default)
    {
        houseGenerator.placeFurniture(furniture);
    }

    [ClientRpc]
    private void showMatchResultsClientRpc(string matchResult, ClientRpcParams clientRpcParams = default)
    {
        Debug.Log(matchResult);

        //Clear the leaderboard.
        for (int i = matchResultsLeaderboard.childCount - 1; i > -1; i--)
            Destroy(matchResultsLeaderboard.GetChild(i).gameObject);

        string[] results = matchResult.Split(",");
        for(int i = 0; i < results.Length; i++)
        {
            GameObject result = GameObject.Instantiate(matchResultPrefab, matchResultsLeaderboard);
            result.transform.localPosition = new Vector3(0.0f, -176.0f * (float)i);
            result.GetComponentInChildren<Text>().text = results[i];
        }

        matchResults.SetActive(true);
        pauseMenu.SetActive(false); //Players will be sent back the lobby after match results anyway.

    }

    [ClientRpc]
    private void setSpectateCameraClientRpc(bool status, ClientRpcParams clientRpcParams = default)
    {
        gameCamera.SetActive(status);
    }

    [ClientRpc]
    private void setHeaderTextClientRpc(string header, ClientRpcParams clientRpcParams = default)
    {
        headerText.text = header;
    }

    [ClientRpc]
    private void messageSentClientRpc(string message, ClientRpcParams clientRpcParams = default)
    {

        if (ChatMessages.Count >= 6)
        {
            Destroy(ChatMessages[0].textObj);
            ChatMessages.RemoveAt(0);
        }

        Message newMessage = new Message();
        newMessage.textObj = GameObject.Instantiate(MessagePrefab, Chat);
        newMessage.textObj.transform.localPosition = new Vector3(0.0f, -64.0f * (float)ChatMessages.Count);
        newMessage.textObj.GetComponent<Text>().text = message;
        ChatMessages.Add(newMessage);
    }

}
