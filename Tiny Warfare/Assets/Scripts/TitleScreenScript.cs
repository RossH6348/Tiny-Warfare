using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;

public class TitleScreenScript : MonoBehaviour
{


    [SerializeField] private NetworkManager Multiplayer;

    [SerializeField] private Camera titleCamera;

    [SerializeField] private Transform cameraTarget;
    [SerializeField] private List<Transform> cameraLocations = new List<Transform>();

    [SerializeField] private GameObject networkDialog;
    [SerializeField] private Text networkTopic;
    [SerializeField] private Text networkText;

    [SerializeField] private GameObject joinDialog;
    [SerializeField] private InputField joinInput;

    // Start is called before the first frame update
    void Start()
    {

        networkDialog.SetActive(false);
        joinDialog.SetActive(false);
        networkTopic.text = networkText.text = joinInput.text = "";

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

        Multiplayer.StartHost();

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

        Multiplayer.StartClient();
        joinDialog.SetActive(false);

        if (cameraTarget != cameraLocations[2])
            cameraTarget = cameraLocations[2];

    }

}
