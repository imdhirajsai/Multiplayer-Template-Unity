using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;

public class CameraManager : MonoBehaviour
{
    public CinemachineVirtualCamera[] virtualCameras; // Assign all player virtual cameras here
    private CinemachineVirtualCamera currentCamera;

    private void Start()
    {
        // Initially set to the local player's camera
        SetActiveCameraForLocalPlayer();
    }

    public void SetActiveCamera(CinemachineVirtualCamera camera)
    {
        if (currentCamera != null)
        {
            currentCamera.gameObject.SetActive(false); // Deactivate the previous camera
        }
        currentCamera = camera;
        currentCamera.gameObject.SetActive(true); // Activate the new camera
    }

    private void SetActiveCameraForLocalPlayer()
    {
        // Your logic to get the local player's camera
        // Example: Assume the first camera in the array is for the local player
        if (virtualCameras.Length > 0)
        {
            SetActiveCamera(virtualCameras[0]); // Change this logic based on how you assign cameras
        }
    }

    // Call this method whenever a player is spawned
    public void OnPlayerSpawned(Transform playerTransform)
    {
        // Assume that the corresponding virtual camera for this player is set in the array
        foreach (var camera in virtualCameras)
        {
            // Check if this camera belongs to the newly spawned player
            if (camera.Follow == playerTransform)
            {
                SetActiveCamera(camera);
                break;
            }
        }
    }

    // Method to set the local player's camera follow target
    public void SetLocalPlayerCameraFollow(Transform playerTransform)
    {
        if (currentCamera != null)
        {
            currentCamera.Follow = playerTransform; // Set the camera to follow the local player
            currentCamera.LookAt = playerTransform;  // Optionally set the camera to look at the player
        }
    }
}
