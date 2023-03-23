using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LobbyPlayerScript : MonoBehaviour
{

    [SerializeField] private Text displayName;
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private Transform canvasTransform;
    [SerializeField] private GameObject kickButton;

    private void Update()
    {
        canvasTransform.LookAt(cameraTransform);
    }

    public void initializePlayer(string name, bool isHost)
    {

        displayName.text = name;
        kickButton.SetActive(isHost);

    }

    public string getPlayerName()
    {

        return displayName.text;

    }

}
