using System.Collections.Generic;
using UnityEngine;
using Photon.Voice.PUN;
using Photon.Pun;
using System.Collections;

public class SpeakerManager : MonoBehaviourPunCallbacks
{
    private PhotonVoiceView photonVoiceView; // Reference to the PhotonVoiceView component
    private UIManagement UI; // Reference to UI management
    private GameObject UIObject; // Reference to the UI object

   
    private static bool isMuted = true; 

    public float maxHearDistance = 15f; // Maximum distance to hear other players clearly
    public float minVolumeDistance = 5f; // Minimum distance to hear players at full volume

    private void Awake()
    {
        // Find UI object by tag
        UIObject = GameObject.FindGameObjectWithTag("UI");
        if (UIObject != null)
        {
            UI = UIObject.GetComponent<UIManagement>();
        }
        else
        {
            Debug.LogError("UIObject not found in the scene. Ensure it is tagged as 'UI'.");
        }

        photonVoiceView = GetComponent<PhotonVoiceView>();

        // Allow the local player to toggle their own speaker
      
    }

    void Start()
    {
        if (photonView.IsMine)
        {
            UI.speakerToggleButton.onClick.AddListener(ToggleSpeaker);

            // Apply the initial mute state for the local player (ensures prefab's mute state is respected)
            AudioSource localAudioSource = GetComponent<AudioSource>();
            if (localAudioSource != null)
            {
                localAudioSource.mute = isMuted; // Ensure the initial mute state is applied
            }

            // Start coroutine to wait for Photon Voice connection and then toggle speaker
            StartCoroutine(WaitForPhotonVoiceConnection());
            UpdateSpeakerToggleButtonText(isMuted);
        }
    }
    private IEnumerator WaitForPhotonVoiceConnection()
    {
        // Wait until Photon Voice is connected and the speaker is ready
        while (!photonVoiceView.IsRecording || photonVoiceView.SpeakerInUse == null)
        {
            yield return null; // Wait for the next frame
        }

        // Once connected, toggle the speaker
        //ToggleSpeaker();

    }

    void Update()
    {
        // Update the audio source mute state based on distance
        UpdateAudioSourceMuteState();
    }

    private void ToggleSpeaker()
    {
        // Only execute if this is the local player's view
        if (photonView.IsMine)
        {
            GameObject localSpeakerGameObject = GameObject.FindGameObjectWithTag("Speaker");

            if (localSpeakerGameObject == null)
            {
                Debug.LogError("Speaker GameObject not found.");
                return;
            }

            AudioSource localAudioSource = localSpeakerGameObject.GetComponent<AudioSource>();
            if (localAudioSource == null)
            {
                Debug.LogError("AudioSource component not found on the local Speaker GameObject.");
                return;
            }

            // Toggle the mute state
            localAudioSource.mute = !localAudioSource.mute;
            isMuted = localAudioSource.mute; // Update the static mute state
            UpdateSpeakerToggleButtonText(localAudioSource.mute); // Update the UI button

            // Log the local player's mute state
            Debug.Log($"{PhotonNetwork.LocalPlayer.NickName} has {(localAudioSource.mute ? "muted" : "unmuted")} their speaker.");

            // Update all other players' speakers based on the local mute state
            UpdateAllSpeakersMuteState(isMuted);
        }
    }

    private void UpdateAudioSourceMuteState()
    {
        if (!PhotonNetwork.IsConnected || isMuted)  // Check if global mute is on, skip the proximity check if muted
            return;

        // Find the local player position
        GameObject localPlayer = PhotonNetwork.LocalPlayer.TagObject as GameObject;

        if (localPlayer == null)
        {
            Debug.LogError("Local player not found.");
            return;
        }

        Vector3 localPlayerPosition = localPlayer.transform.position;

        // Find all GameObjects tagged as "Speaker"
        GameObject[] speakers = GameObject.FindGameObjectsWithTag("Speaker");

        foreach (var speaker in speakers)
        {
            // Ignore the local player’s own speaker
            if (speaker == localPlayer)
                continue;

            AudioSource speakerAudioSource = speaker.GetComponent<AudioSource>();
            if (speakerAudioSource != null)
            {
                float distance = Vector3.Distance(localPlayerPosition, speaker.transform.position);

                // Adjust volume based on distance only if isMuted is false
                if (distance > maxHearDistance)
                {
                    // Mute the player if they are beyond the max hearing distance
                    speakerAudioSource.volume = 0;
                }
                else if (distance < minVolumeDistance)
                {
                    // Full volume if within the minimum volume distance
                    speakerAudioSource.volume = 1;
                }
                else
                {
                    // Adjust volume proportionally based on distance
                    float normalizedDistance = (maxHearDistance - distance) / (maxHearDistance - minVolumeDistance);
                    speakerAudioSource.volume = Mathf.Clamp01(normalizedDistance);
                }

                // Apply mute if the global mute is active
                speakerAudioSource.mute = isMuted;
            }
            else
            {
                Debug.LogError("AudioSource component not found on a Speaker GameObject.");
            }
        }
    }

    private void UpdateSpeakerToggleButtonText(bool isMuted)
    {
        // Change the icon based on the mute state
        int iconIndex = isMuted ? 1 : 0; // 0 = Speaker On, 1 = Speaker Off
        UI.speakerToggleButtonIcon.sprite = UI.speakerIcons[iconIndex]; // Set the appropriate icon
    }

    private void UpdateAllSpeakersMuteState(bool muteState)
    {
        // Update mute state for all players' speakers, including the local player
        GameObject[] speakers = GameObject.FindGameObjectsWithTag("Speaker");
        foreach (var speaker in speakers)
        {
            AudioSource audioSource = speaker.GetComponent<AudioSource>();
            if (audioSource != null)
            {
                audioSource.mute = muteState; // Respect the global mute state
            }
            else
            {
                Debug.LogError("AudioSource component not found on a Speaker GameObject.");
            }
        }
    }


    public override void OnPlayerEnteredRoom(Photon.Realtime.Player newPlayer)
    {
        base.OnPlayerEnteredRoom(newPlayer);

        // Update the new player's speaker to match the local player's mute state
        UpdateNewPlayerSpeakerMuteState(newPlayer);
    }

    private void UpdateNewPlayerSpeakerMuteState(Photon.Realtime.Player newPlayer)
    {
        // Find the GameObject associated with the new player based on their PhotonView
        GameObject newPlayerObject = newPlayer.TagObject as GameObject;

        if (newPlayerObject != null)
        {
            // Find the speaker associated with the new player
            AudioSource newAudioSource = newPlayerObject.GetComponent<AudioSource>();
            if (newAudioSource != null)
            {
                // Set the new player's speaker mute state based on the local player's mute state
                newAudioSource.mute = isMuted;
            }
            else
            {
                Debug.LogError("AudioSource component not found on the new player's Speaker GameObject.");
            }
        }
        else
        {
            Debug.LogError("New player's GameObject not found.");
        }
    }

}
