using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TitleScreenScript : MonoBehaviour
{

    [SerializeField] private Camera titleCamera;

    [SerializeField] private Transform cameraTarget;
    [SerializeField] private List<Transform> cameraLocations = new List<Transform>();

    // Start is called before the first frame update
    void Start()
    {

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

}
