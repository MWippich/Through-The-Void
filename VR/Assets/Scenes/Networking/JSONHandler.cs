using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class JSONHandler : MonoBehaviour
{

    public TextAsset jsonFile;
    public Faults faultsInJson;

    private void Awake()
    {
        GetData();
    }
   

    private void GetData()
    {
        faultsInJson = JsonUtility.FromJson<Faults>(jsonFile.text); // Get faults from JSON

        // Debug
        foreach (Fault fault in faultsInJson.faults) {
            Debug.Log(fault.id + " " + fault.faultLocation + " " + fault.fixLocation);
        }
    }

    public Fault GetFault(string id) {
        foreach (Fault fault in faultsInJson.faults) {
            if (fault.id == id)
                return fault;
        }

        return null;
    }

    public int CountFaults()
    {
        return faultsInJson.faults.Length;
    }
}

[System.Serializable]
public class Fault {
    public string id;
    //public float maxSpeed = 1.0f;
    //public float maxTurnSpeed = 0.5f;
    public string faultLocation;
    public string fixLocation;
    public float maxSpeedModifier;
    public float accelerationModifier;
    public float maxTurnSpeedModifier;
    public float turnAccelerationModifier;

}

[System.Serializable]
public class Faults {
    public Fault[] faults;
}