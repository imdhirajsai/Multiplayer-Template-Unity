using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using UnityEngine.EventSystems;

public class TeleportOnClickWithGizmo : MonoBehaviourPun
{
    public float teleportDistanceThreshold = 0.1f; // Minimum distance to trigger teleport
    public LayerMask groundMask; // Define which layers are valid ground for teleport
    public Transform cameraTransform;
    public Color gizmoColor = Color.green; // Color for the gizmo
    public GameObject teleportIndicatorPrefab; // The 3D mesh prefab to show at the mouse location

    private GameObject teleportIndicator; // Instance of the 3D mesh
    private Vector3 lastPosition; // To track the player's previous position
    private bool canTeleport = true; // To check if teleport is allowed
    private CharacterController characterController; // Reference to the CharacterController
    private Vector3 teleportTargetPoint = Vector3.zero; // The potential teleport target position
    private bool isValidTeleportTarget = false; // Whether we have a valid teleport target

    void Start()
    {
        if (!photonView.IsMine)
        {
            // Disable this script for remote players
            this.enabled = false;
            return;
        }

        characterController = GetComponent<CharacterController>(); // Get the CharacterController component
        lastPosition = transform.position; // Initialize lastPosition

        // Instantiate the teleport indicator (3D mesh) and deactivate it at the start
        teleportIndicator = Instantiate(teleportIndicatorPrefab);
        teleportIndicator.SetActive(false); // Hide it initially
    }

    void Update()
    {
        if (!photonView.IsMine)
            return;

        // If the cursor is locked, don't allow teleport or show the teleport indicator
        if (Cursor.lockState == CursorLockMode.Locked)
        {
            HideTeleportIndicator(); // Ensure the indicator is hidden if the cursor is locked
            return;
        }

        if (Input.GetMouseButton(1))
        {
            HideTeleportIndicator();
            return;
        }

        HandleTeleport();
        HandleCameraRotation();
        CheckMovementState();
    }

    void HandleTeleport()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        // Check if we're pointing at valid ground
        if (Physics.Raycast(ray, out hit, Mathf.Infinity))
        {
            // Check if the hit object is in the groundMask
            if ((groundMask & (1 << hit.collider.gameObject.layer)) != 0)
            {
                Vector3 targetPoint = hit.point; // Point where the ray hit
                isValidTeleportTarget = Vector3.Distance(transform.position, targetPoint) > teleportDistanceThreshold;

                if (isValidTeleportTarget)
                {
                    teleportTargetPoint = targetPoint; // Store the valid teleport point
                    ShowTeleportIndicator(targetPoint); // Show the 3D mesh at the target point
                }
                else
                {
                    HideTeleportIndicator(); // Hide the indicator if invalid
                    teleportTargetPoint = Vector3.zero; // Reset target if invalid
                }

                // Check if the mouse is not over a UI element
                if (!IsPointerOverUI())
                {
                    // Teleport on left click if player is allowed to teleport
                    if (Input.GetMouseButtonDown(0) && canTeleport && isValidTeleportTarget)
                    {
                        // Calculate the direction to the target point before teleporting
                        Vector3 directionToTarget = (teleportTargetPoint - transform.position).normalized;

                        // Debugging log
                        Debug.Log($"Teleporting to: {teleportTargetPoint}, Direction: {directionToTarget}");

                        // Check if the direction is not zero before applying rotation
                        if (directionToTarget != Vector3.zero)
                        {
                            characterController.enabled = false; // Temporarily disable CharacterController
                            transform.position = teleportTargetPoint; // Move player to the target point

                            // Calculate rotation only around the Y-axis
                            Quaternion targetRotation = Quaternion.LookRotation(new Vector3(directionToTarget.x, 0, directionToTarget.z));

                            // Apply rotation to the current GameObject
                            transform.rotation = targetRotation; // Apply rotation to the current GameObject
                            Debug.Log($"Rotating Current Object to: {targetRotation.eulerAngles}");

                            characterController.enabled = true; // Re-enable CharacterController
                            HideTeleportIndicator();
                        }
                        else
                        {
                            Debug.LogWarning("Direction to target is zero, not applying rotation.");
                        }
                    }
                }
            }
            else
            {
                // The hit object is not in the groundMask, hide the indicator and reset teleport target
                isValidTeleportTarget = false; // No valid target if raycast didn't hit the ground
                HideTeleportIndicator(); // Hide the indicator when not valid
            }
        }
        else
        {
            isValidTeleportTarget = false; // No valid target if raycast didn't hit anything
            HideTeleportIndicator(); // Hide the indicator when not valid
        }
    }

    private bool IsPointerOverUI()
    {
        PointerEventData eventData = new PointerEventData(EventSystem.current) { position = Input.mousePosition };
        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        foreach (RaycastResult result in results)
        {
            // If the object is tagged with "TeleportBlock", return true to indicate we're over a UI element
            if (result.gameObject.CompareTag("TeleportBlock"))
            {
                HideTeleportIndicator();
                return true; // Treat as being over UI, preventing teleport
            }
        }

        return false; // No relevant UI elements found
    }

    void ShowTeleportIndicator(Vector3 position)
    {
        if (teleportIndicator != null)
        {
            teleportIndicator.transform.position = position; // Move the indicator to the target point
            teleportIndicator.SetActive(true); // Show the indicator
        }
    }

    void HideTeleportIndicator()
    {
        if (teleportIndicator != null)
        {
            teleportIndicator.SetActive(false); // Hide the indicator
        }
    }

    void HandleCameraRotation()
    {
        // Rotate camera on right-click
        if (Input.GetMouseButton(1))
        {
            float mouseX = Input.GetAxis("Mouse X");
            float mouseY = Input.GetAxis("Mouse Y");

            // Rotate around Y-axis for horizontal mouse movement
            cameraTransform.Rotate(Vector3.up * mouseX);
            // Rotate around X-axis for vertical mouse movement
            cameraTransform.Rotate(Vector3.left * mouseY);
        }
    }

    void CheckMovementState()
    {
        // Compare current position to the last recorded position to determine if the player is moving
        if (Vector3.Distance(transform.position, lastPosition) > teleportDistanceThreshold)
        {
            canTeleport = false; // Disable teleport if player is moving
        }
        else
        {
            canTeleport = true; // Enable teleport if player is not moving
        }

        // Update the last position to the current position
        lastPosition = transform.position;
    }

    // Draw gizmo in scene view when there's a valid teleport target
    void OnDrawGizmos()
    {
        if (isValidTeleportTarget)
        {
            // Set the color for the gizmo
            Gizmos.color = gizmoColor;

            // Draw a wire sphere or disc at the teleport target position on the ground
            Gizmos.DrawWireSphere(teleportTargetPoint, 0.5f); // You can adjust the size (0.5f) as needed
        }
    }
}
