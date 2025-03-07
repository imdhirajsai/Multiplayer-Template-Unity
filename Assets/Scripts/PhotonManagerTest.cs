using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using Cinemachine;
using UnityEngine.UI;

public class PhotonManagerTest : MonoBehaviourPunCallbacks
{
    [Header("Loading Screen UI")]
    public GameObject loginPanel;
    public GameObject connectingPanel;
    public GameObject gamePlayPanel;

    public TMP_InputField chatInput;
    public GameObject Message;
    public GameObject chatContent;

    public TMP_InputField userNameText;
    public GameObject playerPrefab;
    public BoxCollider spawnArea;
    public CinemachineVirtualCamera virtualCameraPrefab;
    private GameObject selfPlayer;

    private string roomName = "StaticRoom";

    public PlayerManager playerManager; // Reference to PlayerManager

    void Start()
    {
        ActivatePanel(loginPanel.name);
    }

    public void OnLoginClick()
    {
        string name = userNameText.text;

        if (!string.IsNullOrEmpty(name))
        {
            PhotonNetwork.LocalPlayer.NickName = name;
            PhotonNetwork.ConnectUsingSettings();
            ActivatePanel(connectingPanel.name);
        }
    }

    public override void OnConnectedToMaster()
    {
        JoinOrCreateRoom();
    }

    public void JoinOrCreateRoom()
    {
        RoomOptions roomOptions = new RoomOptions { MaxPlayers = 0 };
        PhotonNetwork.JoinOrCreateRoom(roomName, roomOptions, TypedLobby.Default);
    }

    public override void OnJoinedRoom()
    {
        
        SpawnPlayer();

    }

    public void SpawnPlayer()
    {
        ActivatePanel(gamePlayPanel.name);
        Vector3 spawnPosition = GetRandomSpawnPositionWithinBounds();

        GameObject player = PhotonNetwork.Instantiate(playerPrefab.name, spawnPosition, Quaternion.identity);
        selfPlayer = player;
        ManagePlayerCamera(player);

        int playerId = PhotonNetwork.LocalPlayer.ActorNumber;

        // Add player to PlayerManager's list
        //playerManager.AddPlayer(PhotonNetwork.LocalPlayer.NickName, playerId, player);

        // Set visibility only for the local player
        player.SetActive(true);
      /*  HideOtherPlayers(); */// Hide other players at spawn
    }

   

    public Vector3 GetRandomSpawnPositionWithinBounds()
    {
        Bounds bounds = spawnArea.bounds;
        float x = Random.Range(bounds.min.x, bounds.max.x);
        float y = bounds.min.y; // Assuming ground-level spawn
        float z = Random.Range(bounds.min.z, bounds.max.z);
        return new Vector3(x, y, z);
    }

  

    public void SendMessage()
    {
        PhotonView playerChat = selfPlayer.GetComponent<PhotonView>();
        playerChat.RPC("GetMessage", RpcTarget.All, PhotonNetwork.NickName + " : " + chatInput.text);
        chatInput.text = "";
    }

   
    public void DisplayMessage(string ChatMessage)
    {
        GameObject M = Instantiate(Message, Vector3.zero, Quaternion.identity, chatContent.transform);
        M.GetComponent<Message>().MyMessage.text = ChatMessage;
    }


    public void ActivatePanel(string panelName)
    {
        loginPanel.SetActive(panelName.Equals(loginPanel.name));
        connectingPanel.SetActive(panelName.Equals(connectingPanel.name));
        gamePlayPanel.SetActive(panelName.Equals(gamePlayPanel.name));
    }


    private void ManagePlayerCamera(GameObject player)
    {
        PhotonView playerPhotonView = player.GetComponent<PhotonView>();

        if (playerPhotonView.IsMine)
        {
            // Find the PlayerCameraRoot in the player prefab
            Transform cameraRoot = player.transform.Find("PlayerCameraRoot");

            if (cameraRoot != null)
            {
                // Create a new virtual camera for this player
                CinemachineVirtualCamera playerCamera = Instantiate(virtualCameraPrefab, cameraRoot.position, Quaternion.identity);
                playerCamera.transform.SetParent(cameraRoot); // Set the camera's parent to PlayerCameraRoot
                playerCamera.Follow = cameraRoot; // Set the camera to follow the PlayerCameraRoot
                playerCamera.LookAt = cameraRoot;  // Optionally set the camera to look at the PlayerCameraRoot

            }
            else
            {
                Debug.LogError("PlayerCameraRoot not found in the player prefab.");
            }
        }
        else
        {
            Debug.LogError("PhotonView not found on player prefab.");
        }
    }

}
