using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using Cinemachine;
using Photon.Voice.PUN;
using Photon.Voice.Unity;
using UnityEngine.UI;
using System.Runtime.CompilerServices;
using ExitGames.Client.Photon;
using StarterAssets;

public class PhotonManager : MonoBehaviourPunCallbacks
{
    private UIManagement ui;
    public TMP_Text loadingText;
    public TMP_InputField userNameText;
    public TMP_Text pingText;
    public TMP_Text networkStatsText;
    public TMP_Text localPlayer;
    private string playerLocalName;

    public GameObject malePrefab;
    public GameObject femalePrefab;
    public bool isGender;

    public TMP_InputField chatInput;
    public GameObject Message;
    public GameObject chatContent;
    private GameObject selfPlayer;
    public GameObject chatBoxParent;
    [SerializeField] private Image hideButton;

    public BoxCollider spawnArea;
    public CinemachineVirtualCamera virtualCameraPrefab;

    private string roomName = "StaticRoom";
    private float timeInterval = 1f;
    private float timePassed = 0f;

    // Voice chat components
    private PhotonVoiceView photonVoiceView;
    public Sprite[] voiceIcons;
    public Image voiceToggleButtonIcon;
    [SerializeField] public Button voiceToggleButton;
    public bool anyFemale = false;
    public bool anyMale = false; 

    private void Start()
    {
        // When the user starts typing, disable the controller
        chatInput.onSelect.AddListener(delegate { DisableThirdPersonController(); });
        // When the input field is deselected (e.g., after pressing enter), re-enable the controller
        chatInput.onDeselect.AddListener(delegate { EnableThirdPersonController(); });
        ui = GetComponent<UIManagement>();
        hideChatbox();
    }

    void Update()
    {
        timePassed += Time.deltaTime;

        if (timePassed >= timeInterval)
        {
            timePassed = 0f;
            UpdatePing();
            UpdateNetworkStats();
        }
    }

    public void OnLoginClick()
    {
        PhotonNetwork.LocalPlayer.NickName = playerLocalName;
        PhotonNetwork.ConnectUsingSettings();
        ui.onConnecting();
        localPlayer.text = playerLocalName;
    }

    public override void OnConnected()
    {
        Debug.Log("Connected to the Internet");
    }

    public override void OnConnectedToMaster()
    {
        Debug.Log(PhotonNetwork.LocalPlayer.NickName + " is connected to Photon");
        JoinOrCreateRoom();
    }

    public void JoinOrCreateRoom()
    {
        RoomOptions roomOptions = new RoomOptions();
        roomOptions.MaxPlayers = 0; // No limit for the number of players in the room
        PhotonNetwork.JoinOrCreateRoom(roomName, roomOptions, TypedLobby.Default);
    }

    public override void OnJoinedRoom()
    {
        Debug.Log("Joined room: " + PhotonNetwork.CurrentRoom.Name);
        ui.ShowGamePlayUI();
        SpawnPlayer();
    }

    public void getPlayerDetails(bool isMale, string playerName)
    {
        Debug.Log($"Initializing Player: {playerName}, Gender: {(isMale ? "Male" : "Female")}");

        // Store player's gender in custom properties
        ExitGames.Client.Photon.Hashtable playerProperties = new ExitGames.Client.Photon.Hashtable
        {
            { "isMale", isMale }
        };
        PhotonNetwork.LocalPlayer.SetCustomProperties(playerProperties);

        playerLocalName = playerName;
        // Spawn the player with the specified gender
        OnLoginClick();
    }

    public void SpawnPlayer()
    {
        Vector3 spawnPosition = GetRandomSpawnPositionWithinBounds();
        Debug.Log("Spawning player at: " + spawnPosition);

       

        // Iterate through all players in the room
        foreach (var p in PhotonNetwork.PlayerList)
        {
            // Check if the player's custom property for gender is set
            if (p.CustomProperties.TryGetValue("isMale", out object isMaleObj) && isMaleObj is bool isMale)
            {
                if (isMale)
                {
                    Debug.Log("male prefab called");
                    anyMale = true;
                    anyFemale = false;
                }
                else
                {
                    anyMale = false;
                    anyFemale = true;
                    Debug.Log("female prefab called");
                }
            }
        }

        GameObject playerPrefab = anyMale ? malePrefab : femalePrefab;
        GameObject player = PhotonNetwork.Instantiate(playerPrefab.name, spawnPosition, Quaternion.identity);
        PhotonNetwork.LocalPlayer.TagObject = player;
        ManagePlayerCamera(player);

        // Assign the photonVoiceView from the player
        photonVoiceView = player.GetComponent<PhotonVoiceView>();

        // Check if photonVoiceView is successfully assigned
        if (photonVoiceView != null)
        {
            Debug.Log("PhotonVoiceView successfully assigned.");
        }
        else
        {
            Debug.LogError("PhotonVoiceView is null! Make sure it's attached to the player prefab.");
        }

        // Check if the player is self
        if (player.GetComponent<PhotonView>().Owner.ActorNumber == PhotonNetwork.LocalPlayer.ActorNumber)
        {
            selfPlayer = player; // Assign selfPlayer to the local player
            Debug.Log($"Self player assigned: {selfPlayer.name}");

            if (voiceToggleButton != null)
            {
                voiceToggleButton.onClick.AddListener(ToggleVoiceChat);
               
            }
            else
            {
                Debug.LogError("Voice Toggle Button is not assigned!");
            }
        }

        SetPlayerName(player, PhotonNetwork.LocalPlayer.NickName);
       
    }

    private void ToggleVoiceChat()
    {
        Debug.Log("ToggleVoiceChat called");
        if (photonVoiceView != null)
        {
            if (photonVoiceView.RecorderInUse != null)
            {
                bool isTransmitting = photonVoiceView.RecorderInUse.TransmitEnabled;
                photonVoiceView.RecorderInUse.TransmitEnabled = !isTransmitting;
                UpdateVoiceToggleButton();

                // Log voice state change
                Debug.Log($"{PhotonNetwork.LocalPlayer.NickName} has " + (isTransmitting ? "muted" : "unmuted") + " their voice.");
            }
            else
            {
                Debug.LogWarning("RecorderInUse is null, make sure the recorder is correctly set up.");
            }
        }
        else
        {
            Debug.LogWarning("PhotonVoiceView is null. Cannot toggle voice chat.");
        }
    }

    private void UpdateVoiceToggleButton()
    {
        if (photonVoiceView != null && photonVoiceView.RecorderInUse != null)
        {
            bool isTransmitting = photonVoiceView.RecorderInUse.TransmitEnabled;

            // Change icon depending on state (Index 0 = Unmuted, Index 1 = Muted)
            int iconIndex = isTransmitting ? 0 : 1;

            // Ensure voiceIcons array is not null and has elements
            if (voiceIcons != null && voiceIcons.Length > 1)
            {
                voiceToggleButtonIcon.sprite = voiceIcons[iconIndex];  // Set the appropriate icon
            }
            else
            {
                Debug.LogError("voiceIcons array is null or empty! Ensure it is properly initialized.");
            }
        }
    }

    private void SetPlayerName(GameObject player, string playerName)
    {
        // Find the TMP_Text component within the Canvas child
        TMP_Text playerNameText = player.GetComponentInChildren<Canvas>().GetComponentInChildren<TMP_Text>();

        if (playerNameText != null)
        {
            playerNameText.text = playerName; // Set the player's name directly
        }
        else
        {
            Debug.LogWarning("No TMP_Text component found on player prefab to set the name.");
        }
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        Debug.Log($"{newPlayer.NickName} has entered the room.");

        // Retrieve and log the new player's gender property
        if (newPlayer.CustomProperties.TryGetValue("isMale", out object isMaleObj) && isMaleObj is bool isMale)
        {
            Debug.Log($"{newPlayer.NickName} Gender: {(isMale ? "Male" : "Female")}");
        }
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
        // Check if the chat input is empty or only contains whitespace
        if (string.IsNullOrWhiteSpace(chatInput.text))
        {
            return;  // Exit the function if the input is empty
        }

        // Get the PhotonView of the player and send the chat message to all clients
        PhotonView playerChat = selfPlayer.GetComponent<PhotonView>();
        playerChat.RPC("GetMessage", RpcTarget.All, PhotonNetwork.NickName + " : " + chatInput.text);

        // Clear the input field after sending the message
        chatInput.text = "";
    }

    public void DisableThirdPersonController()
    {
        // Access the ThirdPersonController from the selfPlayer GameObject and disable it
        ThirdPersonController controller = selfPlayer.GetComponent<ThirdPersonController>();
        if (controller != null)
        {
            controller.enabled = false;  // Disable the ThirdPersonController script
        }
    }

    public void EnableThirdPersonController()
    {
        // Access the ThirdPersonController from the selfPlayer GameObject and enable it
        ThirdPersonController controller = selfPlayer.GetComponent<ThirdPersonController>();
        if (controller != null)
        {
            controller.enabled = true;  // Enable the ThirdPersonController script
        }
    }

    public void DisplayMessage(string ChatMessage)
    {
        chatBoxParent.SetActive(true);
        GameObject M = Instantiate(Message, Vector3.zero, Quaternion.identity, chatContent.transform);
        M.GetComponent<Message>().MyMessage.text = ChatMessage;
    }

    public void hideChatbox()
    {
        chatBoxParent.SetActive(!chatBoxParent.activeSelf);

        if (chatBoxParent.activeSelf)
        {
            hideButton.transform.localRotation = Quaternion.Euler(0, 0, 0);  // Set rotation to 0 degrees
        }
        else
        {
            hideButton.transform.localRotation = Quaternion.Euler(0, 0, 180);  // Set rotation to 180 degrees
        }
    }

    public void UpdatePing()
    {
        int ping = PhotonNetwork.GetPing();
        pingText.text = "Ping: " + ping + " ms";
    }

    public void UpdateNetworkStats()
    {
        int bytesSent = PhotonNetwork.NetworkingClient.LoadBalancingPeer.TrafficStatsOutgoing.TotalPacketBytes;
        int bytesReceived = PhotonNetwork.NetworkingClient.LoadBalancingPeer.TrafficStatsIncoming.TotalPacketBytes;

        float uploadSpeed = bytesSent / 1024f / timeInterval;  // in KB/s
        float downloadSpeed = bytesReceived / 1024f / timeInterval;  // in KB/s

        networkStatsText.text = $"Upload: {uploadSpeed:F2} KB/s\nDownload: {downloadSpeed:F2} KB/s";
    }

    private void ManagePlayerCamera(GameObject player)
    {
        PhotonView playerPhotonView = player.GetComponent<PhotonView>();

        if (playerPhotonView != null)
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

                if (PhotonNetwork.LocalPlayer.ActorNumber == playerPhotonView.Owner.ActorNumber)
                {
                    // Activate camera for the local player
                    playerCamera.gameObject.SetActive(true);

                }
                else
                {
                    // Disable camera for other players
                    playerCamera.gameObject.SetActive(false);
                }
            }
            else
            {
                Debug.LogError("PlayerCameraRoot not found in the player prefab.");
            }
        }
    }
}
