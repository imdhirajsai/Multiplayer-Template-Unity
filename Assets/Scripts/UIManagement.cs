using UnityEngine;
using UnityEngine.UI;
using TMPro;
using StarterAssets;
using Photon.Pun;
using Photon.Voice;
using System.Runtime.CompilerServices;


public class UIManagement : MonoBehaviour
{
    private bool isGenderToggled = false;

    [SerializeField] private GameObject characterUI;
    [SerializeField] private GameObject loginPanel;
    [SerializeField] private GameObject gamePlayPanel;
    [SerializeField] private GameObject connectingPanel;
    public GameObject EmotiBar;

    [SerializeField] private GameObject maleCustomWindow;
    [SerializeField] private GameObject femaleCustomWindow;
    [SerializeField] private GameObject charSelectCam;
    [SerializeField] private GameObject maleCam;
    [SerializeField] private GameObject femaleCam;
    [SerializeField] private GameObject outLine;
    [SerializeField] private GameObject genderText;
    [SerializeField] private GameObject PlayerMale;
    [SerializeField] private GameObject PlayerFemale;
    public string Rename;
    public bool isMuted;

    public Sprite[] speakerIcons;
    public Image speakerToggleButtonIcon;
    [SerializeField] public Button speakerToggleButton;

    public TMP_InputField userNameText;

    // Emote buttons
    public Button helloButton;
    public Button flyingKissButton;
    public Button dance1Button;
    public Button dance2Button;
    public Button dance3Button;
    public Button sadButton;

    private ThirdPersonController playerController; // Reference to the player's controller script
    private PhotonManager photonManager; 
    private bool isCursorLocked = false;
    private bool isMale = false;


    [SerializeField] private Sprite[] maleProfileImages;  // Array for male profile images
    [SerializeField] private Sprite[] femaleProfileImages; // Array for female profile images
    [SerializeField] private Image profilePhotoUI; // UI Image where the profile photo will be displayed


    private void Awake()
    {
        photonManager = GetComponent<PhotonManager>();
    }


    void Start()
    {
        ResetToDefaultState(); // Reset game state at start
        ActivatePanel(loginPanel.name); // Activate login panel at start
        HideEmoteBar();
        
    }

    public void OnLoginClick()
    {
        Rename = userNameText.text;

        if (!string.IsNullOrEmpty(Rename))
        {
            ActivatePanel(genderText.name); // Activate character selection after login
        }
        else
        {
            Debug.LogError("Please Enter the Name");
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape) && isCursorLocked)
        {
            UnlockCursor();
        }


        if (!PhotonNetwork.IsConnected)
        {
            HandleGenderSelection();
        }

        if (PhotonNetwork.IsConnected)
        {
            FetchAnimID();
        }
    }

    public void FetchAnimID()
    {
        // Find all player objects in the scene
        PhotonView[] photonViews = FindObjectsOfType<PhotonView>();

        // Iterate through each PhotonView to find the one owned by the local player
        foreach (PhotonView pv in photonViews)
        {
            // Check if the PhotonView belongs to the local player
            if (pv.IsMine)
            {
                // Attempt to get the ThirdPersonController component from this GameObject
                playerController = pv.GetComponent<ThirdPersonController>();
                if (playerController != null)
                {
                   
                }
                else
                {
                    Debug.LogError("ThirdPersonController component not found on the local player!");
                }
                break; // Exit the loop after finding the local player
            }
        }
    }

    public void ToggleFly()
    {
        playerController.ToggleFlying();
    }

    public void HideEmoteBar()
    {
        EmotiBar.SetActive(!EmotiBar.activeSelf);
    }

    public void LockCursor()
    {
        Cursor.visible = false;
        isCursorLocked = true;
        Cursor.lockState = CursorLockMode.Locked;
    }

    public void ToggleFullScreen()
    {
        Screen.fullScreen = !Screen.fullScreen;
    }

    public void onConnecting()
    {
        ActivatePanel(connectingPanel.name);
    }

    public void ShowGamePlayUI()
    {
        ActivatePanel(gamePlayPanel.name); // Activate gameplay UI
        Debug.Log("Gameplay Panel called");
    }

    public void playFlyingKissEmote()
    {
        playerController.playFlyingKissEmote();
    }

    public void PlayRandomDanceEmote()
    {
        playerController.PlayRandomDanceEmote();
    } 
    
    public void PlayHelloEmote()
    {
        playerController.playHelloEmote();
    }
    
    public void PlaySadEmote()
    {
        playerController.playIsSadEmote();
    }

    public void PlayNamaste()
    {
        if (photonManager == null)
        {
            Debug.LogError("photonManager is not assigned.");
            return;
        }
        isMale = photonManager.anyMale;
        Debug.Log($"Male is: {isMale}");
        if (isMale)
        {
            playerController.playNamasteMale();
        }
        else
        {
            playerController.playNamasteFemale();

        }
    }

    public void ToggleFPS()
    {
        playerController.FPSMode();
    }

    public void ResetToDefaultState()
    {
        DisableAllUIs(); // Disable all UI elements
        DisableAllCameras(); // Disable all cameras
        DisableCustomizationWindows(); // Disable customization windows
        genderText.SetActive(true);
        charSelectCam.SetActive(true); // Enable character selection camera

        outLine.SetActive(true); // Enable outline
        PlayerMale.SetActive(true);
        PlayerFemale.SetActive(true);
    }

    private void HandleGenderSelection()
    {
        // Continue with gender selection logic if not connected
        if (!isGenderToggled && Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            outLine.SetActive(true);

            if (Physics.Raycast(ray, out hit))
            {
                if (hit.collider.CompareTag("PlayerMale"))
                {
                    SetUpMale();
                }
                else if (hit.collider.CompareTag("PlayerFemale"))
                {
                    SetUpFemale();
                }
            }
        }
    }

    public void SetRandomProfilePhoto(bool isMale)
    {
        Sprite randomSprite;

        if (isMale)
        {
            // Choose a random male sprite
            randomSprite = maleProfileImages[Random.Range(0, maleProfileImages.Length)];
        }
        else
        {
            // Choose a random female sprite
            randomSprite = femaleProfileImages[Random.Range(0, femaleProfileImages.Length)];
        }

        // Assign the selected sprite to the profile photo UI
        profilePhotoUI.sprite = randomSprite;
    }

    public void ActivatePanel(string panelName)
    {
        // Set all panels inactive first using the condition
        loginPanel.SetActive(panelName.Equals(loginPanel.name));
        characterUI.SetActive(panelName.Equals(characterUI.name));
        connectingPanel.SetActive(panelName.Equals(connectingPanel.name));
        gamePlayPanel.SetActive(panelName.Equals(gamePlayPanel.name));
        genderText.SetActive(panelName.Equals(genderText.name));
    }

    private void SetUpMale()
    {
        ActivatePanel(characterUI.name); // Ensure character UI is active
        DisableAllCameras();
        DisableCustomizationWindows();
        maleCustomWindow.SetActive(true);
        maleCam.SetActive(true);
        outLine.SetActive(false);
        PlayerFemale.SetActive(false);
        genderText.SetActive(false);

        SetRandomProfilePhoto(true);
    }

    private void SetUpFemale()
    {
        ActivatePanel(characterUI.name); // Ensure character UI is active
        DisableAllCameras();
        DisableCustomizationWindows();
        femaleCustomWindow.SetActive(true);
        femaleCam.SetActive(true);
        outLine.SetActive(false);
        PlayerMale.SetActive(false);
        genderText.SetActive(false);

        SetRandomProfilePhoto(false);
    }

    public void ChangeCharacterMidGame()
    {
        ResetToDefaultState(); // Reset everything
        Debug.Log("Character selection has been reset.");
    }

    private void DisableAllUIs()
    {
        loginPanel.SetActive(false);
        characterUI.SetActive(false);
        gamePlayPanel.SetActive(false);
    }

    private void DisableAllCameras()
    {
        charSelectCam.SetActive(false);
        maleCam.SetActive(false);
        femaleCam.SetActive(false);
    }

    private void DisableCustomizationWindows()
    {
        maleCustomWindow.SetActive(false);
        femaleCustomWindow.SetActive(false);
    }

    private void UnlockCursor()
    {
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        isCursorLocked = false;
    }

   
}

