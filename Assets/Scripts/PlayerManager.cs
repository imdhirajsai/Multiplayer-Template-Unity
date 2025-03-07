using Photon.Pun;
using ExitGames.Client.Photon; // Add this line to use the correct Hashtable
using System.Collections.Generic;
using UnityEngine;
using System;

public class PlayerManager : MonoBehaviourPunCallbacks
{
    public static PlayerManager Instance { get; private set; } // Singleton instance

    [Header("Player Details")]
    public string playerName;           // The player's name
    public GameObject playerObject;     // Reference to the player object
    public bool isMale;                 // True if the player is male
    public bool isFemale;               // True if the player is female

    [Header("Player Mesh Data")]
    public List<string> meshNames = new List<string>();  // Store mesh names
    public Dictionary<string, int> meshIndexes = new Dictionary<string, int>();
    public Dictionary<(string meshType, int colorChangeIndex), int> colorData = new Dictionary<(string, int), int>();
    // Store mesh indexes

    private PhotonManager photonManager;
    private PlayerEquipSystem playerEquipSystem;
    private UIManagement uIManagement;

    private void Awake()
    {
        // Singleton implementation
        if (Instance == null)
        {
            Instance = this; // Set the instance
            DontDestroyOnLoad(gameObject); // Optional: persist across scenes
        }
        else
        {
            Destroy(gameObject); // Destroy this instance if one already exists
        }

        playerEquipSystem = GetComponent<PlayerEquipSystem>();
    }

    void Start()
    {
        photonManager = FindObjectOfType<PhotonManager>();

        if (photonManager == null)
        {
            Debug.LogError("PhotonManager not found in the scene!");
        }
    }

    public void StoreAndSyncMeshIndexes(Dictionary<string, int> newMeshIndexes)
    {
        // Update meshIndexes with the new data
        foreach (var mesh in newMeshIndexes)
        {
            if (meshIndexes.ContainsKey(mesh.Key))
            {
                meshIndexes[mesh.Key] = mesh.Value;  // Update if key exists
            }
            else
            {
                meshIndexes.Add(mesh.Key, mesh.Value);  // Add new entry if key doesn't exist
            }
        }
        Debug.Log("Saved Mesh Data");
    }

    public void StoreAndSyncColorData(Dictionary<(string meshType, int colorChangeIndex), int> newColorData)
    {
        // Update color data with the new data
        foreach (var colorEntry in newColorData)
        {
            var key = colorEntry.Key;  // (meshType, colorChangeIndex)
            var colorIndex = colorEntry.Value;  // Color index

            if (colorData.ContainsKey(key))
            {
                colorData[key] = colorIndex;  // Update if the entry exists
            }
            else
            {
                colorData.Add(key, colorIndex);  // Add new entry if it doesn't exist
            }

            Debug.Log($"Stored color index for {key.meshType}, index {key.colorChangeIndex}: {colorIndex}");
        }
    }



    // Function to set player details (name, gender, etc.) and gather mesh data
    public void SetPlayerDetails(string name, GameObject player, GameObject parent)
        {
        // Set player details
        playerName = name;
        
        isMale = player.CompareTag("PlayerMale");
        isFemale = player.CompareTag("PlayerFemale");

        // Store player's gender in Photon custom properties
        Hashtable playerProperties = new Hashtable // Use Photon Hashtable
        {
            { "isMale", isMale }
        };
        PhotonNetwork.LocalPlayer.SetCustomProperties(playerProperties);

        // Pass player details to PhotonManager
        if (photonManager != null)
        {
            photonManager.getPlayerDetails(isMale, playerName);
            Destroy(parent);  // Destroy local player object, if necessary
        }
        }
}
 