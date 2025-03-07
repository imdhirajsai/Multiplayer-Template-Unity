using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun; // Make sure to include this for Photon

public class BillBoard : MonoBehaviour
{
    private Transform cam;

    // Method to set the camera reference
    public void SetCamera(Transform cameraTransform)
    {
        cam = cameraTransform;
    }

    void LateUpdate()
    {
        
         if (PhotonNetwork.IsConnectedAndReady &&  cam != null)
        {
            transform.LookAt(transform.position + cam.forward);
        }
    }
}
