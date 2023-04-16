using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class OptionsScript : MonoBehaviour
{

    //Text fields in the options menu.
    [SerializeField] private Text volumeText;
    [SerializeField] private Text sensitivityText;
    [SerializeField] private Slider volumeSlider;
    [SerializeField] private Slider sensitivitySlider;

    //The pause variant of the four.
    [SerializeField] private Text volumeTextB;
    [SerializeField] private Text sensitivityTextB;
    [SerializeField] private Slider volumeSliderB;
    [SerializeField] private Slider sensitivitySliderB;

    private string previousName = "";
    [SerializeField] public InputField playerInputField;

    private void Start()
    {
        previousName = "Tiny Soldier";
    }

    public void onVolumeChange(float volume)
    {
        //Update the volume.
        volumeSlider.value = volumeSliderB.value = volume;
        volumeText.text = volumeTextB.text = Mathf.FloorToInt(volume * 100.0f).ToString() + "%";
    }

    public void onSensitivityChange(float sensitivity)
    {
        //Update the sensitivity.
        sensitivitySlider.value = sensitivitySliderB.value = sensitivity;
        sensitivityText.text = sensitivityTextB.text = Mathf.FloorToInt(sensitivity * 100.0f).ToString() + "%";
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
