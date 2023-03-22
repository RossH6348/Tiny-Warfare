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
    private void networkMessage(string topic = "", string reason = "")
    {
        networkDialog.SetActive(true);
        networkTopic.text = topic;
        networkText.text = reason;
    }

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

        ClearLobbyClientRpc();
        NetworkManager.Shutdown();

    }

    List<string> LobbyPlayers = new List<string>();

    [ServerRpc(RequireOwnership = false)]
    private void LobbyHandshakeServerRpc(string playerName)
    {

        //Okay we received this client's name, so let add it to the server list and rebroadcast the current lobby status.
        LobbyPlayers.Add(playerName);
        ClearLobbyClientRpc();
        foreach (string name in LobbyPlayers)
            AddLobbyPlayerClientRpc(name);

    }

    [ClientRpc]
    private void LobbyHandshakeClientRpc(ClientRpcParams clientRpcParams = default)
    {

        LobbyHandshakeServerRpc(options.playerInputField.text);

    }

    [ClientRpc]
    private void ClearLobbyClientRpc()
    {
        foreach (GameObject soldier in LobbySoldiers)
            soldier.SetActive(false);
    }

    [ClientRpc]
    private void AddLobbyPlayerClientRpc(string playerName)
    {
        foreach(GameObject soldier in LobbySoldiers)
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

}
