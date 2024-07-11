using BepInEx;
using BepInEx.Configuration;
using KKAPI;
using KKAPI.Chara;
using KKAPI.Studio;
using KKAPI.Utilities;
using Studio;
using System.Collections.Generic;
using System.Reflection.Emit;
using KKRagdollPlugin;
using UnityEngine;
using UniRx.Triggers;
using System.Collections;
using static MetaCollider;
using NodeCanvas.Tasks.Actions;
using static RootMotion.FinalIK.RagdollUtility;
using System.Runtime.InteropServices;
using Timeline;
using System.Xml;
using BepInEx.Logging;
using BepInEx.Bootstrap;

namespace KKRagdollPlugin;

#if KK
[BepInPlugin("jusmahbasscannon.KKRagdollRevamp", "KKRagdollRevamp", "0.5.2")]
#elif KKS
[BepInPlugin("jusmahbasscannon.KKSRagdollRevamp", "KKSRagdollRevamp", "0.5.2")]
#endif
[BepInDependency("marco.kkapi", "1.32")]
public class KKRagdollRevampBase : BaseUnityPlugin
{
    private bool ragdollUiActive = false;

    private Rect windowRect = new Rect(500f, 40f, 240f, 170f);

    private ChaControl currentChaControl = null;

    public bool ragdollsOn = false;

    private GameCursor.POINT RotationLockPOS = default(GameCursor.POINT);

    internal static ManualLogSource Logger;

    public static ConfigEntry<KeyboardShortcut> ActivateRagdoll { get; private set; }

    public static ConfigEntry<KeyboardShortcut> ActivateRagdollAll { get; private set; }

    public static ConfigEntry<KeyboardShortcut> ExplodeShortcut { get; private set; }

    public static ConfigEntry<KeyboardShortcut> RotateinPlaceShortcut { get; private set; }

    public static ConfigEntry<bool> AutoIKToggle { get; private set; }

    public static ConfigEntry<bool> ToggleKKABMX { get; private set; }

    public static ConfigEntry<bool> ClickDragToggle { get; private set; }

    public static ConfigEntry<float> k_Spring { get; private set; }

    public static ConfigEntry<float> k_Damper { get; private set; }

    public static ConfigEntry<float> k_Drag { get; private set; }

    public static ConfigEntry<float> k_AngularDrag { get; private set; }

    public static ConfigEntry<float> k_Distance { get; private set; }

    public static ConfigEntry<float> ThrowDistance { get; private set; }

    public static ConfigEntry<float> CDRotationSpeed { get; private set; }

    public static ConfigEntry<bool> AutoLockCamera { get; private set; }

    public static ConfigEntry<bool> ExplodeToggle { get; private set; }

    public static ConfigEntry<float> ExplodeRadius { get; private set; }

    public static ConfigEntry<float> ExplodePower { get; private set; }

    public static ConfigEntry<float> ExplodeUpwardsForce { get; private set; }

    public static ConfigEntry<bool> RotateinPlaceWIP { get; private set; }

    public static ConfigEntry<bool> StilettoFix { get; private set; }

    public static ConfigEntry<bool> CollidersFix { get; private set; }

    //DragRigidBody Values
    /* const float k_Spring = 50.0f;
    const float k_Damper = 5.0f;
    const float k_Drag = 10.0f;
    const float k_AngularDrag = 5.0f;
    const float k_Distance = 0.2f; */
    const bool k_AttachToCenterOfMass = false;

    private SpringJoint m_SpringJoint;

    private void Awake()
    {
        if (StudioAPI.InsideStudio)
        {
            CharacterApi.RegisterExtraBehaviour<KKRagdollController>("KKRagdollPlugin");
        }

        Physics.IgnoreLayerCollision(0, 28);

        ActivateRagdoll = base.Config.Bind("Keyboard Shortcuts", "Activate Ragdoll", new KeyboardShortcut(KeyCode.F8), new ConfigDescription("Activates ragdoll for currently selected characters.", null));

        ActivateRagdollAll = base.Config.Bind("Keyboard Shortcuts", "Activate All Ragdolls", new KeyboardShortcut(KeyCode.F10), new ConfigDescription("Activates ragdolls for all characters in the scene.", null));

        AutoIKToggle = base.Config.Bind("Experimental", "Auto Toggle IK", defaultValue: false, new ConfigDescription("Automatically release IK's grip on the model when activating the ragdoll. WARNING: Could cause unexpected glitches!", null, new ConfigurationManagerAttributes { IsAdvanced = true }));

        ToggleKKABMX = base.Config.Bind("Debug", "Auto Disable KKABMX", defaultValue: true, new ConfigDescription("Automatically disables KKABMX when in ragdoll mode to fix gliding ragdolls. WARNING: Will cause physics issues with ragdolls, but might fix character distortion!", null, new ConfigurationManagerAttributes { IsAdvanced = true }));

        StilettoFix = base.Config.Bind("Debug", "Auto Disable Stiletto", defaultValue: true, new ConfigDescription("Automatically disables Stiletto when in ragdoll mode to fix rotating ankles. If you don't have Stilleto installed and are getting Stiletto-related errors in the console, disable this!", null, new ConfigurationManagerAttributes { IsAdvanced = true }));

        CollidersFix = base.Config.Bind("Debug", "Auto Disable Colliders", defaultValue: true, new ConfigDescription("Automatically disables breast, floor and skirt colliders when in ragdoll mode to prevent stretching parts of the ragdoll. This probably shouldn't be disabled unless you're debugging or testing!", null, new ConfigurationManagerAttributes { IsAdvanced = true }));

        ClickDragToggle = base.Config.Bind("Click and Drag", "Toggle Click and Drag", defaultValue: true, new ConfigDescription("Enable or disable click and drag functionality for the ragdolls.", null, new ConfigurationManagerAttributes { Order = 1 }));

        AutoLockCamera = base.Config.Bind("Click and Drag", "Auto Lock Camera", defaultValue: true, new ConfigDescription("Automatically lock the camera when dragging a ragdoll. !!This should always be on unless you have a plan!!", null, new ConfigurationManagerAttributes { Order = 2 }));

        k_Spring = base.Config.Bind("Click and Drag", "Spring", 2000f, new ConfigDescription("The strength of the drag. This should be pretty high for best results.", new AcceptableValueRange<float>(300f, 2000f), new ConfigurationManagerAttributes { Order = 3 }));

        k_Damper = base.Config.Bind("Click and Drag", "Damper", 0.01f, new ConfigDescription("How fast high-speed motion is lost. Low values increase the floppiness, high values increase precision.", new AcceptableValueRange<float>(0.01f, 200f), new ConfigurationManagerAttributes { Order = 4 }));

        k_Drag = base.Config.Bind("Click and Drag", "Linear Drag", 10f, new ConfigDescription("The max speed of the ragdoll in the air. Low values are recommended.", new AcceptableValueRange<float>(10f, 100f), new ConfigurationManagerAttributes { Order = 5 }));

        k_AngularDrag = base.Config.Bind("Click and Drag", "Angular Drag", 10f, new ConfigDescription("The max speed of rotation for joints of the ragdoll through the air. Low values are recommended.", new AcceptableValueRange<float>(10f, 100f), new ConfigurationManagerAttributes { Order = 6 }));

        k_Distance = base.Config.Bind("Click and Drag", "Distance", 0.01f, new ConfigDescription("How far away the cursor is from the ragdoll before it starts being dragged. For best results, put this slider to as low as it can go!", new AcceptableValueRange<float>(0.01f, 1f), new ConfigurationManagerAttributes { Order = 7 }));

        ThrowDistance = base.Config.Bind("Click and Drag", "Push/Pull Strength", 0.2f, new ConfigDescription("How far the ragdoll gets pushed/pulled between scroll wheel clicks.", new AcceptableValueRange<float>(0.1f, 3f), new ConfigurationManagerAttributes { Order = 8 }));

        CDRotationSpeed = base.Config.Bind("Click and Drag", "Mid-Air Rotation Speed", 50f, new ConfigDescription("How fast the ragdoll will rotate while holding down E during a click and drag of a ragdoll. !!WARNING!! THIS IS PART OF A WIP FEATURESET!!", new AcceptableValueRange<float>(0.1f, 3f), new ConfigurationManagerAttributes { Order = 9, IsAdvanced = true }));

        RotateinPlaceWIP = base.Config.Bind("Click and Drag", "Toggle Rotate in Place (UNFINISHED)", defaultValue: false, new ConfigDescription("Enable or disable holding down a key to rotate a part of the ragdoll in place. !!WARNING!! THIS DOESN'T QUITE WORK YET!!!!", null, new ConfigurationManagerAttributes { Order = 10, IsAdvanced = true }));

        RotateinPlaceShortcut = base.Config.Bind("Click and Drag", "Rotate in Place Hold Shortcut", new KeyboardShortcut(KeyCode.E), new ConfigDescription("The key that must be held down to start rotating in place. !!WARNING!! THIS IS PART OF A WIP FEATURESET!!", null, new ConfigurationManagerAttributes { Order = 11, IsAdvanced = true }));

        ExplodeToggle = base.Config.Bind("Explode", "Explode Toggle", defaultValue: true, new ConfigDescription("Enable or disable the explosion feature, originating at the cursor.", null, new ConfigurationManagerAttributes { Order = 1 }));

        ExplodeShortcut = base.Config.Bind("Explode", "Keyboard Shortcut", new KeyboardShortcut(KeyCode.F6), new ConfigDescription("Define which button causes the explosion.", null, new ConfigurationManagerAttributes { Order = 2 }));

        ExplodeRadius = base.Config.Bind("Explode", "Radius", 5f, new ConfigDescription("The size of the explosion hitbox.", new AcceptableValueRange<float>(0.1f, 1000f), new ConfigurationManagerAttributes { Order = 3 }));

        ExplodePower = base.Config.Bind("Explode", "Power", 5f, new ConfigDescription("The strength of the explosion.", new AcceptableValueRange<float>(0.1f, 1000f), new ConfigurationManagerAttributes { Order = 4 }));

        ExplodeUpwardsForce = base.Config.Bind("Explode", "Upwards Force", 5f, new ConfigDescription("How much the explosion pushes the ragdoll upwards.", new AcceptableValueRange<float>(0.1f, 1000f), new ConfigurationManagerAttributes { Order = 5 }));
    }

    Dictionary<string, BepInEx.PluginInfo> activePlugins = new Dictionary<string, BepInEx.PluginInfo>();

    public static bool isStiletto { get; private set; }

    public static bool isCollider { get; private set; }

    private void Start()
    {
        PopulateTimeline();

        activePlugins = Chainloader.PluginInfos;
        if (CheckModInstall("Stiletto")) { isStiletto = true; } else { isStiletto = false; }
        if (CheckModInstall("Colliders")) { isCollider = true; } else { isCollider = false; }
    }

    public bool CheckModInstall(System.String modName)
    {
        //UnityEngine.Debug.Log("Started check mod install");
        //if (activePlugins.Count == 0)
        //{
        //UnityEngine.Debug.Log("activeplugins was 0, so we gave it the chainloader list");
        //activePlugins = Chainloader.PluginInfos;
        //}
        //UnityEngine.Debug.Log("Starting foreach");
        foreach (var plugin in activePlugins)
        {
            var metadata = plugin.Value.Metadata;
            //UnityEngine.Debug.Log("Found " + metadata.Name + ", looking for " + modName + ".");
            if (metadata.Name == modName)
            {
                //UnityEngine.Debug.Log("Found it! " + metadata.Name + " / " + modName + " . Passing true.");
                return true;
            }
        }
        //UnityEngine.Debug.Log("Code could not find " + modName + ".");
        return false;
    }

    private void PopulateTimeline()
    {
        if (TimelineCompatibility.IsTimelineAvailable())
        {
            /* TimelineCompatibility.AddInterpolableModelDynamic<object,object>(
                ("Ragdoll"),
                ("KKRagdollRevamp"),
                ("Toggle Ragdoll"),
                delegate (ObjectCtrlInfo oci, object parameter, object leftValue, object rightValue, float factor)
                {
                    ((KKRagdollController)parameter).fireRagdoll = (bool)leftValue;
                },
                null,
                (ObjectCtrlInfo oci) => oci is OCIChar,
                (ObjectCtrlInfo oci, object parameter) => ((KKRagdollController)parameter).fireRagdoll,
                (object parameter, XmlNode node) => XmlConvert.ToBoolean(node.Attributes["value"].Value),
                delegate (object parameter, XmlTextWriter writer, object value)
                {
                    writer.WriteAttributeString("value", XmlConvert.ToString((bool)value));
                },
                (ObjectCtrlInfo oci) => GetController(((OCIChar)oci).GetChaControl()),
                (ObjectCtrlInfo oci, XmlNode node) => GetController(((OCIChar)oci).GetChaControl()),
                null,
                null,
                true,
                (string currentName, ObjectCtrlInfo oci, object parameter) => "Toggle Ragdoll"); */

            TimelineCompatibility.AddCharaFunctionInterpolable(
                ((string)"KKRagdollRevamp"),
                ((string)"rag"),
                ((string)"Toggle Ragdoll"),
                delegate (OCIChar oci, KKRagdollController parameter, object leftValue, object rightValue, float factor)
                {
                    parameter.fireRagdoll = (bool)leftValue;
                },
                interpolateAfter: null,
                (OCIChar oci, KKRagdollController parameter) => parameter.fireRagdoll,
                (KKRagdollController parameter, XmlNode node) => XmlConvert.ToBoolean(node.Attributes["value"].Value),
                delegate (KKRagdollController parameter, XmlTextWriter writer, object value)
                {
                    writer.WriteAttributeString("value", XmlConvert.ToString((bool)value));
                });
        }
    }


    private static KKRagdollController GetController(ChaControl character)
    {
        if (!(character == null))
        {
            return character.gameObject.GetComponent<KKRagdollController>();
        }
        return null;
    }

#if KK
    private void OnGUI()
    {
        if (ragdollUiActive && KoikatuAPI.GetCurrentGameMode() == GameMode.Studio)
        {
            windowRect = GUI.Window(345, windowRect, WindowFunction, "Koikatsu Ragdoll Revamp");
            IMGUIUtils.EatInputInRect(windowRect);
        }
    }
#endif

    private void Update()
    {
        /* if (Input.GetKeyDown(KeyCode.F9))
        {
            ragdollUiActive = !ragdollUiActive;
        } */

        InitializeDrag();

        if (ActivateRagdoll.Value.IsDown())
        {
            IEnumerable<OCIChar> selectedCharacters = StudioAPI.GetSelectedCharacters();
            foreach (OCIChar item in selectedCharacters)
            {
                currentChaControl = item.GetChaControl();
                //currentChaControl.gameObject.GetComponent<KKRagdollPlugin.KKRagdollController>().BroadcastMessage("ToggleRagdoll", SendMessageOptions.RequireReceiver);
                currentChaControl.gameObject.GetComponent<KKRagdollPlugin.KKRagdollController>().fireRagdoll = !currentChaControl.gameObject.GetComponent<KKRagdollPlugin.KKRagdollController>().fireRagdoll;
                //UnityEngine.Debug.Log("Attempted to send message!");
            }
        }

        if ((ExplodeShortcut.Value.IsDown()) && (ExplodeToggle.Value))
        {
            Explosion();
        }
    }

#if KK
    private void WindowFunction(int WindowID)
    {
        if (GUI.Button(new Rect(10, 70, 150, 30), "Toggle Ragdoll"))
        {
            UnityEngine.Debug.Log("test");
        }
    }
#endif

    private void Explosion()
    {
        RaycastHit hit;
        var mainCamera = FindCamera();
        Physics.Raycast(mainCamera.ScreenPointToRay(Input.mousePosition).origin, mainCamera.ScreenPointToRay(Input.mousePosition).direction, out hit, Mathf.Infinity, Physics.AllLayers);
        Collider[] array = Physics.OverlapSphere(hit.point, ExplodeRadius.Value);
        Collider[] array2 = array;
        foreach (Collider collider in array2)
        {
            //collider.GetComponentInParent<KKRagdollController>().fireRagdoll = true;
            collider.GetComponent<Rigidbody>()?.AddExplosionForce(ExplodePower.Value, hit.point, ExplodeRadius.Value, ExplodeUpwardsForce.Value);
        }
    }


    //DragRigidBody code
    private void InitializeDrag()
    {
        if (!ClickDragToggle.Value) { return; }



        // Make sure the user pressed the mouse down
        if (!Input.GetMouseButtonDown(0))
        {
            //UnityEngine.Debug.Log("DragRigidBody code reported that the current input is not 'MouseButton0'");
            return;
        }

        var mainCamera = FindCamera();

        // We need to actually hit an object
        //RaycastHit hit = new RaycastHit();
        RaycastHit[] hits;
        hits = Physics.RaycastAll(mainCamera.ScreenPointToRay(Input.mousePosition).origin,
                                mainCamera.ScreenPointToRay(Input.mousePosition).direction, Mathf.Infinity,
                                Physics.AllLayers);
        System.Array.Sort(hits, (x, y) => x.distance.CompareTo(y.distance));

        for (int i = 0; i < hits.Length; i++)
        {
            RaycastHit hit = hits[i];
            if (
                !Physics.Raycast(mainCamera.ScreenPointToRay(Input.mousePosition).origin,
                                    mainCamera.ScreenPointToRay(Input.mousePosition).direction, Mathf.Infinity,
                                    Physics.AllLayers))
            {
                continue;
            }

            if (!hit.rigidbody || hit.rigidbody.isKinematic)
            {
                continue;
            }

            if (!hit.rigidbody.GetComponentInParent<Studio.OptionItemCtrl>().outsideVisible)
            {
                continue;
            }

            if (!m_SpringJoint)
            {
                var go = new GameObject("Rigidbody dragger");
                Rigidbody body = go.AddComponent<Rigidbody>();
                m_SpringJoint = go.AddComponent<SpringJoint>();
                body.isKinematic = true;
            }

            m_SpringJoint.transform.position = hit.point;
            m_SpringJoint.anchor = Vector3.zero;

            m_SpringJoint.spring = k_Spring.Value;
            m_SpringJoint.damper = k_Damper.Value;
            m_SpringJoint.maxDistance = k_Distance.Value;
            m_SpringJoint.connectedBody = hit.rigidbody;

            StartCoroutine("DragObject", hit.distance);
            break;
        }
    }

    [DllImport("USER32.dll", CallingConvention = CallingConvention.StdCall)]
    private static extern void SetCursorPos(int X, int Y);

    [DllImport("USER32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetCursorPos(out GameCursor.POINT lpPoint);


    private IEnumerator DragObject(float distance)
    {
        var oldDrag = m_SpringJoint.connectedBody.drag;
        var oldAngularDrag = m_SpringJoint.connectedBody.angularDrag;
        m_SpringJoint.connectedBody.drag = k_Drag.Value;
        m_SpringJoint.connectedBody.angularDrag = k_AngularDrag.Value;
        var mainCamera = FindCamera();
        if (AutoLockCamera.Value) { mainCamera.GetComponent<Studio.CameraControl>().enabled = false; }
        var distanceModifier = new Vector3(0, 0, 0);
        var rotationSpeed = CDRotationSpeed.Value;
        var isRotating = false;
        var lockPosition = new Vector3();
        //var scrollOld = new Vector3();
        //var scrollNew = new Vector3();
        //var scrollTime = 1f;
        //var lockedRay = new Ray();
        //var torque = 50f;
        while (Input.GetMouseButton(0))
        {
            var ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            if ((RotateinPlaceShortcut.Value.IsPressed()) || (RotateinPlaceWIP.Value))
            {
                var connectedRB = m_SpringJoint.connectedBody;
                if (!isRotating)
                {
                    GetCursorPos(out RotationLockPOS);
                    //m_SpringJoint.connectedBody.constraints = RigidbodyConstraints.FreezePosition;
                    Cursor.visible = false;
                    lockPosition = m_SpringJoint.transform.position;
                }
                m_SpringJoint.transform.position = lockPosition;
                //m_SpringJoint.connectedBody.rotation = Quaternion.Euler(m_SpringJoint.connectedBody.rotation.eulerAngles + new Vector3(rotationSpeed * Input.GetAxis("Mouse X"), rotationSpeed * Input.GetAxis("Mouse Y"), 0f));

                //var EularAngleVelocity = new Vector3(10, 10, 0);
                //Quaternion deltaRotation = Quaternion.Euler(EularAngleVelocity * Time.fixedDeltaTime);
                //m_SpringJoint.connectedBody.MoveRotation(m_SpringJoint.connectedBody.rotation * deltaRotation);

                connectedRB.angularVelocity += new Vector3(Input.GetAxis("Mouse Y"), Input.GetAxis("Mouse X"), 0f) * rotationSpeed;

                isRotating = true;
            }
            else
            {
                if (isRotating)
                {
                    SetCursorPos(RotationLockPOS.X, RotationLockPOS.Y);
                    Cursor.visible = true;
                    //new WaitForSecondsRealtime(0.2f);
                    //m_SpringJoint.connectedBody.constraints = RigidbodyConstraints.None;
                }
                //scrollOld = m_SpringJoint.transform.position;
                //scrollNew = (ray.GetPoint(distance) + distanceModifier);
                //m_SpringJoint.transform.position = Vector3.Lerp(scrollOld, scrollNew, scrollTime);
                m_SpringJoint.transform.position = (ray.GetPoint(distance) + distanceModifier);
                if (Input.GetAxis("Mouse ScrollWheel") > 0f)
                {
                    distanceModifier = distanceModifier + mainCamera.transform.forward * ThrowDistance.Value;
                }
                else if (Input.GetAxis("Mouse ScrollWheel") < 0f)
                {
                    distanceModifier = distanceModifier - mainCamera.transform.forward * ThrowDistance.Value;
                }
                isRotating = false;
            }
            if (Input.GetMouseButton(1)) { break; }
            yield return null;
        }
        if (AutoLockCamera.Value) { mainCamera.GetComponent<Studio.CameraControl>().enabled = true; }
        if (isRotating)
        {
            SetCursorPos(RotationLockPOS.X, RotationLockPOS.Y);
            Cursor.visible = true;
            m_SpringJoint.connectedBody.constraints = RigidbodyConstraints.None;
        }
        if (m_SpringJoint.connectedBody)
        {
            m_SpringJoint.connectedBody.drag = oldDrag;
            m_SpringJoint.connectedBody.angularDrag = oldAngularDrag;
            m_SpringJoint.connectedBody = null;
        }
    }


    private Camera FindCamera()
    {
        if (GetComponent<Camera>())
        {
            return GetComponent<Camera>();
        }

        return Camera.main;
    }
}