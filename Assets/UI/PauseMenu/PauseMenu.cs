using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseMenu : MonoBehaviour
{
    public GameObject bGPause; //Background pause menu variable

    public void QuitGame()
    {
        Debug.Log("The game would have returned to the main menu");
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (bGPause.activeSelf == true)
                bGPause.SetActive(false);
            else
                bGPause.SetActive(true);
        }
    }
}
