using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;

public class TitleScreenScript : NetworkBehaviour
{


    //[SerializeField] private NetworkManager Multiplayer;
    [SerializeField] private OptionsScript options;

    public GameObject titleCamera;

    [SerializeField] private Transform cameraTarget;
    [SerializeField] private List<Transform> cameraLocations = new List<Transform>();

    [SerializeField] private GameObject networkDialog;
    [SerializeField] private Text networkTopic;
    [SerializeField] private Text networkText;

    [SerializeField] private GameObject joinDialog;
    [SerializeField] private InputField joinInput;

    [SerializeField] private List<GameObject> LobbySoldiers;
    [SerializeField] private GameObject StartGameButton;

    [SerializeField] private MainGameScript mainGame;

    // Start is called before the first frame update
    void Start()
    {

        networkDialog.SetActive(false);
        joinDialog.SetActive(false);
        networkTopic.text = networkText.text = joinInput.text = "";

        NetworkManager.OnClientConnectedCallback += onLobbyJoin;
        NetworkManager.OnClientDisconnectCallback += onLobbyLeave;
    }

    // Update is called once per frame
    void Update()
    {

        titleCamera.transform.rotation = Quaternion.Lerp(titleCamera.transform.rotation, cameraTarget.rotation, Time.deltaTime * 2.5f);
        titleCamera.transform.position = Vector3.Lerp(titleCamera.transform.position, cameraTarget.position, Time.deltaTime * 2.5f);

    }

    public void MainScreen()
    {
        if (cameraTarget != cameraLocations[0])
            cameraTarget = cameraLocations[0];
    }

    public void LobbyScreen()
    {
        if (cameraTarget != cameraLocations[2])
            cameraTarget = cameraLocations[2];
    }

    public void OptionsScreen()
    {
        if (cameraTarget != cameraLocations[1])
            cameraTarget = cameraLocations[1];
    }

    public void ExitGame()
    {
        Application.Quit();
    }

    //All networking related stuff will be done here.
    public void HostLobby()
    {

        NetworkManager.StartHost();

        if (cameraTarget != cameraLocations[2])
            cameraTarget = cameraLocations[2];

    }

    public void JoinLobby()
    {

        joinDialog.SetActive(true);
        joinInput.text = "";

    }

    public void ConnectLobby()
    {

        NetworkManager.StartClient();
        joinDialog.SetActive(false);

        LobbyScreen();

    }

    public void LeaveLobby()
    {

        LobbyDisconnectServerRpc();

    }

    public void LobbyKickPlayer(Text playerText)
    {

        LobbyKickPlayerServerRpc(playerText.text);

    }

    public void StartLobbyGame()
    {

        LobbyStartGameServerRpc();

    }

    private void onLobbyJoin(ulong clientId)
    {

        ClientRpcParams clientRpcParams = new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = new ulong[] { clientId }
            }
        };

        LobbyHandshakeClientRpc(clientRpcParams);

    }

    public void onLobbyLeave(ulong clientId)
    {
        NetworkManager.Shutdown();
        ClientGroupScript.clientToName.Clear();
        ClientGroupScript.nameToClient.Clear();
        ClientGroupScript.clientIsReady.Clear();

        titleCamera.SetActive(true);
        mainGame.gameCamera.SetActive(false);
        mainGame.pauseMenu.SetActive(false);

        ClearLobbyClientRpc();
        MainScreen();
    }

    [ServerRpc(RequireOwnership = false)]
    private void LobbyHandshakeServerRpc(string playerName, ServerRpcParams serverRpcParams = default)
    {

        //Okay we received this client's name, so let add it to the server list and rebroadcast the current lobby status.
        ClientGroupScript.clientToName.Add(serverRpcParams.Receive.SenderClientId, playerName);
        ClientGroupScript.nameToClient.Add(playerName, serverRpcParams.Receive.SenderClientId);
        ClientGroupScript.clientIsReady.Add(serverRpcParams.Receive.SenderClientId, false);
        ClearLobbyClientRpc();
        foreach (KeyValuePair<ulong, string> clientData in ClientGroupScript.clientToName)
            AddLobbyPlayerClientRpc(clientData.Value);
    }

    [ServerRpc(RequireOwnership = false)]
    public void LobbyDisconnectServerRpc(ServerRpcParams serverRpcParams = default)
    {

        //Okay this client wants to disconnect, let find the client's playername to remove and rebroadcast.
        //Unless it is the host, then we will need to disconnect everyone with a reason.
        ulong senderId = serverRpcParams.Receive.SenderClientId;
        if (senderId == NetworkManager.ServerClientId)
        {

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

                networkMessageClientRpc("Disconnected", "Host has left the lobby!", clientRpcParams);
            }

            NetworkManager.Shutdown();
            ClientGroupScript.clientToName.Clear();
            ClientGroupScript.nameToClient.Clear();
            ClientGroupScript.clientIsReady.Clear();
            ClearLobbyClientRpc();
            MainScreen();
            return;
        }
        else
        {
            ClientGroupScript.nameToClient.Remove(ClientGroupScript.clientToName[serverRpcParams.Receive.SenderClientId]);
            ClientGroupScript.clientToName.Remove(serverRpcParams.Receive.SenderClientId);
            ClientGroupScript.clientIsReady.Remove(serverRpcParams.Receive.SenderClientId);
            NetworkManager.DisconnectClient(serverRpcParams.Receive.SenderClientId);
        }

        ClearLobbyClientRpc();
        foreach (KeyValuePair<ulong, string> clientData in ClientGroupScript.clientToName)
            AddLobbyPlayerClientRpc(clientData.Value);
    }

    [ServerRpc(RequireOwnership = false)]
    private void LobbyKickPlayerServerRpc(string playerName, ServerRpcParams serverRpcParams = default)
    {

        ulong senderId = serverRpcParams.Receive.SenderClientId;
        ulong clientId = ClientGroupScript.nameToClient[playerName];

        ClientRpcParams clientRpcParams = new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = new ulong[] { clientId }
            }
        };
        networkMessageClientRpc("Disconnected", "Host has kicked you from the lobby!", clientRpcParams);
        ClientGroupScript.nameToClient.Remove(ClientGroupScript.clientToName[clientId]);
        ClientGroupScript.clientToName.Remove(clientId);
        NetworkManager.DisconnectClient(clientId);

        ClearLobbyClientRpc();
        foreach (KeyValuePair<ulong, string> clientData in ClientGroupScript.clientToName)
            AddLobbyPlayerClientRpc(clientData.Value);
    }

    [ServerRpc(RequireOwnership = false)]
    private void LobbyStartGameServerRpc(ServerRpcParams serverRpcParams = default)
    {

        //Check if it is the host that is sending this command.
        if (serverRpcParams.Receive.SenderClientId != NetworkManager.ServerClientId)
            return;

        //Tell all other clients to switch to the game scene
        LobbyStartGameClientRpc();

    }

    [ClientRpc]
    private void LobbyStartGameClientRpc(ClientRpcParams clientRpcParams = default)
    {

        titleCamera.SetActive(false);
        mainGame.InitializeGame();

    }

    [ClientRpc]
    private void LobbyHandshakeClientRpc(ClientRpcParams clientRpcParams = default)
    {

        //Response back to the handshake with our player name.
        LobbyHandshakeServerRpc(options.playerInputField.text);

    }

    [ClientRpc]
    public void ClearLobbyClientRpc(ClientRpcParams clientRpcParams = default)
    {

        ClearLobby();

    }

    private void ClearLobby()
    {
        foreach (GameObject soldier in LobbySoldiers)
            soldier.SetActive(false);
    }

    [ClientRpc]
    public void AddLobbyPlayerClientRpc(string playerName, ClientRpcParams clientRpcParams = default)
    {

        AddLobbyPlayer(playerName);
    }

    private void AddLobbyPlayer(string playerName)
    {

        foreach (GameObject soldier in LobbySoldiers)
        {
            if (!soldier.activeSelf)
            {
                soldier.SetActive(true);
                LobbyPlayerScript lobbyPlayer = soldier.GetComponent<LobbyPlayerScript>();
                if (lobbyPlayer != null)
                    lobbyPlayer.initializePlayer(playerName, (NetworkManager.LocalClientId == NetworkManager.ServerClientId && playerName != options.playerInputField.text));
                break;
            }
        }

        //Check if there is enough players and is host to enable the start game button.
        StartGameButton.SetActive((NetworkManager.LocalClientId == NetworkManager.ServerClientId && NetworkManager.ConnectedClients.Count > 1));
    }

    [ClientRpc]
    public void networkMessageClientRpc(string topic = "", string reason = "", ClientRpcParams clientRpcParams = default)
    {
        networkDialog.SetActive(true);
        networkTopic.text = topic;
        networkText.text = reason;
    }

    void Cleanup()
    {
        if (NetworkManager.Singleton != null)
        {
            Destroy(NetworkManager.Singleton.gameObject);
        }
    }

}
