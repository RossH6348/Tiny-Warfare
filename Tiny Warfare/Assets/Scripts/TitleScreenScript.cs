using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;

public class TitleScreenScript : NetworkBehaviour
{


    //[SerializeField] private NetworkManager Multiplayer;
    [SerializeField] private OptionsScript options;

    [SerializeField] private Camera titleCamera;

    [SerializeField] private Transform cameraTarget;
    [SerializeField] private List<Transform> cameraLocations = new List<Transform>();

    [SerializeField] private GameObject networkDialog;
    [SerializeField] private Text networkTopic;
    [SerializeField] private Text networkText;

    [SerializeField] private GameObject joinDialog;
    [SerializeField] private InputField joinInput;

    [SerializeField] private List<GameObject> LobbySoldiers;

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

        if (cameraTarget != cameraLocations[2])
            cameraTarget = cameraLocations[2];

    }

    public void LeaveLobby()
    {

        LobbyDisconnectServerRpc();

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

    private void onLobbyLeave(ulong clientId)
    {
        NetworkManager.Shutdown();
        ClearLobby();
        MainScreen();
    }

    [ServerRpc(RequireOwnership = false)]
    private void LobbyHandshakeServerRpc(string playerName, ServerRpcParams serverRpcParams = default)
    {

        //Okay we received this client's name, so let add it to the server list and rebroadcast the current lobby status.
        ClientToNameScript.playerNames.Add(serverRpcParams.Receive.SenderClientId, playerName);
        ClearLobbyClientRpc();
        foreach (KeyValuePair<ulong, string> clientData in ClientToNameScript.playerNames)
            AddLobbyPlayerClientRpc(clientData.Value);

    }

    [ServerRpc(RequireOwnership = false)]
    private void LobbyDisconnectServerRpc(ServerRpcParams serverRpcParams = default)
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
            ClearLobby();
            MainScreen();
            return;
        }
        else
        {
            ClientToNameScript.playerNames.Remove(serverRpcParams.Receive.SenderClientId);
            NetworkManager.DisconnectClient(serverRpcParams.Receive.SenderClientId);
        }

        ClearLobbyClientRpc();
        foreach (KeyValuePair<ulong, string> clientData in ClientToNameScript.playerNames)
            AddLobbyPlayerClientRpc(clientData.Value);

    }

    [ClientRpc]
    private void LobbyHandshakeClientRpc(ClientRpcParams clientRpcParams = default)
    {

        //Response back to the handshake with our player name.
        LobbyHandshakeServerRpc(options.playerInputField.text);

    }

    [ClientRpc]
    private void ClearLobbyClientRpc(ClientRpcParams clientRpcParams = default)
    {

        ClearLobby();

    }

    private void ClearLobby()
    {
        foreach (GameObject soldier in LobbySoldiers)
            soldier.SetActive(false);
    }

    [ClientRpc]
    private void AddLobbyPlayerClientRpc(string playerName, ClientRpcParams clientRpcParams = default)
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
                    lobbyPlayer.initializePlayer(playerName, IsHost);
                break;
            }
        }
    }

    [ClientRpc]
    private void networkMessageClientRpc(string topic = "", string reason = "", ClientRpcParams clientRpcParams = default)
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
