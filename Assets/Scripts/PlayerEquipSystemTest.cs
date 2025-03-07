using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;
using System.Linq;

public class PlayerEquipSystemTest : MonoBehaviour
{
    public Button casualIndexButton;
    public Button afterSliceIndexButton;

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
        public bool shouldSlice = true;
    }

    public List<MeshType> meshTypes;
    private Dictionary<string, SkinnedMeshRenderer> currentMeshes = new Dictionary<string, SkinnedMeshRenderer>();
    private Dictionary<string, int> meshIndexes = new Dictionary<string, int>();
    private Dictionary<string, Transform> playerBonesDict;

    // Tracking the previous selections
    private Dictionary<string, int> previousMeshData = new Dictionary<string, int>();


    [Header("Player Bones")]
    public Transform[] playerBonesArray;

    [Header("Key Configurations")]
    public KeyCode nextMeshKey = KeyCode.N;
    public KeyCode prevMeshKey = KeyCode.P;

    private void Awake()
    {
        InitializeBoneDictionary();
        if (playerBonesArray == null || playerBonesArray.Length == 0)
        {
            Debug.LogError("playerBonesArray is not set or empty! Please assign bones in the inspector.");
        }
    }

    void Start()
    {
        InitializeMeshIndexes();
        SetupIndexButtons();
        SetupButtons();
        HandleKeyInput();
        PhotonNetwork.AddCallbackTarget(this);
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
                meshIndexes[meshType.meshName] = meshType.casualIndex;
                ChangeMesh(meshTypes.IndexOf(meshType), meshType.casualIndex);
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
        // Check if updates should be ignored
       
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



    public void AttachItemToPlayer(SkinnedMeshRenderer itemMesh, Transform rootBone, ref SkinnedMeshRenderer currentMesh)
    {
        if (itemMesh == null)
        {
            if (currentMesh != null)
            {
                Destroy(currentMesh.gameObject);
                currentMesh = null;
            }
            return;
        }

        if (rootBone == null) return;

        if (currentMesh != null && currentMesh.sharedMesh == itemMesh.sharedMesh) return;

        if (currentMesh != null)
        {
            Destroy(currentMesh.gameObject);
        }

        SkinnedMeshRenderer newMesh = Instantiate(itemMesh);
        Transform[] newBones = new Transform[itemMesh.bones.Length];

        for (int i = 0; i < itemMesh.bones.Length; i++)
        {
            if (itemMesh.bones[i] == null) continue;

            if (playerBonesDict.TryGetValue(itemMesh.bones[i].name, out Transform bone))
            {
                newBones[i] = bone;
            }
            else
            {
                newBones[i] = rootBone;
            }
        }

        newMesh.bones = newBones;
        newMesh.rootBone = rootBone;
        newMesh.transform.SetParent(rootBone.parent);
        newMesh.transform.localPosition = Vector3.zero;
        newMesh.transform.localRotation = Quaternion.identity;
        newMesh.enabled = true;

        currentMesh = newMesh;
    }

    private void HandleKeyInput()
    {
        if (meshTypes == null || meshTypes.Count == 0)
        {
            return;
        }

        for (int i = 0; i < meshTypes.Count; i++)
        {
            if (!meshIndexes.ContainsKey(meshTypes[i].meshName)) continue;

            int currentIndex = meshIndexes[meshTypes[i].meshName];

            if (Input.GetKeyDown(nextMeshKey))
            {
                int nextIndex = (currentIndex + 1) % meshTypes[i].meshes.Count;
                ChangeMesh(i, nextIndex);
                UpdateSlice(i, nextIndex); // Call UpdateSlice after changing mesh
            }

            if (Input.GetKeyDown(prevMeshKey))
            {
                int prevIndex = (currentIndex - 1 + meshTypes[i].meshes.Count) % meshTypes[i].meshes.Count;
                ChangeMesh(i, prevIndex);
                UpdateSlice(i, prevIndex); // Call UpdateSlice after changing mesh
            }
        }
    }
}
