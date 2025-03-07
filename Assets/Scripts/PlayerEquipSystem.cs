using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;
using System.Linq;
using static PlayerEquipSystem;
using System;
using TMPro;
using ExitGames.Client.Photon;

public class PlayerEquipSystem : MonoBehaviourPun, IInRoomCallbacks
{
    private GameObject UIObject;
    public Button updatePlayerInfoButton;
    public Button casualIndexButton;
    public Button afterSliceIndexButton;
    public GameObject colorButtonContainer;
    public Color[] colorOptions;
    public GameObject billboardCanvas;

    [System.Serializable]
    public class MeshType
    {
        public string meshName;
        public Transform rootBone;
        public List<SkinnedMeshRenderer> meshes;
        public Button[] buttons;
        public int casualIndex;      // Index before the slice point
        public int afterSliceIndex;  // Index after the slice point
        public int slicePoint;       // Defines where the slice happens
        public int[] colorChangeArray;
        public int colorChangeIndex = -1;
        public bool shouldSlice = true;

    }

    

    public List<MeshType> meshTypes;
    private Dictionary<string, SkinnedMeshRenderer> currentMeshes = new Dictionary<string, SkinnedMeshRenderer>();
    private Dictionary<string, int> meshIndexes = new Dictionary<string, int>();
    private Dictionary<string, float> lastChangeTimes = new Dictionary<string, float>();
    private Dictionary<string, Transform> playerBonesDict;
    private Dictionary<(string meshType, int colorChangeIndex), int> colorData = new Dictionary<(string, int), int>();
    // Tracking the previous selections
    private Dictionary<string, int> previousMeshData = new Dictionary<string, int>();



    [Header("Player Bones")]
    public Transform[] playerBonesArray;

    
    private float changeCooldown = 0.5f;
    private float lastChangeTime = 0f;
    private PlayerManager playerManager; // Class-level variable
    private UIManagement UI; // Class-level variable


    private void Awake()
    {
        UIObject = GameObject.FindGameObjectWithTag("UI"); // Find UI object by tag
        if (UIObject != null)
        {
            UI = UIObject.GetComponent<UIManagement>();
            playerManager = UIObject.GetComponent<PlayerManager>(); // Assign here
           

            if (playerManager == null)
            {
                Debug.LogError("PlayerManager component not found on UIObject.");
            }
        }
        else
        {
            Debug.LogError("UIObject not found in the scene. Ensure it is tagged as 'UI'.");
        }

        InitializeBoneDictionary();
        if (playerBonesArray == null || playerBonesArray.Length == 0)
        {
            Debug.LogError("playerBonesArray is not set or empty! Please assign bones in the inspector.");
        }
    }
    void Start()
    {
        InitializeBoneDictionary();
        InitializeMeshIndexes();
        SetupUpdateButton();
        SetupIndexButtons();
        SetupButtons();
        PhotonNetwork.AddCallbackTarget(this);

        // Sync the player's customization to others after spawning
        if (photonView.IsMine)
        {
            SyncColorChangeIndices();
            SyncMeshandColorWithOthers();
            billboardCanvas.SetActive(false);
        }
        Transform playerTextTransform = billboardCanvas.transform.Find("PlayerName");
        TextMeshProUGUI playerText = playerTextTransform.GetComponent<TextMeshProUGUI>();
        playerText.text = photonView.Owner.NickName;

    }

    private void SetupIndexButtons()
    {
        if (casualIndexButton != null)
        {
            casualIndexButton.onClick.AddListener(SetMeshIndicesToCasual);
        }
        else
        {
            Debug.LogError("casualIndexButton is not assigned!");
        }

        if (afterSliceIndexButton != null)
        {
            afterSliceIndexButton.onClick.AddListener(SetMeshIndicesToAfterSlice);
        }
        else
        {
            Debug.LogError("afterSliceIndexButton is not assigned!");
        }
    }

    private void SetMeshIndicesToCasual()
    {
        for (int i = 0; i < meshTypes.Count; i++)
        {
            ChangeMesh(i, meshTypes[i].casualIndex);
        }

    }

    private void SetMeshIndicesToAfterSlice()
    {
        for (int i = 0; i < meshTypes.Count; i++)
        {
            ChangeMesh(i, meshTypes[i].afterSliceIndex);
        }

    }

    void Update()
    {
        
        if (!photonView.IsMine && PhotonNetwork.IsConnected)
        {
            billboardCanvas.SetActive(true);
            BillBoard billBoard = billboardCanvas.GetComponentInChildren<BillBoard>();
            Transform otherPlayerCameraRoot = gameObject.transform.Find("PlayerCameraRoot"); // Find the other player's camera root
            // Now set your local player's cameraRoot transform to this other player's cameraRoot
            GameObject myPlayer = PhotonNetwork.LocalPlayer.TagObject as GameObject; // Get your local player object (if properly set in Photon)

            if (myPlayer != null)
            {
                Transform myCameraRoot = myPlayer.transform.Find("PlayerCameraRoot"); // Find your own player's camera root
                if (myCameraRoot != null && otherPlayerCameraRoot != null)
                {
                    // Update my local player's cameraRoot to match the other player's cameraRoot
                    otherPlayerCameraRoot = myCameraRoot.transform;
                }
                billBoard.SetCamera(otherPlayerCameraRoot);
            }
            
        }
        else
        {
            Debug.Log("local camera is not set");
        }
    }

    

    void OnDestroy()
    {
        PhotonNetwork.RemoveCallbackTarget(this);
    }

    private void InitializeBoneDictionary()
    {
        playerBonesDict = new Dictionary<string, Transform>();
        foreach (Transform bone in playerBonesArray)
        {
            playerBonesDict.Add(bone.name, bone);
        }
    }

    private void InitializeMeshIndexes()
    {
        foreach (var meshType in meshTypes)
        {
            if (meshType.meshes.Count > 0)
            {
                // For local player not connected to the network
                if (!PhotonNetwork.IsConnectedAndReady && photonView.IsMine)
                {
                    meshIndexes[meshType.meshName] = meshType.casualIndex; // Set to default index
                    ChangeMesh(meshTypes.IndexOf(meshType), meshType.casualIndex); // Apply default mesh
                }
                // For local player connected to the network
                else if (PhotonNetwork.IsConnectedAndReady && photonView.IsMine)
                {
                    // Retrieve stored indexes to sync that data
                    int storedIndex = PlayerManager.Instance.meshIndexes[meshType.meshName];
                    meshIndexes[meshType.meshName] = storedIndex; // Update with stored index
                    ChangeMesh(meshTypes.IndexOf(meshType), storedIndex); // Apply stored mesh
                }
                
            }
            else
            {
                Debug.LogWarning("No meshes available for mesh type: " + meshType.meshName);
                meshIndexes[meshType.meshName] = -1;
            }
        }
    }

    public void ChangeMesh(int meshTypeIndex, int buttonIndex)
    {
        Debug.Log($"ChangeMesh called with meshTypeIndex: {meshTypeIndex}, buttonIndex: {buttonIndex}");

        // Skip if meshTypeIndex is invalid or buttonIndex is invalid
        if (meshTypeIndex < 0 || meshTypeIndex >= meshTypes.Count)
        {
            Debug.LogError("Invalid mesh type index: " + meshTypeIndex);
            return;
        }

        MeshType meshType = meshTypes[meshTypeIndex];

        if (meshType.meshes.Count == 0)
        {
            Debug.LogWarning("No meshes available for mesh type: " + meshType.meshName);
            return;
        }

        // Validate buttonIndex against available meshes
        if (buttonIndex < 0 || buttonIndex >= meshType.meshes.Count)
        {
            Debug.LogError($"Invalid button index: {buttonIndex} for mesh type: {meshType.meshName}");
            return;
        }

        // Store the current mesh type and index in previousMeshData
        string meshKey = meshType.meshName; // Use mesh name as the key
        previousMeshData[meshKey] = buttonIndex; // Save the button index

        // Calculate the index of the mesh
        int index = buttonIndex % meshType.meshes.Count;
        meshIndexes[meshType.meshName] = index;

        // Local update
        SkinnedMeshRenderer currentMesh;
        if (currentMeshes.TryGetValue(meshType.meshName, out currentMesh))
        {
            Debug.Log("Changing existing mesh");
            AttachItemToPlayer(meshType.meshes[index], meshType.rootBone, ref currentMesh);
            currentMeshes[meshType.meshName] = currentMesh; // Update the dictionary
        }
        else
        {
            Debug.Log("Adding new mesh");
            currentMesh = null;
            AttachItemToPlayer(meshType.meshes[index], meshType.rootBone, ref currentMesh);
            currentMeshes.Add(meshType.meshName, currentMesh); // Store the new mesh
        }

    }

    public void AttachItemToPlayer(SkinnedMeshRenderer itemMesh, Transform rootBone, ref SkinnedMeshRenderer currentMesh)
    {
        // Check if the item mesh is null
        if (itemMesh == null)
        {
            Debug.LogError("Item mesh is null! Destroying current mesh if it exists.");
            if (currentMesh != null)
            {
                Destroy(currentMesh.gameObject); // Clean up if current mesh exists
                currentMesh = null; // Reset current mesh to null
            }
            return;
        }

        // Check if the root bone is null
        if (rootBone == null)
        {
            Debug.LogError("Root bone is null!");
            return;
        }

        // Check if playerBonesDict is initialized
        if (playerBonesDict == null)
        {
            Debug.LogError("playerBonesDict is not initialized!");
            return;
        }

        Debug.Log($"Attaching item mesh: {itemMesh.name}, to root bone: {rootBone.name}, currentMesh: {currentMesh?.name}");

        // Avoid recreating the mesh if it's already attached
        if (currentMesh != null && currentMesh.sharedMesh == itemMesh.sharedMesh)
        {
            Debug.Log("Item mesh is already attached.");
            return; // Early exit if the same mesh is already attached
        }

        // Destroy the existing mesh if it exists
        if (currentMesh != null)
        {
            Debug.Log($"Destroying existing mesh: {currentMesh.name}");
            Destroy(currentMesh.gameObject);
        }

        // Instantiate the new mesh
        SkinnedMeshRenderer newMesh = Instantiate(itemMesh);
        Transform[] newBones = new Transform[itemMesh.bones.Length];

        // Setting up bones for the new mesh
        for (int i = 0; i < itemMesh.bones.Length; i++)
        {
            if (itemMesh.bones[i] == null)
            {
                Debug.LogError($"Bone at index {i} in itemMesh is null.");
                continue; // You might want to handle this differently
            }

            if (playerBonesDict.TryGetValue(itemMesh.bones[i].name, out Transform bone))
            {
                newBones[i] = bone;
            }
            else
            {
                Debug.LogError("Player bones Dictionary does not contain bone: " + itemMesh.bones[i].name);
                newBones[i] = rootBone; // Fallback to rootBone if bone not found
            }
        }

        // Check for any null bones after the setup
        foreach (var bone in newBones)
        {
            if (bone == null)
            {
                Debug.LogError("A bone is null after setup. This may cause issues with mesh attachment.");
            }
        }

        newMesh.bones = newBones;
        newMesh.rootBone = rootBone;

        // Ensure rootBone has a valid parent
        if (rootBone.parent == null)
        {
            Debug.LogError("Root bone has no parent!");
            return;
        }

        newMesh.transform.SetParent(rootBone.parent);
        newMesh.transform.localPosition = Vector3.zero; // Reset local position
        newMesh.transform.localRotation = Quaternion.identity; // Reset local rotation
        newMesh.enabled = true;

        Debug.Log($"New mesh instantiated: {newMesh.name}, is active: {newMesh.gameObject.activeSelf}");

        currentMesh = newMesh; // Update the reference to the current mesh
        Debug.Log($"Updated current mesh to: {currentMesh.name}");
    }



    [PunRPC]
    public void RPC_ChangeMesh(int meshTypeIndex, int buttonIndex)
    {
        ChangeMesh(meshTypeIndex, buttonIndex);
    }

    private void SyncMeshandColorWithOthers()
    {
        foreach (var meshType in meshTypes)
        {
            int meshTypeIndex = meshTypes.IndexOf(meshType);
            int currentIndex = meshIndexes[meshType.meshName];

            // Sync the mesh with all other players
            photonView.RPC("RPC_SyncNewPlayerMesh", RpcTarget.Others, meshTypeIndex, currentIndex);

            // Check if color data exists for the current mesh type
            if (colorData.TryGetValue((meshType.meshName, meshType.colorChangeIndex), out int colorIndex))
            {
                // Sync the color with all other players
                photonView.RPC("RPC_ChangeMeshColor", RpcTarget.All, meshType.meshName, colorIndex);
                Debug.Log("Synced Local Data to all ");
            }

        }

    }

    #region ColorChangeandSyncing
    private void CheckColorIndex(int meshTypeIndex, int buttonIndex)
    {
        // Check if the meshTypeIndex is valid
        if (meshTypeIndex < 0 || meshTypeIndex >= meshTypes.Count)
        {
            Debug.LogError($"Invalid meshTypeIndex: {meshTypeIndex}");
            return;
        }

        MeshType meshType = meshTypes[meshTypeIndex];

        if (Array.Exists(meshType.colorChangeArray, index => index == buttonIndex))
        {

            Debug.Log($"Activating color button container for mesh type: {meshType.meshName} with button index: {buttonIndex}");

            colorButtonContainer.SetActive(true);

            Button[] colorButtons = colorButtonContainer.GetComponentsInChildren<Button>();

            if (colorButtons.Length != colorOptions.Length)
            {
                Debug.LogError($"Mismatch: Number of color buttons ({colorButtons.Length}) does not match number of colors ({colorOptions.Length}). Please adjust them in the Inspector.");
                return;
            }

            foreach (var button in colorButtons)
            {
                button.onClick.RemoveAllListeners();
            }

            for (int i = 0; i < colorButtons.Length; i++)
            {
                int colorIndex = i;
                colorButtons[colorIndex].onClick.AddListener(() => ChangeMeshColor(meshTypeIndex, colorIndex));
            }

            Debug.Log("Color buttons setup completed successfully.");
            meshType.colorChangeIndex = buttonIndex;
        }
        else
        {
            // If it does not match, deactivate the color button container
            colorButtonContainer.SetActive(false);
            Debug.Log($"Deactivating color button container for mesh type: {meshType.meshName} with button index: {buttonIndex}");
        }
    }

    private void LogColorChange(string meshName, Color newColor, int colorIndex, int playerId)
    {
        Debug.Log($"Player {playerId} changed color for {meshName} to {newColor} (Index: {colorIndex}) at {System.DateTime.Now}");
    }


    public void ChangeMeshColor(int meshTypeIndex, int colorIndex)
    {
        if (meshTypeIndex < 0 || meshTypeIndex >= meshTypes.Count)
        {
            Debug.LogError($"Invalid meshTypeIndex: {meshTypeIndex}");
            return;
        }

        if (colorIndex < 0 || colorIndex >= colorOptions.Length)
        {
            Debug.LogError($"Invalid color index: {colorIndex}");
            return;
        }

        MeshType meshType = meshTypes[meshTypeIndex];

        if (currentMeshes.TryGetValue(meshType.meshName, out SkinnedMeshRenderer currentMesh))
        {
            // Clone the material if necessary
            if (currentMesh.materials[0] == currentMesh.sharedMaterial)
            {
                currentMesh.materials[0] = new Material(currentMesh.materials[0]);
            }

            Material[] materials = currentMesh.materials;

            if (materials.Length > 0)
            {
                Debug.Log($"Changing color of material {materials[0].name} on {meshType.meshName} to {colorOptions[colorIndex]}");

                materials[0].color = colorOptions[colorIndex];
                Debug.Log($"Changed color of {meshType.meshName} to {colorOptions[colorIndex]}");

                LogColorChange(meshType.meshName, colorOptions[colorIndex], colorIndex, PhotonNetwork.LocalPlayer.ActorNumber);


                // Store the color index in the colorData dictionary
                colorData[(meshType.meshName, meshType.colorChangeIndex)] = colorIndex;
                Debug.Log($"Stored color index for {meshType.meshName}, index {meshType.colorChangeIndex}: {colorIndex}");
            }
            else
            {
                Debug.LogWarning($"No materials found on mesh: {meshType.meshName}");
            }
        }
        else
        {
            Debug.LogWarning($"No current mesh found for type: {meshType.meshName}");
        }
    }

    [PunRPC]
    public void RPC_ChangeMeshColor(string meshName, int colorIndex)
    {
        // Log the incoming RPC call
        Debug.Log($"Received RPC to change color for mesh: {meshName} with index: {colorIndex}");

        // Find the corresponding mesh type by name
        var meshType = meshTypes.FirstOrDefault(m => m.meshName == meshName);

        // Debug available mesh types
        if (meshType == null)
        {
            Debug.LogWarning($"No meshType found for name: {meshName}. Available mesh types: {string.Join(", ", meshTypes.Select(m => m.meshName))}");
            return; // Early exit if meshType not found
        }

        // Get the current mesh
        if (!currentMeshes.TryGetValue(meshType.meshName, out SkinnedMeshRenderer currentMesh))
        {
            Debug.LogWarning($"No current mesh found for type: {meshType.meshName}. Current meshes: {string.Join(", ", currentMeshes.Keys)}");
            return; // Early exit if mesh not found
        }

        // Clone the material if necessary
        if (currentMesh.materials[0] == currentMesh.sharedMaterial)
        {
            currentMesh.materials[0] = new Material(currentMesh.materials[0]);
        }

        Material[] materials = currentMesh.materials;

        if (materials.Length > 0 && colorIndex >= 0 && colorIndex < colorOptions.Length)
        {
            Debug.Log($"Changing color of material {materials[0].name} on {meshType.meshName} to {colorOptions[colorIndex]} in RPC");

            materials[0].color = colorOptions[colorIndex]; // Apply the new color
            Debug.Log($"RPC Changed color of {meshType.meshName} to {colorOptions[colorIndex]}");
            LogColorChange(meshType.meshName, colorOptions[colorIndex], colorIndex, PhotonNetwork.LocalPlayer.ActorNumber);
        }
        else
        {
            if (materials.Length == 0)
            {
                Debug.LogWarning($"No materials found on mesh: {meshType.meshName}");
            }
            if (colorIndex < 0 || colorIndex >= colorOptions.Length)
            {
                Debug.LogWarning($"Invalid color index: {colorIndex}. Valid range is 0 to {colorOptions.Length - 1}");
            }
        }
    }


    public void SyncColorChangeIndices()
    {
        Debug.Log("Starting to sync colorChangeIndices from PlayerManager.");

        // Log the current color data in PlayerManager
        LogColorDataInPlayerManager();

        // Sync PlayerManager color data to local colorData
        SyncColorDataFromPlayerManager();

        // Create a list of colorData keys to safely iterate over
        var colorDataKeys = new List<(string meshType, int colorChangeIndex)>(colorData.Keys);

        // Loop through each mesh type
        foreach (var meshType in meshTypes) // Ensure meshTypes is properly defined
        {
            Debug.Log($"Checking mesh type: {meshType.meshName}");

                // Attempt to retrieve the correct colorChangeIndex from local colorData using meshName and index
                foreach (var key in colorDataKeys) // Safely iterating over the keys
                {
                    // Check if the meshType's name matches the key in local colorData
                    if (key.meshType == meshType.meshName)
                    {
                        // Update the current colorChangeIndex with the stored one from local colorData
                        int storedColorOptionIndex = colorData[key];

                        // Log the update for debugging purposes
                        Debug.Log($"Updated colorChangeIndex for {meshType.meshName} at index {meshType.colorChangeIndex} to use color option {storedColorOptionIndex}");

                        // Change the mesh color based on the updated color option index
                        ChangeMeshColor(meshTypes.IndexOf(meshType), storedColorOptionIndex);
                    }
                }
            
        }
    }


    private void SyncColorDataFromPlayerManager()
    {
        // Clear local colorData to avoid duplication
        colorData.Clear();

        // Iterate through PlayerManager's color data and copy it to the local dictionary
        foreach (var entry in PlayerManager.Instance.colorData)
        {
            // Assuming entry.Key is a tuple (string, int) and entry.Value is the corresponding color option index
            colorData[entry.Key] = entry.Value;
        }

        // Debug log to show the synced color data
        Debug.Log("Color data synced from PlayerManager to local colorData.");

        // Additional debug log to display the contents of the local colorData
        Debug.Log("Current contents of local colorData:");
        foreach (var entry in colorData)
        {
            Debug.Log($"Mesh Type: {entry.Key.meshType}, Color Change Index: {entry.Key.colorChangeIndex}, Color Option Index: {entry.Value}");
        }
    }



    // Logging function to verify color data in PlayerManager
    private void LogColorDataInPlayerManager()
    {
        var colorData = PlayerManager.Instance.colorData;

        if (colorData != null && colorData.Count > 0)
        {
            Debug.Log("Current color data in PlayerManager:");
            foreach (var entry in colorData)
            {
                Debug.Log($"Mesh: {entry.Key.Item1}, ColorChangeIndex: {entry.Key.Item2} => colorOptionIndex: {entry.Value}");
            }
        }
        else
        {
            Debug.Log("No color data found in PlayerManager.");
        }
    }
    #endregion

   

    public void ChangeMeshByName(string meshType, int selectedIndex)
    {
        int meshTypeIndex = meshTypes.FindIndex(m => m.meshName.Equals(meshType, System.StringComparison.OrdinalIgnoreCase));
        if (meshTypeIndex >= 0)
        {
            ChangeMesh(meshTypeIndex, selectedIndex);
        }
        else
        {
            Debug.LogError($"Mesh type '{meshType}' not found.");
        }
    }


    public void OnPlayerEnteredRoom(Player newPlayer)
    {
        Debug.Log($"New player entered: {newPlayer.NickName}. Sending customization data.");
        foreach (var meshType in meshTypes)
        {
            int meshTypeIndex = meshTypes.IndexOf(meshType);
            int currentIndex = meshIndexes[meshType.meshName];
            photonView.RPC("RPC_ChangeMesh", newPlayer, meshTypeIndex, currentIndex);
            if (colorData.TryGetValue((meshType.meshName, meshType.colorChangeIndex), out int colorOptionIndex))
            {
                photonView.RPC("RPC_ChangeMeshColor", newPlayer, meshType.meshName, colorOptionIndex);
            }
        }

    }

    [PunRPC]
    public void RPC_SyncNewPlayerMesh(int meshTypeIndex, int buttonIndex)
    {
        Debug.Log($"Syncing new player's mesh: meshTypeIndex={meshTypeIndex}, buttonIndex={buttonIndex}");

        MeshType meshType = meshTypes[meshTypeIndex];
        if (meshType != null)
        {
            ChangeMesh(meshTypeIndex, buttonIndex);  // Apply the mesh locally
            Debug.Log($"Applied mesh for {meshType.meshName} with index {buttonIndex}");
        }
        else
        {
            Debug.LogError("MeshType is null! Unable to sync new player's mesh.");
        }
    }

    private void SetupUpdateButton()
    {
        if (updatePlayerInfoButton != null)
        {
            updatePlayerInfoButton.onClick.AddListener(OnUpdateButtonClick);
        }
        else
        {
            Debug.LogWarning("Update Player Info Button is not assigned in the inspector.");
        }
    }

    private void OnUpdateButtonClick()
    {
        if (gameObject.activeInHierarchy)
        {
            StoreMeshIndexesInPlayerManager();
        }
        else
        {
            Debug.LogWarning("Button clicked, but the GameObject is inactive.");
        }
    }

    private void SetupButtons()
    {
        Debug.Log("SetupButtons called");

        foreach (var meshType in meshTypes)
        {
            if (meshType.buttons != null && meshType.buttons.Length > 0)
            {
                for (int j = 0; j < meshType.buttons.Length; j++)
                {
                    if (meshType.buttons[j] != null) // Check if button is assigned
                    {
                        int meshTypeIndex = meshTypes.IndexOf(meshType);
                        int buttonIndex = j;

                        // Assign the button click listener
                        meshType.buttons[j].onClick.AddListener(() =>
                        {

                            if (meshType.shouldSlice)
                            {
                                UpdateSlice(meshTypeIndex, buttonIndex); // Call UpdateSlice here
                            }
                            ChangeMesh(meshTypeIndex, buttonIndex);
                            CheckColorIndex(meshTypeIndex, buttonIndex); // Call to check color index
                        });
                    }
                    else
                    {
                        Debug.LogWarning($"Button at index {j} for MeshType '{meshType.meshName}' is not assigned.");
                    }
                }
            }
            else
            {
                Debug.LogError($"MeshType '{meshType.meshName}' has no buttons assigned.");
            }
        }
    }

    private void UpdateSlice(int meshTypeIndex, int buttonIndex)
    {
        MeshType meshType = meshTypes[meshTypeIndex];

        // Get previous mesh data
        string meshKey = meshType.meshName;
        int previousButtonIndex;

        // Retrieve previous button index from previousMeshData
        previousMeshData.TryGetValue(meshKey, out previousButtonIndex);

        Debug.Log($"UpdateSlice called with meshTypeIndex: {meshTypeIndex}, buttonIndex: {buttonIndex}");
        Debug.Log($"Previous button index: {previousButtonIndex}");

        bool previousIsBeforeSlice = previousButtonIndex <= meshType.slicePoint;
        bool currentIsBeforeSlice = buttonIndex <= meshType.slicePoint;
        bool previousIsAfterSlice = previousButtonIndex > meshType.slicePoint;
        bool currentIsAfterSlice = buttonIndex > meshType.slicePoint;

        if (previousIsBeforeSlice && currentIsAfterSlice)
        {
            // Previous was before slice, and current is after slice
            Debug.Log("Updating all mesh types to after-slice indices.");
            for (int i = 0; i < meshTypes.Count; i++)
            {
                if (i != meshTypeIndex)
                {
                    ChangeMesh(i, meshTypes[i].afterSliceIndex);
                }
            }
        }
        else if (previousIsAfterSlice && currentIsBeforeSlice)
        {
            // Previous was after slice, and current is before slice
            Debug.Log("Updating all mesh types to casual indices.");
            for (int i = 0; i < meshTypes.Count; i++)
            {
                if (i != meshTypeIndex)
                {
                    ChangeMesh(i, meshTypes[i].casualIndex);
                }
            }
        }
    }

    public void StoreMeshIndexesInPlayerManager()
    {
        if (playerManager != null)
        {
            // Log before storing mesh indexes
            playerManager.StoreAndSyncMeshIndexes(meshIndexes);
            Debug.Log("Saved Mesh Data");

            // Log the color data
            Debug.Log($"Color data count before storing: {colorData.Count}");

            // Log each entry in colorData
            foreach (var entry in colorData)
            {
                Debug.Log($"Color entry: MeshType: {entry.Key.meshType}, ColorChangeIndex: {entry.Key.colorChangeIndex}, Color: {entry.Value}");
            }

            // Call to store color data
            playerManager.StoreAndSyncColorData(colorData);
            Debug.Log("Saved Color Data");

            Debug.Log("Mesh indexes stored in PlayerManager.");
        }
        else
        {
            Debug.LogError("PlayerManager instance not found!");
        }

        playerManager.SetPlayerDetails(UI.Rename, gameObject, gameObject.transform.parent.gameObject);
        
    }

    public void OnPlayerLeftRoom(Player otherPlayer) { }
    public void OnRoomPropertiesUpdate(ExitGames.Client.Photon.Hashtable propertiesThatChanged) { }
    public void OnPlayerPropertiesUpdate(Player targetPlayer, ExitGames.Client.Photon.Hashtable changedProps) { }
    public void OnMasterClientSwitched(Player newMasterClient) { }

}
