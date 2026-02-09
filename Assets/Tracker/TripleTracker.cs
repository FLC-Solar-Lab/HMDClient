using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using PeterO.Cbor;

public class TripleTracker : MonoBehaviour
{
    public GameObject trackedObject;

    // marker settings
    public string aruco_id = "2";
    public string vrpn_id = "origin";
        
    [SerializeField] private bool useCameraRay = true;
    [SerializeField] private Transform rayOriginOverride;
    [SerializeField] private Transform movementTargetOverride;
    [SerializeField] private Transform simulatedController;
    [SerializeField] private bool useScreenRay = false;
    [SerializeField] private bool useCameraForwardRay = true;
    [SerializeField] private bool showDebugRay = true;
    [SerializeField] private float debugRayLength = 5f;
    [SerializeField] private Color debugRayIdleColor = Color.cyan;
    [SerializeField] private Color debugRayActiveColor = Color.magenta;
    [SerializeField] private float debugRayWidth = 0.01f;
    [SerializeField] private LineRenderer debugRay;
    [SerializeField] private bool logInput = true;
    [SerializeField] private float logIntervalSeconds = 1f;
    [SerializeField] private bool enableMouseLook = true;
    [SerializeField] private bool aimWithMouse = true;
    [SerializeField] private float lookSensitivity = 2f;
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float sprintMultiplier = 3f;
    [SerializeField] private bool showHud = true;
    [SerializeField] private TextMeshProUGUI hudText;
    [SerializeField] private bool hideCursor = true;
    [SerializeField] private bool showCrosshair = true;
    [SerializeField] private float crosshairSize = 12f;
    [SerializeField] private float crosshairThickness = 2f;
    [SerializeField] private Color crosshairColor = Color.white;
    [SerializeField] private KeyCode toggleCursorKey = KeyCode.Escape;

    private NOODLESRoot _noodlesRoot;
    private float _nextMethodCheckTime;
    private bool _hasPositionUpdated;
    private bool _hasPositionSet;
    private bool _warnedMissingMethods;
    private float _nextInputLogTime;
    private float _yaw;
    private float _pitch;
    private Camera _cachedCam;
    private float _nextHudUpdateTime;
    private Canvas _hudCanvas;
    private Image _crosshairHorizontal;
    private Image _crosshairVertical;
    private bool _cursorHidden;

    private void OnEnable()
    {
    }

    private void Start()
    {
        showHud = true;
        showCrosshair = true;
        if (trackedObject != null)
        {
            trackedObject.SetActive(true);
            var textNode = trackedObject.transform.GetChild(4).gameObject;
            textNode.SetActive(false);
        }

        _cachedCam = FindObjectOfType<Camera>();
        var target = GetMovementTarget();
        if (target != null)
        {
            var euler = target.rotation.eulerAngles;
            _yaw = euler.y;
            _pitch = euler.x;
        }
        EnsureDebugRay();
        EnsureHud();
        _cursorHidden = hideCursor;
    }

    // Handles the event for the Trigger.
    private void HandleOnTriggerPull()
    {
        if (trackedObject != null)
        {
            trackedObject.transform.GetChild(4).gameObject.SetActive(false);
        }
    }
    
    private void HandleOnTriggerRelease()
    {
        if (trackedObject != null)
        {
            trackedObject.transform.GetChild(4).gameObject.SetActive(false);
        }

    }

    private void Update()
    {
        if (Input.GetKeyDown(toggleCursorKey))
        {
            _cursorHidden = !_cursorHidden;
        }

        UpdateCursorState();

        if (enableMouseLook)
        {
            UpdateMouseLook();
            UpdateMovement();
        }

        UpdateHud();

        var pressHeld = Input.GetMouseButton(0) || Input.GetKey(KeyCode.Space);
        var pressDown = Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space);
        var pressUp = Input.GetMouseButtonUp(0) || Input.GetKeyUp(KeyCode.Space);

        if (pressDown)
        {
            HandleOnTriggerPull();
            if (logInput)
            {
                Debug.Log("TripleTracker: press down");
            }
        }

        if (pressUp)
        {
            HandleOnTriggerRelease();
            if (_hasPositionSet)
            {
                HandleOnBumperRelease();
            }
            if (logInput)
            {
                Debug.Log("TripleTracker: press up");
            }
        }

        UpdateDebugRay(pressHeld);

        if (pressHeld)
        {
            if (!EnsureMethodsReady())
            {
                return;
            }

            var ray = GetInputRay();
            var controllerPosition = ray.origin;
            var controllerRotation = Quaternion.LookRotation(ray.direction, Vector3.up);

            try
            {
                var root = GetNoodlesRoot();
                if (root != null)
                {
                    var args = new List<CBORObject>();
                    args.Add(CBORObject.FromObject(controllerPosition));
                    args.Add(CBORObject.FromObject(controllerRotation.x));
                    args.Add(CBORObject.FromObject(controllerRotation.y));
                    args.Add(CBORObject.FromObject(controllerRotation.z));
                    args.Add(CBORObject.FromObject(controllerRotation.w));

                    root.TryInvokeMethodByName("position_updated", args, NoReply);

                    if (logInput && Time.time >= _nextInputLogTime)
                    {
                        _nextInputLogTime = Time.time + Mathf.Max(0.1f, logIntervalSeconds);
                        Debug.Log($"TripleTracker: sent position_updated pos={controllerPosition} dir={ray.direction} mouse={Input.mousePosition}");
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }
    }
    private void HandleOnBumperRelease()
    {
        try 
        {
            var root = GetNoodlesRoot();
            if (root != null) {
                var args = new List<CBORObject>();
                root.TryInvokeMethodByName("position_set",  args, NoReply);
            }
        }
        catch (Exception e) 
        {
            Debug.LogException(e);
        }
    }

    private void OnRequestReply(CBORObject reply) {
        //Debug.Log("OriginTracker: RQ REPLY" + reply.ToString());

        var transform_array = reply["result"];


        var offset = new Vector3(
            transform_array[0].ToObject<float>(),
            transform_array[1].ToObject<float>(), 
            -transform_array[2].ToObject<float>()
        );

        var rotation = new Quaternion(
            transform_array[3].ToObject<float>(),
            transform_array[4].ToObject<float>(), 
            -transform_array[5].ToObject<float>(),
            -transform_array[6].ToObject<float>()
        );

        var n_root = GameObject.FindWithTag("NoodlesRootItem");
        var n_room_offset = GameObject.FindWithTag("OriginOffsetItem");
        var indicator = GameObject.FindWithTag("CoordinateIndicator");



        if (n_root == null || n_room_offset == null || indicator == null)
        {
            Debug.LogWarning("Unable to find root or root offset nodes, we cannot properly set the origin for this client!");
            return;
        }

        n_root.transform.localPosition = offset;
        n_room_offset.transform.localRotation = rotation;

        // set the indicator as the inverse of this transform
        var tf_a = n_root.transform;
        var tf_b = transform;

        var indicator_pos = tf_a.InverseTransformPoint(tf_b.position);
        var indicator_rot = Quaternion.Inverse(tf_a.rotation) * tf_b.rotation;

        indicator.transform.SetLocalPositionAndRotation(indicator_pos, indicator_rot);

    }

    private void NoReply(CBORObject reply) {
    }

    private Transform GetControllerTransform()
    {
        return GetRayOrigin();
    }

    private void UpdateMouseLook()
    {
        var target = GetMovementTarget();
        if (target == null)
        {
            return;
        }

        var aimActive = aimWithMouse || Input.GetMouseButton(1);

        if (!hideCursor)
        {
            if (Input.GetMouseButtonDown(1))
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }

            if (Input.GetMouseButtonUp(1))
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }

        if (aimActive)
        {
            _yaw += Input.GetAxis("Mouse X") * lookSensitivity;
            _pitch -= Input.GetAxis("Mouse Y") * lookSensitivity;
            _pitch = Mathf.Clamp(_pitch, -80f, 80f);

            target.rotation = Quaternion.Euler(_pitch, _yaw, 0f);
        }
    }

    private void UpdateMovement()
    {
        var target = GetMovementTarget();
        if (target == null)
        {
            return;
        }

        var forward = target.forward;
        var right = target.right;
        forward.y = 0f;
        right.y = 0f;
        forward.Normalize();
        right.Normalize();

        var move = Vector3.zero;
        if (Input.GetKey(KeyCode.W)) move += forward;
        if (Input.GetKey(KeyCode.S)) move -= forward;
        if (Input.GetKey(KeyCode.D)) move += right;
        if (Input.GetKey(KeyCode.A)) move -= right;

        if (move.sqrMagnitude > 0f)
        {
            var speed = moveSpeed * (Input.GetKey(KeyCode.LeftShift) ? sprintMultiplier : 1f);
            target.position += move * (speed * Time.deltaTime);
        }
    }

    private void EnsureHud()
    {
        if (!showHud || hudText != null)
        {
            return;
        }

        var canvasObj = new GameObject("TripleTrackerHUD");
        _hudCanvas = canvasObj.AddComponent<Canvas>();
        _hudCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _hudCanvas.sortingOrder = 999;

        var scaler = canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
        scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        var textObj = new GameObject("HUDText");
        textObj.transform.SetParent(canvasObj.transform, false);
        hudText = textObj.AddComponent<TextMeshProUGUI>();
        hudText.fontSize = 20;
        hudText.color = Color.white;
        hudText.alignment = TextAlignmentOptions.TopLeft;

        var rect = hudText.rectTransform;
        rect.anchorMin = new Vector2(0, 1);
        rect.anchorMax = new Vector2(0, 1);
        rect.pivot = new Vector2(0, 1);
        rect.anchoredPosition = new Vector2(10, -10);
        rect.sizeDelta = new Vector2(900, 400);

        EnsureCrosshair();
    }

    private void UpdateHud()
    {
        if (!showHud)
        {
            if (hudText != null)
            {
                hudText.enabled = false;
            }
            if (_crosshairHorizontal != null)
            {
                _crosshairHorizontal.enabled = false;
            }
            if (_crosshairVertical != null)
            {
                _crosshairVertical.enabled = false;
            }
            return;
        }

        EnsureHud();
        if (hudText == null)
        {
            return;
        }

        if (_crosshairHorizontal != null)
        {
            _crosshairHorizontal.enabled = showCrosshair;
        }
        if (_crosshairVertical != null)
        {
            _crosshairVertical.enabled = showCrosshair;
        }

        if (Time.time < _nextHudUpdateTime)
        {
            return;
        }

        _nextHudUpdateTime = Time.time + 0.1f;

        var cam = Camera.main != null ? Camera.main : _cachedCam;
        var camName = cam != null ? cam.name : "(none)";
        var ray = GetInputRay();
        var mouse = Input.mousePosition;

        hudText.enabled = true;
        hudText.text =
            $"Cam: {camName}\n" +
            $"Mouse: {mouse.x:0},{mouse.y:0}\n" +
            $"Ray origin: {ray.origin}\n" +
            $"Ray dir: {ray.direction}\n" +
            $"Methods ready: {_hasPositionUpdated && _hasPositionSet}";
    }

    private void EnsureCrosshair()
    {
        if (!showCrosshair || _hudCanvas == null || _crosshairHorizontal != null)
        {
            return;
        }

        _crosshairHorizontal = CreateCrosshairImage("CrosshairH", new Vector2(crosshairSize * 2f, crosshairThickness));
        _crosshairVertical = CreateCrosshairImage("CrosshairV", new Vector2(crosshairThickness, crosshairSize * 2f));
    }

    private Image CreateCrosshairImage(string name, Vector2 size)
    {
        var obj = new GameObject(name);
        obj.transform.SetParent(_hudCanvas.transform, false);
        var image = obj.AddComponent<Image>();
        image.color = crosshairColor;
        var rect = image.rectTransform;
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = size;
        return image;
    }

    private void UpdateCursorState()
    {
        if (!_cursorHidden)
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
            return;
        }

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    private Transform GetRayOrigin()
    {
        if (rayOriginOverride != null)
        {
            return rayOriginOverride;
        }

        if (useCameraRay)
        {
            var cam = Camera.main != null ? Camera.main : _cachedCam;
            if (cam != null)
            {
                return cam.transform;
            }
        }

        if (simulatedController != null)
        {
            return simulatedController;
        }

        return transform;
    }

    private Transform GetMovementTarget()
    {
        if (movementTargetOverride != null)
        {
            return movementTargetOverride;
        }

        if (useCameraRay)
        {
            var cam = Camera.main != null ? Camera.main : _cachedCam;
            if (cam != null)
            {
                return cam.transform;
            }
        }

        if (simulatedController != null)
        {
            return simulatedController;
        }

        return transform;
    }

    private Ray GetInputRay()
    {
        var cam = Camera.main != null ? Camera.main : _cachedCam;
        if (cam != null)
        {
            if (useCameraForwardRay)
            {
                return new Ray(cam.transform.position, cam.transform.forward);
            }

            if (useScreenRay)
            {
                return cam.ScreenPointToRay(Input.mousePosition);
            }
        }

        var origin = GetRayOrigin();
        return new Ray(origin.position, origin.rotation * Vector3.forward);
    }

    private NOODLESRoot GetNoodlesRoot()
    {
        if (_noodlesRoot != null)
        {
            return _noodlesRoot;
        }

        var n_root = GameObject.FindWithTag("NoodlesRootItem");
        if (n_root == null)
        {
            return null;
        }

        _noodlesRoot = n_root.GetComponent<NOODLESRoot>();
        return _noodlesRoot;
    }

    private bool EnsureMethodsReady()
    {
        if (_hasPositionUpdated && _hasPositionSet)
        {
            return true;
        }

        if (Time.time < _nextMethodCheckTime)
        {
            return false;
        }

        _nextMethodCheckTime = Time.time + 1f;

        var root = GetNoodlesRoot();
        if (root == null)
        {
            return false;
        }

        _hasPositionUpdated = root.HasMethod("position_updated");
        _hasPositionSet = root.HasMethod("position_set");

        if (!_hasPositionUpdated || !_hasPositionSet)
        {
            if (!_warnedMissingMethods && root.HasAnyMethods())
            {
                Debug.LogWarning("NOODLES methods missing: position_updated/position_set. Check Godot method registration.");
                _warnedMissingMethods = true;
            }
            if (logInput && Time.time >= _nextInputLogTime)
            {
                _nextInputLogTime = Time.time + Mathf.Max(0.1f, logIntervalSeconds);
                Debug.Log("TripleTracker: NOODLES methods not ready yet.");
            }
            return false;
        }

        return true;
    }

    private void EnsureDebugRay()
    {
        if (debugRay != null || !showDebugRay)
        {
            return;
        }

        debugRay = GetComponent<LineRenderer>();
        if (debugRay == null)
        {
            debugRay = gameObject.AddComponent<LineRenderer>();
        }

        debugRay.useWorldSpace = true;
        debugRay.positionCount = 2;
        debugRay.widthMultiplier = debugRayWidth;
        debugRay.startWidth = debugRayWidth;
        debugRay.endWidth = debugRayWidth;
        debugRay.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        debugRay.receiveShadows = false;
        debugRay.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
        debugRay.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;

        if (debugRay.material == null)
        {
            var shader = Shader.Find("Unlit/Color");
            if (shader == null) shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            if (shader != null)
            {
                var mat = new Material(shader);
                if (mat.HasProperty("_Color"))
                {
                    mat.color = debugRayIdleColor;
                }
                debugRay.material = mat;
            }
        }
    }

    private void UpdateDebugRay(bool active)
    {
        if (!showDebugRay)
        {
            if (debugRay != null)
            {
                debugRay.enabled = false;
            }
            return;
        }

        EnsureDebugRay();

        if (debugRay == null)
        {
            return;
        }

        var ray = GetInputRay();
        var origin = ray.origin;
        var dir = ray.direction;

        debugRay.enabled = true;
        var color = active ? debugRayActiveColor : debugRayIdleColor;
        debugRay.startColor = color;
        debugRay.endColor = color;
        if (debugRay.material != null && debugRay.material.HasProperty("_Color"))
        {
            debugRay.material.color = color;
        }
        debugRay.SetPosition(0, origin);
        debugRay.SetPosition(1, origin + dir * debugRayLength);
    }
}
