using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class Chat : MonoBehaviour
{
    GameObject uiPrefab;

    private void Start()
    {
         uiPrefab = GameObject.FindWithTag("UI");
    }
    
    [PunRPC]
    public void GetMessage(string ReceiveMessage)
    {
        // Access the PhotonManager from the UI prefab
        PhotonManager photonManager = uiPrefab.GetComponent<PhotonManager>();

        // Check if the PhotonManager is found
        if (photonManager != null)
        {
            // Call the ReceiveMessage method and pass the receisved message
            photonManager.DisplayMessage(ReceiveMessage);
        }
        else
        {
            Debug.LogError("PhotonManager component not found on the UI prefab!");
        }
    }
}
