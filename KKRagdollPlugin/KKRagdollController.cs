using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using BepInEx.Logging;
using KKAPI;
using KKAPI.Chara;
using KKAPI.Utilities;
using UnityEngine;
using UnityEngine.UI;
using KKABMX;
using static KKRagdollPlugin.KKRagdollRevampBase;
#if KK
using static UnityEngine.GUI;
#endif
using Studio;
using RootMotion;
using static RootMotion.FinalIK.IKSolver;
using Stiletto;
using KKAPI.Studio;
using ADV.Commands.Base;
using BepInEx.Bootstrap;

namespace KKRagdollPlugin;

public class KKRagdollController : CharaCustomFunctionController
{
	private class BoneInfo
	{
		public string name;

		public Transform anchor;

		public CharacterJoint joint;

		public BoneInfo parent;

		public float minLimit;

		public float maxLimit;

		public float swing1Limit;

		public float swing2Limit;

		public Vector3 axis;

		public Vector3 normalAxis;

		public float radiusScale;

		public Type colliderType;

		public List<BoneInfo> children = new List<BoneInfo>();

		public float density;

		public float summedMass;

		/*public bool _isLocked;

		public bool isLocked
		{
			get
			{
				return _isLocked;
			}
			set
			{
				_isLocked = value;
                if (value)
				{
					this.anchor.GetComponent<Rigidbody>().constraints = RigidbodyConstraints.FreezeAll;
                }
				if (!value)
				{
                    this.anchor.GetComponent<Rigidbody>().constraints = RigidbodyConstraints.None;
                }
            }
		}*/
	}

	private string pelvisName = "BodyTop/p_cf_body_bone/cf_j_root/cf_n_height/cf_j_hips";

	private string leftHipsName = "BodyTop/p_cf_body_bone/cf_j_root/cf_n_height/cf_j_hips/cf_j_waist01/cf_j_waist02/cf_j_thigh00_L";

	private string leftKneeName = "BodyTop/p_cf_body_bone/cf_j_root/cf_n_height/cf_j_hips/cf_j_waist01/cf_j_waist02/cf_j_thigh00_L/cf_j_leg01_L";

	private string leftFootName = "BodyTop/p_cf_body_bone/cf_j_root/cf_n_height/cf_j_hips/cf_j_waist01/cf_j_waist02/cf_j_thigh00_L/cf_j_leg01_L/cf_j_leg03_L";

	private string rightHipsName = "BodyTop/p_cf_body_bone/cf_j_root/cf_n_height/cf_j_hips/cf_j_waist01/cf_j_waist02/cf_j_thigh00_R";

	private string rightKneeName = "BodyTop/p_cf_body_bone/cf_j_root/cf_n_height/cf_j_hips/cf_j_waist01/cf_j_waist02/cf_j_thigh00_R/cf_j_leg01_R";

	private string rightFootName = "BodyTop/p_cf_body_bone/cf_j_root/cf_n_height/cf_j_hips/cf_j_waist01/cf_j_waist02/cf_j_thigh00_R/cf_j_leg01_R/cf_j_leg03_R";

	private string leftArmName = "BodyTop/p_cf_body_bone/cf_j_root/cf_n_height/cf_j_hips/cf_j_spine01/cf_j_spine02/cf_j_spine03/cf_d_shoulder_L/cf_j_shoulder_L/cf_j_arm00_L";

	private string leftElbowName = "BodyTop/p_cf_body_bone/cf_j_root/cf_n_height/cf_j_hips/cf_j_spine01/cf_j_spine02/cf_j_spine03/cf_d_shoulder_L/cf_j_shoulder_L/cf_j_arm00_L/cf_j_forearm01_L";

	private string leftHandName = "BodyTop/p_cf_body_bone/cf_j_root/cf_n_height/cf_j_hips/cf_j_spine01/cf_j_spine02/cf_j_spine03/cf_d_shoulder_L/cf_j_shoulder_L/cf_j_arm00_L/cf_j_forearm01_L/cf_j_hand_L";

	private string rightArmName = "BodyTop/p_cf_body_bone/cf_j_root/cf_n_height/cf_j_hips/cf_j_spine01/cf_j_spine02/cf_j_spine03/cf_d_shoulder_R/cf_j_shoulder_R/cf_j_arm00_R";

	private string rightElbowName = "BodyTop/p_cf_body_bone/cf_j_root/cf_n_height/cf_j_hips/cf_j_spine01/cf_j_spine02/cf_j_spine03/cf_d_shoulder_R/cf_j_shoulder_R/cf_j_arm00_R/cf_j_forearm01_R";

	private string rightHandName = "BodyTop/p_cf_body_bone/cf_j_root/cf_n_height/cf_j_hips/cf_j_spine01/cf_j_spine02/cf_j_spine03/cf_d_shoulder_R/cf_j_shoulder_R/cf_j_arm00_R/cf_j_forearm01_R/cf_j_hand_R";

	private string lowerSpineName = "BodyTop/p_cf_body_bone/cf_j_root/cf_n_height/cf_j_hips/cf_j_spine01";

	private string middleSpineName = "BodyTop/p_cf_body_bone/cf_j_root/cf_n_height/cf_j_hips/cf_j_spine01/cf_j_spine02";

	private string upperWaistName = "BodyTop/p_cf_body_bone/cf_j_root/cf_n_height/cf_j_hips/cf_j_waist01"; // not used

	private string lowerWaistName = "BodyTop/p_cf_body_bone/cf_j_root/cf_n_height/cf_j_hips/cf_j_waist01/cf_j_waist02";

	private string headName = "BodyTop/p_cf_body_bone/cf_j_root/cf_n_height/cf_j_hips/cf_j_spine01/cf_j_spine02/cf_j_spine03/cf_j_neck/cf_j_head";

	public Transform pelvis = null;

	public Transform leftHips = null;

	public Transform leftKnee = null;

	public Transform leftFoot = null;

	public Transform rightHips = null;

	public Transform rightKnee = null;

	public Transform rightFoot = null;

	public Transform leftArm = null;

	public Transform leftElbow = null;

	public Transform leftHand = null;

	public Transform rightArm = null;

	public Transform rightElbow = null;

	public Transform rightHand = null;

	public Transform middleSpine = null;

	public Transform lowerSpine = null;

	public Transform upperWaist = null;

	public Transform lowerWaist = null;

	public Transform head = null;

	public Transform benis01 = null;

	public Transform benis02 = null;

	public Transform benis03 = null;

	public Transform benis04 = null;

	public Transform benis05 = null;

	public float totalMass = 22f;

	public float strength = 0f;

	public bool flipForward = false;

	public float radius = 100f;

	public float power = 200f;

	public float upwardsForce = 0f;

	private Vector3 right = Vector3.right;

	private Vector3 up = Vector3.up;

	private Vector3 forward = Vector3.forward;

	private Vector3 worldRight = Vector3.right;

	private Vector3 worldUp = Vector3.up;

	private Vector3 worldForward = Vector3.forward;

	private Vector3 leftArmTwist = Vector3.up;

	private Vector3 leftArmSwing = Vector3.forward;

	private Vector3 leftElbowTwist = Vector3.left;

	private Vector3 leftElbowSwing = Vector3.forward;

	private Vector3 leftHandTwist = Vector3.left;

	private Vector3 leftHandSwing = Vector3.forward;

	private Vector3 rightArmTwist = Vector3.down;

	private Vector3 rightArmSwing = Vector3.forward;

	private Vector3 rightElbowTwist = Vector3.right;

	private Vector3 rightElbowSwing = Vector3.left;

	private Vector3 rightHandTwist = Vector3.right;

	private Vector3 rightHandSwing = Vector3.back;

	private List<BoneInfo> bones;

	private BoneInfo rootBone;

	private bool ready = false;

	private bool isRagdoll = false;

	private bool ragdollTransition = false;

	private bool _fireRagdoll;
	
	public bool fireRagdoll
	{
		get
		{
			return _fireRagdoll;
		}
		set
		{
			_fireRagdoll = value;
			if (!ragdollTransition)
			{
				if (value && !isRagdoll) {
					StartCoroutine(PrepareRagdoll(true));
				}
				if (!value && isRagdoll) {
					StartCoroutine(PrepareRagdoll(false));
				}
			}
		}
	}

	System.Random rnd = new System.Random();

	public KKRagdollRevampBase revampBase;

	/* private float armRadiusScaleOverride = 0.15f;

	private float armDensityOverride = 1f;

	private float armMinLimitOverride = -90f;

	private float armMaxLimitOverride = 0f;

	private float armSwingLimitOverride = 0f; */

    protected override void OnCardBeingSaved(GameMode currentGameMode)
	{
	}

	protected override void OnReload(GameMode currentGameMode)
	{
        Physics.sleepThreshold = 1f;

        if (Time.fixedDeltaTime != 0.005f)
		{
			Time.fixedDeltaTime = 0.005f;
		}
		StartCoroutine(DelayedInitiate(2f));
	}

	protected override void Update()
	{
		// if (base.transform.Find("BodyTop/p_cf_body_bone/cf_j_root/cf_n_height/cf_j_hips/cf_j_spine01/cf_j_spine02/cf_j_spine03/cf_j_neck/cf_j_head").GetComponent<Rigidbody>().IsSleeping()) { UnityEngine.Debug.Log("Head Slept!"); }
		
		if (ActivateRagdollAll.Value.IsDown())
		{
			fireRagdoll = !fireRagdoll;
		}
		base.Update();
	}

	private IEnumerator DelayedInitiate(float delayTime)
	{
		yield return new WaitForSeconds(delayTime);
		if (Initialize())
		{
			Cleanup();
			BuildCapsules();
			AddBreastColliders();
			AddHeadCollider();
			BuildBodies();
			BuildJoints();
			CalculateMass();
			yield return new WaitForSeconds(1f);
			ready = true;
			Physics.IgnoreCollision(pelvis.gameObject.GetComponent<Collider>(), leftHips.gameObject.GetComponent<Collider>());
			Physics.IgnoreCollision(pelvis.gameObject.GetComponent<Collider>(), rightHips.gameObject.GetComponent<Collider>());
        }
	}

	bool fkDebug = false;
	bool ikDisableTest = false;
	bool whatIsThisLmao = false;
	float ragdollPreloadDelay = 0.01f;

	private IEnumerator PrepareRagdoll(bool launching)
	{
		ragdollTransition = true;
		stilettoInstalled = KKRagdollRevampBase.isStiletto;
        if (launching)
		{
			if (stilettoInstalled && StilettoFix.Value) { StilettoCompatibility(true); }
			yield return new WaitForSeconds(ragdollPreloadDelay);
		}
        ToggleRagdoll();
		if (!launching)
		{
            yield return new WaitForSeconds(0.1f);
            if (stilettoInstalled && StilettoFix.Value) { StilettoCompatibility(false); }
        }
		ragdollTransition = false;
    }

	private void StilettoCompatibility(bool launching)
	{
		//UnityEngine.Debug.Log("Got to ragdoll fix routine");
		if (launching)
		{
			base.transform.gameObject.GetComponent<Stiletto.HeelInfo>().enabled = false;
		}
		else
		{
			base.transform.gameObject.GetComponent<Stiletto.HeelInfo>().enabled = true;
		}
    }

	public bool stilettoInstalled = false;
	
	private void ToggleRagdoll()
	{
		if (!ready)
		{
			return;
		}
		GenerateObjectColliders();
        bool wasIkOn = base.transform.Find("BodyTop/p_cf_body_bone").gameObject.GetComponent<RootMotion.FinalIK.FullBodyBipedIK>().enabled;
        if (!isRagdoll)
		{
			
			OCIChar ociChar = StudioObjectExtensions.GetOCIChar(base.ChaControl);
			if (ociChar.oiCharInfo.enableFK || fkDebug)
			{
				ociChar.oiCharInfo.enableFK = false;
				ociChar.ActiveKinematicMode(OICharInfo.KinematicMode.FK, false, true);
			}
			if (ikDisableTest)
			{
                ociChar.oiCharInfo.enableIK = false;
				if (whatIsThisLmao)
				{
					ociChar.finalIK.enabled = true;
				}
                ociChar.ActiveKinematicMode(OICharInfo.KinematicMode.IK, false, true);
            }
			ociChar.ChangeLookNeckPtn(3);


            foreach (BoneInfo bone in bones)
			{
				Rigidbody component = bone.anchor.GetComponent<Rigidbody>();
				component.isKinematic = false;
				component.useGravity = true;
				/* if (bone.name.Contains("Head")) {
					var currentRotation = bone.anchor.rotation;
					//var currentEulerAngles = new Vector3(0f, 0f, 0f);
					//currentRotation.eulerAngles = currentEulerAngles;
					var neckToggle = GameObject.Find("StudioScene/Canvas Main Menu/02_Manipulate/00_Chara/02_Kinematic/FK & IK/FKIK Toggle Neck");
					neckToggle.GetComponent<UnityEngine.UI.Toggle>().isOn = false;
					//bone.anchor.gameObject.GetComponent<Rigidbody>().rotation = currentRotation;
				} */
			}
			base.transform.Find("BodyTop/p_cf_body_bone").gameObject.GetComponent<Animator>().enabled = false;
			if (ToggleKKABMX.Value) { base.transform.gameObject.GetComponent<KKABMX.Core.BoneController>().enabled = false; }
            if (wasIkOn && AutoIKToggle.Value)
			{
				base.transform.Find("BodyTop/p_cf_body_bone").gameObject.GetComponent<RootMotion.FinalIK.FullBodyBipedIK>().enabled = false;
            }
			isRagdoll = true;
			return;
		}
		foreach (BoneInfo bone2 in bones)
		{
			Rigidbody component2 = bone2.anchor.GetComponent<Rigidbody>();
			component2.isKinematic = true;
			component2.useGravity = false;
            /* if (bone2.name.Contains("Head"))
            {
                var neckToggle = GameObject.Find("StudioScene/Canvas Main Menu/02_Manipulate/00_Chara/02_Kinematic/FK & IK/FKIK Toggle Neck");
                neckToggle.GetComponent<UnityEngine.UI.Toggle>().isOn = true;
            } */
        }
		base.transform.Find("BodyTop/p_cf_body_bone").gameObject.GetComponent<Animator>().enabled = true;
		if (ToggleKKABMX.Value) { base.transform.gameObject.GetComponent<KKABMX.Core.BoneController>().enabled = true; }
        if (wasIkOn && AutoIKToggle.Value)
        {
            base.transform.Find("BodyTop/p_cf_body_bone").gameObject.GetComponent<RootMotion.FinalIK.FullBodyBipedIK>().enabled = true;
        }
        isRagdoll = false;
	}

	private void GenerateObjectColliders()
	{
		List<Transform> list = (from Transform t in base.transform.parent
								where t.name.Contains("p_koi_stu_cube01_02")
								select t).ToList();
		foreach (Transform item in list)
		{
			if (item.GetComponent<BoxCollider>() == null)
			{
				item.gameObject.AddComponent<BoxCollider>();
			}
			if (item.GetComponent<Rigidbody>() == null)
			{
				Rigidbody rigidbody = item.gameObject.AddComponent<Rigidbody>();
				rigidbody.isKinematic = true;
				rigidbody.useGravity = false;
			}
		}
	}

	private bool Initialize()
	{
		AssignBones();
		string text = CheckConsistency();
		CalculateAxes();
		if (text.Length != 0)
		{
			Debug.Log(text);
			Debug.Log("Drag all bones from the hierarchy into their slots.");
			Debug.Log("Make sure your character is in T-Stand.");
		}
		else
		{
			Debug.Log("Make sure your character is in T-Stand.");
			Debug.Log("Make sure the blue axis faces in the same direction the chracter is looking.");
			Debug.Log("Use flipForward to flip the direction.");
		}
		return text.Length == 0;
	}

	private void AssignBones()
	{
		pelvis = base.transform.Find(pelvisName);
		leftHips = base.transform.Find(leftHipsName);
		leftKnee = base.transform.Find(leftKneeName);
		leftFoot = base.transform.Find(leftFootName);
		rightHips = base.transform.Find(rightHipsName);
		rightKnee = base.transform.Find(rightKneeName);
		rightFoot = base.transform.Find(rightFootName);
		leftArm = base.transform.Find(leftArmName);
		leftElbow = base.transform.Find(leftElbowName);
		leftHand = base.transform.Find(leftHandName);
		rightArm = base.transform.Find(rightArmName);
		rightElbow = base.transform.Find(rightElbowName);
		rightHand = base.transform.Find(rightHandName);
		middleSpine = base.transform.Find(middleSpineName);
		lowerSpine = base.transform.Find(lowerSpineName);
		upperWaist = base.transform.Find(upperWaistName);
		lowerWaist = base.transform.Find(lowerWaistName);
		head = base.transform.Find(headName);
	}

	private string CheckConsistency()
	{
		PrepareBones();
		Hashtable hashtable = new Hashtable();
		foreach (BoneInfo bone in bones)
		{
			if ((bool)bone.anchor)
			{
				if (hashtable[bone.anchor] != null)
				{
					BoneInfo boneInfo = (BoneInfo)hashtable[bone.anchor];
					return bone.name + " and " + boneInfo.name + " may not be assigned to the same bone.";
				}
				hashtable[bone.anchor] = bone;
			}
		}
		foreach (BoneInfo bone2 in bones)
		{
			if (bone2.anchor == null)
			{
				return bone2.name + " has not been assigned yet.\n";
			}
		}
		return "";
	}

	private void DecomposeVector(out Vector3 normalCompo, out Vector3 tangentCompo, Vector3 outwardDir, Vector3 outwardNormal)
	{
		outwardNormal = outwardNormal.normalized;
		normalCompo = outwardNormal * Vector3.Dot(outwardDir, outwardNormal);
		tangentCompo = outwardDir - normalCompo;
	}

	private void CalculateAxes()
	{
		if (head != null && pelvis != null)
		{
			up = CalculateDirectionAxis(pelvis.InverseTransformPoint(head.position));
		}
		if (rightElbow != null && pelvis != null)
		{
			DecomposeVector(out var _, out var tangentCompo, pelvis.InverseTransformPoint(rightElbow.position), up);
			right = CalculateDirectionAxis(tangentCompo);
		}
		forward = Vector3.Cross(right, up);
		if (flipForward)
		{
			forward = -forward;
		}
	}

	private void PrepareBones()
	{
		if ((bool)pelvis)
		{
			worldRight = pelvis.TransformDirection(right);
			worldUp = pelvis.TransformDirection(up);
			worldForward = pelvis.TransformDirection(forward);
		}
		bones = new List<BoneInfo>();
		rootBone = new BoneInfo
		{
			name = "Pelvis",
			anchor = pelvis,
			parent = null,
			density = 2.5f
		};
		bones.Add(rootBone);
		AddJoint("Lower Spine", lowerSpine, "Pelvis", worldRight, worldUp, -40f, 15f, 15f, 15f, null, 1f, 2.5f);
		AddJoint("Middle Spine", middleSpine, "Lower Spine", worldRight, worldUp, -20f, 15f, 15f, 15f, null, 1f, 2.5f);
		AddJoint("Lower Waist", lowerWaist, "Pelvis", worldRight, worldUp, -10f, 30f, 15f, 15f, null, 1f, 2.5f);
		AddJoint("Left Hips", leftHips, "Lower Waist", worldRight, worldForward, -10f, 90f, 65f, 80f, typeof(CapsuleCollider), 0.2f, 1.5f);
		AddJoint("Right Hips", rightHips, "Lower Waist", worldRight, worldForward, -10f, 90f, 65f, 80f, typeof(CapsuleCollider), 0.2f, 1.5f);
		AddMirroredJoint("Knee", leftKnee, rightKnee, "Hips", worldRight, worldForward, -130f, 0f, 0f, 0f, typeof(CapsuleCollider), 0.15f, 1.5f);
		AddMirroredJoint("Foot", leftFoot, rightFoot, "Knee", worldRight, worldForward, -30f, 10f, 0f, 5f, typeof(CapsuleCollider), 0.2f, 1f);
		AddJoint("Left Arm", leftArm, "Middle Spine", leftArmTwist, leftArmSwing, -95f, 60f, 95f, 95f, typeof(CapsuleCollider), 0.2f, 1f);
		AddJoint("Right Arm", rightArm, "Middle Spine", rightArmTwist, rightArmSwing, -95f, 60f, 95f, 95f, typeof(CapsuleCollider), 0.2f, 1f);
		AddJoint("Left Elbow", leftElbow, "Left Arm", worldUp, worldRight, -155f, 0f, 0f, 0f, typeof(CapsuleCollider), 0.15f, 1f);
		AddJoint("Right Elbow", rightElbow, "Right Arm", worldUp, worldRight, 0f, 155f, 0f, 0f, typeof(CapsuleCollider), 0.15f, 1f);
		//AddJoint("Left Hand", leftHand, "Left Elbow", worldForward, worldRight, -40f, 90f, 30f, 10f, typeof(CapsuleCollider), 0.10f, 1f);
		//AddJoint("Right Hand", rightHand, "Right Elbow", worldForward, worldRight, -40f, 90f, 30f, 10f, typeof(CapsuleCollider), 0.10f, 1f);
		AddJoint("Head", head, "Middle Spine", worldRight, worldForward, -60f, 40f, 40f, 70f, null, 1.5f, 1f);
	}

	private BoneInfo FindBone(string name)
	{
		return bones.Where((BoneInfo b) => b.name == name).FirstOrDefault();
	}

	private void AddMirroredJoint(string name, Transform leftAnchor, Transform rightAnchor, string parent, Vector3 worldTwistAxis, Vector3 worldSwingAxis, float minLimit, float maxLimit, float swing1Limit, float swing2limit, Type colliderType, float radiusScale, float density)
	{
		AddJoint("Left " + name, leftAnchor, parent, worldTwistAxis, worldSwingAxis, minLimit, maxLimit, swing1Limit, swing2limit, colliderType, radiusScale, density);
		AddJoint("Right " + name, rightAnchor, parent, worldTwistAxis, worldSwingAxis, minLimit, maxLimit, swing1Limit, swing2limit, colliderType, radiusScale, density);
	}

	private void AddJoint(string name, Transform anchor, string parent, Vector3 worldTwistAxis, Vector3 worldSwingAxis, float minLimit, float maxLimit, float swing1Limit, float swing2limit, Type colliderType, float radiusScale, float density)
	{
		BoneInfo boneInfo = new BoneInfo
		{
			name = name,
			anchor = anchor,
			axis = worldTwistAxis,
			normalAxis = worldSwingAxis,
			minLimit = minLimit,
			maxLimit = maxLimit,
			swing1Limit = swing1Limit,
			swing2Limit = swing2limit,
			density = density,
			colliderType = colliderType,
			radiusScale = radiusScale,
			/*isLocked = false,
			_isLocked = false*/
		};
		if (FindBone(parent) != null)
		{
			boneInfo.parent = FindBone(parent);
		}
		else if (name.StartsWith("Left"))
		{
			boneInfo.parent = FindBone("Left " + parent);
		}
		else if (name.StartsWith("Right"))
		{
			boneInfo.parent = FindBone("Right " + parent);
		}
		boneInfo.parent.children.Add(boneInfo);
		bones.Add(boneInfo);
	}

	private void BuildCapsules()
	{
		foreach (BoneInfo bone in bones)
		{
			if (bone.colliderType != typeof(CapsuleCollider))
			{
				continue;
			}
			int direction;
			float distance;
			if (bone.children.Count == 1)
			{
				BoneInfo boneInfo = bone.children[0];
				Vector3 position = boneInfo.anchor.position;
				CalculateDirection(bone.anchor.InverseTransformPoint(position), out direction, out distance);
			}
			else
			{
				Vector3 position2 = bone.anchor.position - bone.parent.anchor.position + bone.anchor.position;
				CalculateDirection(bone.anchor.InverseTransformPoint(position2), out direction, out distance);
				if (bone.anchor.GetComponentsInChildren<Transform>().Length > 1)
				{
					Bounds bounds = default(Bounds);
					Transform[] componentsInChildren = bone.anchor.GetComponentsInChildren<Transform>();
					foreach (Transform transform in componentsInChildren)
					{
						bounds.Encapsulate(bone.anchor.InverseTransformPoint(transform.position));
					}
					distance = ((!(distance > 0f)) ? bounds.min[direction] : bounds.max[direction]);
				}
			}
			CapsuleCollider capsuleCollider = bone.anchor.gameObject.AddComponent<CapsuleCollider>();
            if (bone.name.Contains("Foot"))
            {
                capsuleCollider.direction = 2;
				Vector3 zero = Vector3.zero;
				zero[direction] = distance * 0.5f;
				capsuleCollider.center = zero + new Vector3(0f, -0.01f, 0.05f);
				capsuleCollider.height = Mathf.Abs(distance) + 0.11f;
				capsuleCollider.radius = Mathf.Abs(distance * bone.radiusScale) + 0.02f;
			}
			else if ((bone.name.Contains("Knee")) || (bone.name.Contains("Hips")))
			{
                capsuleCollider.direction = direction;
                Vector3 zero = Vector3.zero;
                zero[direction] = distance * 0.5f;
                capsuleCollider.center = zero - new Vector3(0f, 0f, 0.02f);
                capsuleCollider.height = Mathf.Abs(distance);
                capsuleCollider.radius = Mathf.Abs(distance * bone.radiusScale) - 0.02f;
            }
			else if (bone.name.Contains("Elbow"))
			{
                capsuleCollider.direction = direction;
                Vector3 zero = Vector3.zero;
                zero[direction] = distance * 0.5f;
                capsuleCollider.center = zero;
                capsuleCollider.height = Mathf.Abs(distance);
                capsuleCollider.radius = Mathf.Abs(distance * bone.radiusScale) - 0.03f;
            }
			else if (bone.name.Contains("Arm"))
			{
                capsuleCollider.direction = direction;
                Vector3 zero = Vector3.zero;
                zero[direction] = distance * 0.5f;
                capsuleCollider.center = zero - new Vector3(0f, 0f, 0.01f);
                capsuleCollider.height = Mathf.Abs(distance);
                capsuleCollider.radius = Mathf.Abs(distance * bone.radiusScale) - 0.02f;
            }
			else
			{
				capsuleCollider.direction = direction;
				Vector3 zero = Vector3.zero;
				zero[direction] = distance * 0.5f;
				capsuleCollider.center = zero;
				capsuleCollider.height = Mathf.Abs(distance);
				capsuleCollider.radius = Mathf.Abs(distance * bone.radiusScale);
            }
        }
	}

	private void Cleanup()
	{
		foreach (BoneInfo bone in bones)
		{
			if ((bool)bone.anchor)
			{
				Joint[] componentsInChildren = bone.anchor.GetComponentsInChildren<Joint>();
				Joint[] array = componentsInChildren;
				foreach (Joint obj in array)
				{
					UnityEngine.Object.DestroyImmediate(obj);
				}
				Rigidbody[] componentsInChildren2 = bone.anchor.GetComponentsInChildren<Rigidbody>();
				Rigidbody[] array2 = componentsInChildren2;
				foreach (Rigidbody obj2 in array2)
				{
					UnityEngine.Object.DestroyImmediate(obj2);
				}
				Collider[] componentsInChildren3 = bone.anchor.GetComponentsInChildren<Collider>();
				Collider[] array3 = componentsInChildren3;
				foreach (Collider obj3 in array3)
				{
					UnityEngine.Object.DestroyImmediate(obj3);
				}
			}
		}
	}

	private void BuildBodies()
	{
		foreach (BoneInfo bone in bones)
		{
			bone.anchor.gameObject.AddComponent<Rigidbody>();
			bone.anchor.GetComponent<Rigidbody>().mass = bone.density;
			// bone.anchor.GetComponent<Rigidbody>().drag = 2f;
			if (bone.name.Contains("Head"))
			{
                bone.anchor.GetComponent<Rigidbody>().angularDrag = 10f;
            } else
			{
                bone.anchor.GetComponent<Rigidbody>().angularDrag = 2f;
            }
			bone.anchor.GetComponent<Rigidbody>().isKinematic = true;
			bone.anchor.GetComponent<Rigidbody>().useGravity = false;
		}
	}

	private void BuildJoints()
	{
		foreach (BoneInfo bone in bones)
		{
			if (bone.parent != null)
			{
				CharacterJoint characterJoint = (bone.joint = bone.anchor.gameObject.AddComponent<CharacterJoint>());
				/* if (bone.name.Contains("Elbow"))
				{
					characterJoint.axis = bone.axis;
					characterJoint.swingAxis = bone.normalAxis;
					//characterJoint.axis = bone.normalAxis;
					//characterJoint.swingAxis = bone.axis;
					//characterJoint.autoConfigureConnectedAnchor = false;
					//if (bone.name.Contains("Left"))
					//{
     //                   characterJoint.connectedAnchor = new Vector3(-0.25f, 0f, 0f);
     //               }
					//if (bone.name.Contains("Right"))
					//{
     //                   characterJoint.connectedAnchor = new Vector3(0.25f, 0f, 0f);
     //               }

                }
				else
				{ */
				characterJoint.axis = CalculateDirectionAxis(bone.anchor.InverseTransformDirection(bone.axis));
				characterJoint.swingAxis = CalculateDirectionAxis(bone.anchor.InverseTransformDirection(bone.normalAxis));
				// }
				characterJoint.anchor = Vector3.zero;
				characterJoint.connectedBody = bone.parent.anchor.GetComponent<Rigidbody>();
				characterJoint.enablePreprocessing = false;
				SoftJointLimit softJointLimit = default(SoftJointLimit);
				SoftJointLimitSpring softJointLimitSpring = default(SoftJointLimitSpring);
				softJointLimit.contactDistance = 0f;
				softJointLimit.limit = bone.minLimit;
				characterJoint.lowTwistLimit = softJointLimit;
				softJointLimit.limit = bone.maxLimit;
				characterJoint.highTwistLimit = softJointLimit;
				softJointLimit.limit = bone.swing1Limit;
				characterJoint.swing1Limit = softJointLimit;
				softJointLimit.limit = bone.swing2Limit;
				characterJoint.swing2Limit = softJointLimit;
				characterJoint.autoConfigureConnectedAnchor = false;
				if (bone.name.Contains("Foot"))
				{
					softJointLimitSpring.spring = 9f;
					characterJoint.twistLimitSpring = softJointLimitSpring;
					characterJoint.swingLimitSpring = softJointLimitSpring;
				}
				else if (bone.name.Contains("Hips"))
				{
					softJointLimitSpring.spring = 300f;
					characterJoint.swingLimitSpring = softJointLimitSpring;
				}
			}
		}
	}

	private void CalculateMassRecurse(BoneInfo bone)
	{
		float num = bone.anchor.GetComponent<Rigidbody>().mass;
		foreach (BoneInfo child in bone.children)
		{
			CalculateMassRecurse(child);
			num += child.summedMass;
		}
		bone.summedMass = num;
	}

	private void CalculateMass()
	{
		CalculateMassRecurse(rootBone);
		float num = totalMass / rootBone.summedMass;
		foreach (BoneInfo bone in bones)
		{
			bone.anchor.GetComponent<Rigidbody>().mass *= num;
			if (bone.name.Contains("Head"))
			{
				bone.anchor.GetComponent<Rigidbody>().mass = 5;
			}
		}
		CalculateMassRecurse(rootBone);
	}

	private static void CalculateDirection(Vector3 point, out int direction, out float distance)
	{
		direction = 0;
		if (Mathf.Abs(point[1]) > Mathf.Abs(point[0]))
		{
			direction = 1;
		}
		if (Mathf.Abs(point[2]) > Mathf.Abs(point[direction]))
		{
			direction = 2;
		}
		distance = point[direction];
	}

	private static Vector3 CalculateDirectionAxis(Vector3 point)
	{
		CalculateDirection(point, out var direction, out var distance);
		Vector3 zero = Vector3.zero;
		if (distance > 0f)
		{
			zero[direction] = 1f;
		}
		else
		{
			zero[direction] = -1f;
		}
		return zero;
	}

	private static int SmallestComponent(Vector3 point)
	{
		int num = 0;
		if (Mathf.Abs(point[1]) < Mathf.Abs(point[0]))
		{
			num = 1;
		}
		if (Mathf.Abs(point[2]) < Mathf.Abs(point[num]))
		{
			num = 2;
		}
		return num;
	}

	private static int LargestComponent(Vector3 point)
	{
		int num = 0;
		if (Mathf.Abs(point[1]) > Mathf.Abs(point[0]))
		{
			num = 1;
		}
		if (Mathf.Abs(point[2]) > Mathf.Abs(point[num]))
		{
			num = 2;
		}
		return num;
	}

	private static int SecondLargestComponent(Vector3 point)
	{
		int num = SmallestComponent(point);
		int num2 = LargestComponent(point);
		if (num < num2)
		{
			int num3 = num2;
			num2 = num;
			num = num3;
		}
		if (num == 0 && num2 == 1)
		{
			return 2;
		}
		if (num == 0 && num2 == 2)
		{
			return 1;
		}
		return 0;
	}

	private Bounds Clip(Bounds bounds, Transform relativeTo, Transform clipTransform, bool below)
	{
		int index = LargestComponent(bounds.size);
		if (Vector3.Dot(worldUp, relativeTo.TransformPoint(bounds.max)) > Vector3.Dot(worldUp, relativeTo.TransformPoint(bounds.min)) == below)
		{
			Vector3 min = bounds.min;
			min[index] = relativeTo.InverseTransformPoint(clipTransform.position)[index];
			bounds.min = min;
		}
		else
		{
			Vector3 max = bounds.max;
			max[index] = relativeTo.InverseTransformPoint(clipTransform.position)[index];
			bounds.max = max;
		}
		return bounds;
	}

	private Bounds GetBreastBounds(Transform relativeTo)
	{
		Bounds result = default(Bounds);
		result.Encapsulate(relativeTo.InverseTransformPoint(leftHips.position));
		result.Encapsulate(relativeTo.InverseTransformPoint(rightHips.position));
		result.Encapsulate(relativeTo.InverseTransformPoint(leftArm.position));
		result.Encapsulate(relativeTo.InverseTransformPoint(rightArm.position));
		Vector3 size = result.size;
		size[SmallestComponent(result.size)] = size[LargestComponent(result.size)] / 2f;
		result.size = size;
		return result;
	}

	private void AddBreastColliders()
	{
		if (lowerSpine != null && middleSpine != null && pelvis != null)
		{
			Bounds bounds = Clip(GetBreastBounds(pelvis), pelvis, lowerSpine, below: false);
			BoxCollider boxCollider = pelvis.gameObject.AddComponent<BoxCollider>();
			boxCollider.center = bounds.center;
			boxCollider.size = bounds.size / 1.5f;
			bounds = Clip(GetBreastBounds(middleSpine), middleSpine, middleSpine, below: true);
			boxCollider = middleSpine.gameObject.AddComponent<BoxCollider>();
			boxCollider.center = bounds.center;
			boxCollider.size = bounds.size / 1.5f;
		}
		else if (middleSpine != null && pelvis != null)
		{
			Bounds bounds2 = Clip(GetBreastBounds(pelvis), pelvis, middleSpine, below: false);
			BoxCollider boxCollider2 = pelvis.gameObject.AddComponent<BoxCollider>();
			boxCollider2.center = bounds2.center;
			boxCollider2.size = bounds2.size / 1.5f;
			bounds2 = Clip(GetBreastBounds(middleSpine), middleSpine, middleSpine, below: true);
			boxCollider2 = middleSpine.gameObject.AddComponent<BoxCollider>();
			boxCollider2.center = bounds2.center;
			boxCollider2.size = bounds2.size / 1.5f;
		}
		else
		{
			Bounds bounds3 = default(Bounds);
			bounds3.Encapsulate(pelvis.InverseTransformPoint(leftHips.position));
			bounds3.Encapsulate(pelvis.InverseTransformPoint(rightHips.position));
			bounds3.Encapsulate(pelvis.InverseTransformPoint(leftArm.position));
			bounds3.Encapsulate(pelvis.InverseTransformPoint(rightArm.position));
			Vector3 size = bounds3.size;
			size[SmallestComponent(bounds3.size)] = size[LargestComponent(bounds3.size)] / 2f;
			BoxCollider boxCollider3 = pelvis.gameObject.AddComponent<BoxCollider>();
			boxCollider3.center = bounds3.center;
			boxCollider3.size = size;
		}
	}

	private void AddHeadCollider()
	{
		if ((bool)head.GetComponent<Collider>())
		{
			UnityEngine.Object.Destroy(head.GetComponent<Collider>());
		}
		float num = Vector3.Distance(leftArm.transform.position, rightArm.transform.position);
		num /= 2f;
		SphereCollider sphereCollider = head.gameObject.AddComponent<SphereCollider>();
		sphereCollider.radius = num;
		Vector3 zero = Vector3.zero;
		CalculateDirection(head.InverseTransformPoint(pelvis.position), out var direction, out var distance);
		if (distance > 0f)
		{
			zero[direction] = 0f - num;
		}
		else
		{
			zero[direction] = num;
		}
		sphereCollider.center = zero;
	}

	private void AddBenisColliders()
	{
		if ((bool)benis01.GetComponent<Collider>())
		{
			UnityEngine.Object.Destroy(benis01.GetComponent<Collider>());
		}
		if ((bool)benis02.GetComponent<Collider>())
		{
			UnityEngine.Object.Destroy(benis02.GetComponent<Collider>());
		}
		if ((bool)benis03.GetComponent<Collider>())
		{
			UnityEngine.Object.Destroy(benis03.GetComponent<Collider>());
		}
		if ((bool)benis04.GetComponent<Collider>())
		{
			UnityEngine.Object.Destroy(benis04.GetComponent<Collider>());
		}
		if ((bool)benis05.GetComponent<Collider>())
		{
			UnityEngine.Object.Destroy(benis05.GetComponent<Collider>());
		}
		float num = 0.02f;
		SphereCollider sphereCollider = benis01.gameObject.AddComponent<SphereCollider>();
		sphereCollider.radius = num;
		SphereCollider sphereCollider2 = benis02.gameObject.AddComponent<SphereCollider>();
		sphereCollider2.radius = num;
		SphereCollider sphereCollider3 = benis03.gameObject.AddComponent<SphereCollider>();
		sphereCollider3.radius = num;
		SphereCollider sphereCollider4 = benis04.gameObject.AddComponent<SphereCollider>();
		sphereCollider4.radius = num;
		SphereCollider sphereCollider5 = benis05.gameObject.AddComponent<SphereCollider>();
		sphereCollider5.radius = num;
	}

	/* private bool isTwitching = false;

    private List<BoneInfo> twitchBones = new List<BoneInfo>();

	private int twitchForceMin = -200;

	private int twitchForceMax = 200;

	private void TwitchSimPrep()
	{
		foreach (BoneInfo bone in bones)
		{
			if ((bone.name.Contains("Right")) || (bone.name.Contains("Left")))
			{
				twitchBones.Add(bone);
			}
		}
	}

	private void TwitchSimExec()
	{ 
		if (!isTwitching && isRagdoll)
		{
			StartCoroutine(TwitchSim());
		}
	}

    private IEnumerator TwitchSim()
    {
		isTwitching = true;
		UnityEngine.Debug.Log("TWITCH START!");
        float loopingTime = 20f; // The duration of the loop in seconds.
        float delay = 2f; // The delay duration in seconds.

        float startTime = Time.time;
        float endTime = startTime + loopingTime;

		// Loop  for given duration
		do
		{
			UnityEngine.Debug.Log("TWITCH!");
			var getBone = rnd.Next(twitchBones.Count);
			var selectedRigid = twitchBones[getBone].anchor.GetComponent<Rigidbody>();
			float xRandom = rnd.Next(twitchForceMin, twitchForceMax);
			float yRandom = rnd.Next(twitchForceMin, twitchForceMax);
			float zRandom = rnd.Next(twitchForceMin, twitchForceMax);
			selectedRigid.AddForce(new Vector3(xRandom, yRandom, zRandom));
			yield return new WaitForSeconds(delay);
		}
		while (Time.time < endTime);
		//Reset Vars
        isTwitching = false;
		UnityEngine.Debug.Log("TWITCH END");
    } */

}