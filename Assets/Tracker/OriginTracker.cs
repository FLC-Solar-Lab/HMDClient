using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.XR.MagicLeap;

public class OriginTracker : MonoBehaviour
{
    // marker settings
    public float qrCodeMarkerSize; // size of the expected qr code, in meters
    private Dictionary<string, GameObject> _markers = new Dictionary<string, GameObject>();
    private ASCIIEncoding _asciiEncoder = new System.Text.ASCIIEncoding();

    // the object that will be instantiated on the marker
    public GameObject trackerObject;

    private void Start()
    {
        Debug.Log("OriginTracker::Start()");
        // create a tracker settings object with variables defined above
        MLMarkerTracker.ArucoDictionaryName d = MLMarkerTracker.ArucoDictionaryName.DICT_5X5_100;
        MLMarkerTracker.Profile profile = MLMarkerTracker.Profile.Accuracy;
        // create a tracker settings object with variables defined above
        MLMarkerTracker.TrackerSettings trackerSettings = MLMarkerTracker.TrackerSettings.Create(true, MLMarkerTracker.MarkerType.Aruco_April, qrCodeMarkerSize, d, 0.0762f, profile);


        trackerObject.SetActive(false);

        // start marker tracking with tracker settings object
        _ = MLMarkerTracker.SetSettingsAsync(trackerSettings);
    }

    // subscribe to the event that detects markers
    private void OnEnable()
    {
        Debug.Log("OriginTracker::OnEnable()");
        MLMarkerTracker.OnMLMarkerTrackerResultsFound += OnTrackerResultsFound;
    }

    // when the marker is detected...
    private void OnTrackerResultsFound(MLMarkerTracker.MarkerData data)
    {
        string id = "";

        if (data.Type == MLMarkerTracker.MarkerType.QR)
        {
            id = _asciiEncoder.GetString(data.BinaryData.Data, 0, data.BinaryData.Data.Length);
        } 
        else if (data.Type == MLMarkerTracker.MarkerType.Aruco_April)
        {
            id = data.ArucoData.Id.ToString();
        }

        Debug.Log("OriginTracker::OnTrackerResultsFound" + data.Pose);
        
        if (true)//!string.IsNullOrEmpty(id))
        {
            if (id == "2")
            {
                this.transform.position = data.Pose.position;
                this.transform.rotation = data.Pose.rotation * Quaternion.Euler(-90,0,180);
            
            if (_markers.ContainsKey(id))
            {
                Debug.Log("marker " + id + " exists");

                GameObject marker = _markers[id];

                // Reposition the marker
                marker.transform.position = data.Pose.position + this.transform.rotation * new Vector3(-1.0f,0.0f,0f);
                marker.transform.rotation = data.Pose.rotation * Quaternion.Euler(-90,0,180);
            }
            else
            {
                Debug.Log("Creating new marker " + id);
               //Create an origin marker
                GameObject marker = Instantiate(trackerObject, data.Pose.position, data.Pose.rotation);
                marker.SetActive(true);
                marker.transform.GetChild(4).gameObject.GetComponent<TextMeshPro>().text = id;
                marker.transform.up = Vector3.up;

                // Position the marker
                //marker.transform.position = data.Pose.position;
                marker.transform.rotation = data.Pose.rotation * Quaternion.Euler(-90,0,180);

                GameObject offset = Instantiate(trackerObject, data.Pose.position + this.transform.rotation * new Vector3(-1.0f,0.0f,0f), this.transform.rotation);
                offset.transform.GetChild(4).gameObject.GetComponent<TextMeshPro>().text = "offset";
                offset.SetActive(true);

                _markers.Add(id, offset);
            }
            }
        }

        // stop scanning after object has been instantiated
        //_ = MLMarkerTracker.StopScanningAsync();

    }
}
