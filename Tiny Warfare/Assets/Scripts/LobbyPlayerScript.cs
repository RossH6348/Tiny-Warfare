using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LobbyPlayerScript : MonoBehaviour
{

    [SerializeField] private Text displayName;

    public void initializePlayer(string name, bool isHost)
    {

        displayName.text = name;

    }

    public string getPlayerName()
    {

        return displayName.text;

    }

}
