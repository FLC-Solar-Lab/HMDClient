using System.Collections;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text;
using UnityEngine;
using TMPro;



public class Instructions : MonoBehaviour
{

    private NOODLESRoot _noodlesRoot;
    private TextMeshProUGUI _serverStatus;

    private System.Net.WebSockets.WebSocketState _connectionState = WebSocketState.None;
    private System.Net.WebSockets.WebSocketState _lastConnectionState = WebSocketState.None;

    public GameObject canvas;

    // Start is called before the first frame update
    void Start()
    {
       // Using legacy input to avoid dependency on the Input System package.

       _noodlesRoot = GameObject.Find("NoodlesRoot").gameObject.GetComponent<NOODLESRoot>();
       _serverStatus = GameObject.Find("ServerStatus").gameObject.GetComponent<TextMeshProUGUI>();
       updateServerInfo();
    }


    void Update() {
        if (canvas.activeSelf) {
            updateServerInfo();
        }

        if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.B))
        {
            HandleButtonPress();
        }

        if (Input.GetKeyDown(KeyCode.M) || Input.GetKeyDown(KeyCode.Escape))
        {
            HandleMenu();
        }
    }

    void updateServerInfo()
    {
        if (_noodlesRoot) {
            _connectionState =_noodlesRoot.serverConnection();

            if (_connectionState == _lastConnectionState)
            {
                return;
            }

            _lastConnectionState = _connectionState;

            switch (_connectionState)
            {
                case WebSocketState.None:
                    _serverStatus.text = "Initializing ...";
                    _serverStatus.color = new Color(1.0f, 1.0f, 0.0f, 1.0f);
                    break;
                case WebSocketState.Connecting:
                    _serverStatus.text = "Connecting to server ...";
                    _serverStatus.color = new Color(1.0f, 1.0f, 0.0f, 1.0f);
                    break;
                case WebSocketState.Open:
                    _serverStatus.text = "Connected to server";
                    _serverStatus.color = new Color(0.0f, 1.0f, 0.0f, 1.0f);
                    break;
                default:
                    _serverStatus.text = "No server connection!\n\nStart the server and restart this app.";
                    _serverStatus.color = new Color(1.0f, 0.0f, 0.0f, 1.0f);
                    break;    
            }
        }
    }


    private void HandleButtonPress()
    {
        canvas.SetActive(false);
    }
    private void HandleMenu()
    {
        updateServerInfo();
        canvas.SetActive(!canvas.activeSelf);
    }
}
