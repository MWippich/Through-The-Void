using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.UI;

public class InteractionHandler : MonoBehaviour {
    [SerializeField] Camera _mainCamera;
    [SerializeField] GameObject _ship;
    [SerializeField] Slider _timeSlider;
    private AudioManager _audioManager;

    [SerializeField] Color selectedColor = Color.cyan;
    [SerializeField] Color brokenColor = Color.red;

    private Color defaultColor = Color.white;
    private Color partColor;

    private FaultHandler faultHandler;

    private Transform previousPart;

    private Dictionary<string, Fault> fixesToFaults = new Dictionary<string, Fault>();
    private Dictionary<string, Fault> faultsToFix = new Dictionary<string, Fault>();

    private bool updateFaultColors = false;

    // Time tracking
    private float timeHeld = 0f;
    private float timeToHold = 3.0f;

    private void Start() {
        partColor = defaultColor;
        faultHandler = GetComponent<FaultHandler>();
        _audioManager = FindObjectOfType<AudioManager>();
    }

    private void Update() {
        RegisterTouch();
        UpdateSlider();

        if (updateFaultColors)
            UpdateFaultColors();
    }

    private void OnDisable() {
        Reset();
    }

    private void UpdateFaultColors() {
        foreach (string fault in faultsToFix.Keys) {
            SetPartColor(fault, brokenColor);
        }

        updateFaultColors = false;
    }

    private void RegisterTouch() {
        if (Input.touchCount <= 0) {
            Reset();
            return; // Return if not touching screen
        }

        Touch touch = Input.GetTouch(0); // Only care about first finger
        partColor = HandlePhase(touch);
        
        Ray ray = _mainCamera.ScreenPointToRay(touch.position);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit)) { // Check if ray hits object
            Transform part = GetPart(hit);
            int partStatus = GetPartStatus(part.name);

            switch (partStatus) {
                case 0: // Part is broken
                    UpdateInformation(GetPart(hit));
                    timeHeld = 0f;
                    break;
                default:
                    if (FixedPart(part)) {
                        if (partStatus == 1) // Part can fix other part
                            FixPart(part.name);
                    }

                    UpdateInformation(GetPart(hit));
                    UpdateColorOnTouch(hit, touch);
                    break;
            }
        } else {
            Reset();
        }
    }

    private void Reset() {
        SetPartColor(previousPart, defaultColor);
        timeHeld = 0f;
    }

    private bool FixedPart(Transform part) {
        if (previousPart == part) {
            timeHeld += Time.deltaTime;

            if (timeHeld > timeToHold) {
                timeHeld = 0f;
                return true;
            }
        } else {
            timeHeld = 0f;
        }

        return false;
    }

    private void FixPart(string partName) {
        if (fixesToFaults.ContainsKey(partName)) {
            Fault fault = fixesToFaults[partName];

            SetPartColor(fault.faultLocation, defaultColor);
            RemoveFault(fault.faultLocation, partName);
            faultHandler.SendMessage(fault.id);
        }
    }

    // Based on current ship model, change based on where collider is
    private Transform GetPart(RaycastHit hit) {
        return hit.collider.transform.parent;
    }

    private void UpdateColorOnTouch(RaycastHit hit, Touch touch) {
        // Change color of part
        Transform part = GetPart(hit); 
        SetPartColor(part, partColor);

        if (HasMovedToNewPart(touch, part)) { // We moved between two different parts
            SetPartColor(previousPart, defaultColor); // Reset color of previously selected part
        }

        previousPart = part; // Update previous part
    }

    private void UpdateInformation(Transform part) {
        if (previousPart != null && previousPart != part) {
            previousPart.Find("Text").gameObject.SetActive(false);
        }
        if (previousPart != part || !part.Find("Text").gameObject.activeInHierarchy) {
            part.Find("Text").gameObject.SetActive(true);
        }
    }

    /*
     Returns:
        0 - if part is broken
        1 - if part can fix broken part
        -1 - if part has default status
     */
    private int GetPartStatus(string partName) {
        if (faultsToFix.ContainsKey(partName))
            return 0;
        if (fixesToFaults.ContainsKey(partName))
            return 1;

        return -1;
    }

    // Has the player moved their finger between two parts of the ship
    private bool HasMovedToNewPart(Touch touch, Transform part) {
        return touch.phase == TouchPhase.Moved && previousPart && previousPart != part;
    }

    private void SetPartColor(string name, Color color) {
        foreach (Renderer partRenderer in _ship.GetComponentsInChildren<Renderer>()) {
            if (string.Compare(partRenderer.gameObject.name, name, System.StringComparison.OrdinalIgnoreCase) == 0) {
                SetPartColor(partRenderer.gameObject.transform, color);
                return;
            }
        }
    }

    // Set the color of a specific ship part
    private void SetPartColor(Transform part, Color color) {
        if (part == null)
            return;

        part.GetComponent<Renderer>().material.color = color;
    }

    // Handle player starting and stopping touching the screen
    private Color HandlePhase(Touch touch) {
        if (touch.phase == TouchPhase.Began)
            return selectedColor;
        else if (touch.phase == TouchPhase.Ended)
            return defaultColor;

        return partColor;
    }

    public void AddFault(Fault fault) {
        if (!faultsToFix.ContainsKey(fault.faultLocation)) {
            faultsToFix.Add(fault.faultLocation, fault);
            updateFaultColors = true;
        }
            
        if (!fixesToFaults.ContainsKey(fault.fixLocation)) {
            fixesToFaults.Add(fault.fixLocation, fault);
            updateFaultColors = true;
        }
    }

    private void RemoveFault(string fault, string fix) {
        faultsToFix.Remove(fault);
        fixesToFaults.Remove(fix);

        _audioManager.Play("Heal");
    }

    private void UpdateSlider() {
        float margin = 0.2f;

        if (timeHeld < margin) {
            _timeSlider.gameObject.SetActive(false);
        } else if (!_timeSlider.IsActive()) {
            _timeSlider.gameObject.SetActive(true);
        } else {
            _timeSlider.value = (timeHeld - margin) / (timeToHold - margin);
        }
    }
}
