using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TitleScreenScript : MonoBehaviour
{

    [SerializeField] private Camera titleCamera;


    private bool isCrateSelected = false;
    private CrateButtonScript crateButton = null;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {

        //Perform a raycast to find any crate buttons.
        RaycastHit result;
        if (Physics.Raycast(titleCamera.ScreenPointToRay(Input.mousePosition), out result, 9999.0f))
        {
            CrateButtonScript crateScript = result.collider.GetComponent<CrateButtonScript>();
            if (crateScript != null)
            {
                //One is found, select it only when it is a different crate button.
                if (crateButton != crateScript)
                {
                    //If we already got one beforehand, deselect it first.
                    if (crateButton != null)
                        crateButton.onLeave();

                    //Select the crate.
                    crateScript.onEnter();
                }

                //Store the crate currently selected.
                crateButton = crateScript;
            }
        }
        else if(crateButton != null)
        {
            //If none is found but we still got one, deselect it.
            crateButton.onLeave();
            crateButton = null;
        }


        //Handle clicking for a selected crate button.
        if(crateButton != null)
        {
            if (Input.GetMouseButtonDown(0))
                crateButton.onPressed();
            else if (Input.GetMouseButtonUp(0))
                crateButton.onReleased();
        }

    }
}
