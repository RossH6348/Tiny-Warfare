using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class OptionsScript : MonoBehaviour
{

    //Text fields in the options menu.
    [SerializeField] private Text volumeText;
    [SerializeField] private Text sensitivityText;

    private string previousName = "";
    [SerializeField] private InputField playerInputField;

    [SerializeField] private Slider volumeSlider;
    [SerializeField] private Slider sensitivitySlider;

    private void Start()
    {
        previousName = "Tiny Soldier";
    }

    public void onVolumeChange()
    {
        //Update the volume.
        volumeText.text = Mathf.FloorToInt(volumeSlider.value * 100.0f).ToString() + "%";
    }

    public void onSensitivityChange()
    {
        //Update the sensitivity.
        sensitivityText.text = Mathf.FloorToInt(sensitivitySlider.value * 100.0f).ToString() + "%";
    }

    public void onNameChange()
    {
        //If the name change is blank, reverse back to the previous name, otherwise update to the new name.
        if (playerInputField.text.Equals(""))
        {
            playerInputField.text = previousName;
        }
        else
        {
            previousName = playerInputField.text;
        }
    }
}
