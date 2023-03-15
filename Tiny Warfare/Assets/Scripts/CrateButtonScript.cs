using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CrateButtonScript : MonoBehaviour
{

    //Colors to determine what to pick when highlighted, clicked and so on.
    [SerializeField] private Color defaultColor;
    [SerializeField] private Color highlightColor;
    [SerializeField] private Color clickedColor;

    [SerializeField] private Text buttonText;

    private void Awake()
    {
        buttonText.color = defaultColor;
    }

    //This is called when the user's mouse cursor hovers over the crate.
    public void onEnter()
    {

        buttonText.color = highlightColor;

    }

    //This is called when the user's mouse cursor no longer hovers over the crate.
    public void onLeave()
    {

        buttonText.color = defaultColor;

    }

    //This is called when the user clicks on the crate.
    public void onPressed()
    {

        buttonText.color = clickedColor;

    }

    //This is called when the user release their click on the crate.
    public void onReleased()
    {

        buttonText.color = highlightColor;

    }

}
