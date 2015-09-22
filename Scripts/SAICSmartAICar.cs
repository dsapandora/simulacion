//----------------------------------------------
//                  Smart AI Car
//
// Copyright © 2015 BoneCracker Games
// http://www.bonecrackergames.com
//
//----------------------------------------------

using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
	
[RequireComponent (typeof (Rigidbody))]
public class SAICSmartAICar : MonoBehaviour {

	private Rigidbody rigid;
	
	// Wheel transforms of the vehicle.
	public Transform FrontLeftWheelTransform;
	public Transform FrontRightWheelTransform;
	public Transform RearLeftWheelTransform;
	public Transform RearRightWheelTransform;

	//Wheel colliders of the vehicle.
	public WheelCollider FrontLeftWheelCollider;
	public WheelCollider FrontRightWheelCollider;
	public WheelCollider RearLeftWheelCollider;
	public WheelCollider RearRightWheelCollider;

	private WheelCollider[] allWheelColliders;
	public float antiRoll = 10000.0f;

	//Waypoint Container.
	public SAICWaypointsContainer waypointsContainer;
	public int currentWaypoint = 0;

	//  Raycast distances.
	public LayerMask obstacleLayers;
	public int wideRayLength = 20;
	public int tightRayLength = 20;
	public int sideRayLength = 3;

	private float newInputSteer = 0f;
	private bool  raycasting = false;
	private float resetTime = 0f;
		
	//Center of mass.
	public Transform COM;

	private bool reversing = false;

	// Steer, motor, and brake inputs.
	private float steerInput = 0f;
	private float motorInput = 0f;
	private float brakeInput = 0f;

	// Brake Zone.
	private float maximumSpeedInBrakeZone = 0f;
	private bool inBrakeZone = false;
	
	// Counts laps and how many waypoints passed.
	public int lap = 0;
	public int totalWaypointPassed = 0;
	public int nextWaypointPassRadius = 20;
	public bool ignoreWaypointNow = false;

	// Unity's Navigator.
	private NavMeshAgent navigator;
	private GameObject navigatorObject;

	// Set wheel drive of the vehicle.
	public enum WheelType{FWD, RWD, AWD};
	public WheelType _wheelTypeChoise;

	//Vehicle Mecanim
	[HideInInspector]
	public AnimationCurve[] engineTorqueCurve;
	public float engineTorque = 750.0f;
	public float brakeTorque = 1000f;
	public float minimumEngineRPM = 1000.0f;
	public float maximumEngineRPM = 6000.0f;
	public float maximumSpeed = 180.0f;
	[HideInInspector]
	public float speed = 0f;
	private float engineRPM = 0f;
	public float steerAngle = 20.0f;
	public float highSpeedSteerAngle = 10.0f;
	public float highSpeedSteerAngleAtSpeed = 80.0f;
	private float defsteerAngle;

	//Gears.
	public int currentGear;
	public int totalGears = 6;
	[HideInInspector]
	public bool changingGear = false;
	[HideInInspector]
	public float[] gearSpeed;

	// Vehicle acceleration for adjusting rigidbody drag.
	private float acceleration;
	private float lastVelocity;

	//Sounds
	private AudioSource engineAudio;
	public AudioClip engineClip;
	private AudioSource skidAudio;
	public AudioClip skidClip;
	private AudioSource crashAudio;
	public AudioClip[] crashClips;
	
	// Each wheel transform's rotation value.
	private float rotationValueFL, rotationValueFR, rotationValueRL, rotationValueRR;
	private float[] rotationValueExtra;
	
	public GameObject wheelSmoke;
	private List <ParticleSystem> wheelParticles = new List<ParticleSystem>();
	
	public GameObject chassis;
	private float horizontalLean = 0.0f;
	private float verticalLean = 0.0f;

	public bool info = false;
	
	void Start (){

		rigid = GetComponent<Rigidbody>();
		allWheelColliders = GetComponentsInChildren<WheelCollider>();

		if(!waypointsContainer)
			waypointsContainer = FindObjectOfType(typeof(SAICWaypointsContainer)) as SAICWaypointsContainer;

		SoundsInitialize(); 

		if(wheelSmoke)
			SmokeInit();

		navigatorObject = new GameObject("Navigator");
		navigatorObject.transform.parent = transform;
		navigatorObject.transform.localPosition = Vector3.zero;
		navigatorObject.AddComponent<NavMeshAgent>();
		navigatorObject.GetComponent<NavMeshAgent>().radius = 1;
		navigatorObject.GetComponent<NavMeshAgent>().speed = 1f;
		navigatorObject.GetComponent<NavMeshAgent>().height = 1;
		navigatorObject.GetComponent<NavMeshAgent>().avoidancePriority = 99;
		navigator = navigatorObject.GetComponent<NavMeshAgent>();
			
		// Lower the center of mass for make more stable car.
		rigid.centerOfMass = new Vector3(COM.localPosition.x * transform.localScale.x , COM.localPosition.y * transform.localScale.y , COM.localPosition.z * transform.localScale.z);
		rigid.maxAngularVelocity = 3f;
		defsteerAngle = steerAngle;
		
	}

	public void CreateWheelColliders (){
		
		List <Transform> allWheelTransforms = new List<Transform>();
		allWheelTransforms.Add(FrontLeftWheelTransform); allWheelTransforms.Add(FrontRightWheelTransform); allWheelTransforms.Add(RearLeftWheelTransform); allWheelTransforms.Add(RearRightWheelTransform);
		
		if(allWheelTransforms[0] == null){
			Debug.LogError("You haven't choose your Wheel Transforms. Please select all of your Wheel Transforms before creating Wheel Colliders. Script needs to know their positions, aye?");
			return;
		}
		
		transform.rotation = Quaternion.identity;
		
		GameObject WheelColliders = new GameObject("Wheel Colliders");
		WheelColliders.transform.parent = transform;
		WheelColliders.transform.rotation = transform.rotation;
		WheelColliders.transform.localPosition = Vector3.zero;
		WheelColliders.transform.localScale = Vector3.one;
		
		foreach(Transform wheel in allWheelTransforms){
			
			GameObject wheelcollider = new GameObject(wheel.transform.name); 
			
			wheelcollider.transform.position = wheel.transform.position;
			wheelcollider.transform.rotation = transform.rotation;
			wheelcollider.transform.name = wheel.transform.name;
			wheelcollider.transform.parent = WheelColliders.transform;
			wheelcollider.transform.localScale = Vector3.one;
			wheelcollider.layer = LayerMask.NameToLayer("Wheel");
			wheelcollider.AddComponent<WheelCollider>();
			wheelcollider.GetComponent<WheelCollider>().radius = (wheel.GetComponent<MeshRenderer>().bounds.size.y / 2f) / transform.localScale.y;
			
			wheelcollider.AddComponent<SAICWheelCollider>();
			
			JointSpring spring = wheelcollider.GetComponent<WheelCollider>().suspensionSpring;
			
			spring.spring = 30000f;
			spring.damper = 2000f;
			
			wheelcollider.GetComponent<WheelCollider>().suspensionSpring = spring;
			wheelcollider.GetComponent<WheelCollider>().suspensionDistance = .25f;
			wheelcollider.GetComponent<WheelCollider>().forceAppPointDistance = .25f;
			wheelcollider.GetComponent<WheelCollider>().mass = 100f;
			wheelcollider.GetComponent<WheelCollider>().wheelDampingRate = .5f;
			
			wheelcollider.transform.localPosition = new Vector3(wheelcollider.transform.localPosition.x, wheelcollider.transform.localPosition.y + (wheelcollider.GetComponent<WheelCollider>().suspensionDistance / 2f), wheelcollider.transform.localPosition.z);
			
			WheelFrictionCurve sidewaysFriction = wheelcollider.GetComponent<WheelCollider>().sidewaysFriction;
			WheelFrictionCurve forwardFriction = wheelcollider.GetComponent<WheelCollider>().forwardFriction;
			
			forwardFriction.extremumSlip = .4f;
			forwardFriction.extremumValue = 1;
			forwardFriction.asymptoteSlip = .8f;
			forwardFriction.asymptoteValue = .75f;
			forwardFriction.stiffness = 1.75f;
			
			sidewaysFriction.extremumSlip = .25f;
			sidewaysFriction.extremumValue = 1;
			sidewaysFriction.asymptoteSlip = .5f;
			sidewaysFriction.asymptoteValue = .75f;
			sidewaysFriction.stiffness = 2f;
			
			wheelcollider.GetComponent<WheelCollider>().sidewaysFriction = sidewaysFriction;
			wheelcollider.GetComponent<WheelCollider>().forwardFriction = forwardFriction;
			
		}
		
		WheelColliders.layer = LayerMask.NameToLayer("Wheel");
		
		WheelCollider[] allWheelColliders = new WheelCollider[allWheelTransforms.Count];
		allWheelColliders = GetComponentsInChildren<WheelCollider>();
		
		FrontLeftWheelCollider = allWheelColliders[0];
		FrontRightWheelCollider = allWheelColliders[1];
		RearLeftWheelCollider = allWheelColliders[2];
		RearRightWheelCollider = allWheelColliders[3];
		
	}

	public AudioSource CreateAudioSource(string audioName, float minDistance, float volume, AudioClip audioClip, bool loop, bool playNow, bool destroyAfterFinished){
		
		GameObject audioSource = new GameObject(audioName);
		audioSource.transform.position = transform.position;
		audioSource.transform.rotation = transform.rotation;
		audioSource.transform.parent = transform;
		audioSource.AddComponent<AudioSource>();
		audioSource.GetComponent<AudioSource>().minDistance = minDistance;
		audioSource.GetComponent<AudioSource>().volume = volume;
		audioSource.GetComponent<AudioSource>().clip = audioClip;
		audioSource.GetComponent<AudioSource>().loop = loop;
		audioSource.GetComponent<AudioSource>().spatialBlend = 1f;
		
		if(playNow)
			audioSource.GetComponent<AudioSource>().Play();
		
		if(destroyAfterFinished)
			Destroy(audioSource, audioClip.length);
		
		return audioSource.GetComponent<AudioSource>();
		
	}
	
	void SoundsInitialize(){
		
		engineAudio = CreateAudioSource("engineSound", 5f, 1f, engineClip, true, true, false);
		skidAudio = CreateAudioSource("skidSound", 5f, 0f, skidClip, true, true, false);
		
	}

	public void SmokeInit (){

		for(int i = 0; i < allWheelColliders.Length; i++){
			GameObject ps = (GameObject)Instantiate(wheelSmoke, transform.position, transform.rotation) as GameObject;
			wheelParticles.Add(ps.GetComponent<ParticleSystem>());
			ps.GetComponent<ParticleSystem>().enableEmission = false;
			ps.transform.SetParent(allWheelColliders[i].transform);
			ps.transform.localPosition = Vector3.zero;
		}
		
	}
	
	void OnGUI (){
	
		if(info){
			
			GUI.backgroundColor = Color.black;
			float guiWidth = Screen.width/2 - 200;
			
			GUI.Box(new Rect(Screen.width-410 - guiWidth, 10, 400, 220), "");
			GUI.Label(new Rect(Screen.width-400 - guiWidth, 10, 400, 150), "Engine RPM : " + Mathf.CeilToInt(engineRPM));
			GUI.Label(new Rect(Screen.width-400 - guiWidth, 30, 400, 150), "speed : " + Mathf.CeilToInt(speed));
			if(_wheelTypeChoise == WheelType.FWD){
				GUI.Label(new Rect(Screen.width-400 - guiWidth, 50, 400, 150), "Left Wheel RPM : " + Mathf.CeilToInt(FrontLeftWheelCollider.rpm));
				GUI.Label(new Rect(Screen.width-400 - guiWidth, 70, 400, 150), "Right Wheel RPM : " + Mathf.CeilToInt(FrontRightWheelCollider.rpm));
				GUI.Label(new Rect(Screen.width-400 - guiWidth, 90, 400, 150), "Left Wheel Torque : " + Mathf.CeilToInt(FrontLeftWheelCollider.motorTorque));
				GUI.Label(new Rect(Screen.width-400 - guiWidth, 110, 400, 150), "Right Wheel Torque : " + Mathf.CeilToInt(FrontRightWheelCollider.motorTorque));
				GUI.Label(new Rect(Screen.width-400 - guiWidth, 130, 400, 150), "Left Wheel brake : " + Mathf.CeilToInt(FrontLeftWheelCollider.brakeTorque));
				GUI.Label(new Rect(Screen.width-400 - guiWidth, 150, 400, 150), "Right Wheel brake : " + Mathf.CeilToInt(FrontRightWheelCollider.brakeTorque));
				GUI.Label(new Rect(Screen.width-400 - guiWidth, 170, 400, 150), "Steer Angle : " + Mathf.CeilToInt(FrontLeftWheelCollider.steerAngle));
			}
			if(_wheelTypeChoise == WheelType.RWD || _wheelTypeChoise == WheelType.AWD){
				GUI.Label(new Rect(Screen.width-400 - guiWidth, 50, 400, 150), "Left Wheel RPM : " + Mathf.CeilToInt(RearLeftWheelCollider.rpm));
				GUI.Label(new Rect(Screen.width-400 - guiWidth, 70, 400, 150), "Right Wheel RPM : " + Mathf.CeilToInt(RearRightWheelCollider.rpm));
				GUI.Label(new Rect(Screen.width-400 - guiWidth, 90, 400, 150), "Left Wheel Torque : " + Mathf.CeilToInt(RearLeftWheelCollider.motorTorque));
				GUI.Label(new Rect(Screen.width-400 - guiWidth, 110, 400, 150), "Right Wheel Torque : " + Mathf.CeilToInt(RearRightWheelCollider.motorTorque));
				GUI.Label(new Rect(Screen.width-400 - guiWidth, 130, 400, 150), "Left Wheel brake : " + Mathf.CeilToInt(RearLeftWheelCollider.brakeTorque));
				GUI.Label(new Rect(Screen.width-400 - guiWidth, 150, 400, 150), "Right Wheel brake : " + Mathf.CeilToInt(RearRightWheelCollider.brakeTorque));
				GUI.Label(new Rect(Screen.width-400 - guiWidth, 170, 400, 150), "Steer Angle : " + Mathf.CeilToInt(FrontLeftWheelCollider.steerAngle));
			}
			
			GUI.backgroundColor = Color.blue;
			GUI.Button (new Rect(Screen.width-30 - guiWidth, 165, 10, Mathf.Clamp(((-motorInput) * 100), -100, 0)), "");
			
			GUI.backgroundColor = Color.red;
			GUI.Button (new Rect(Screen.width-45 - guiWidth, 165, 10, Mathf.Clamp((-brakeInput * 100), -100, 0)), "");
			
		}
		
	}

	void Update(){

		WheelAlign();
		SkidAudio();
		ShiftGears();

		if(chassis)
			Chassis();

		if(wheelSmoke)
			SmokeInstantiateRate();

		navigator.transform.localPosition = Vector3.zero;

	}
		
	void  FixedUpdate (){
	
		Engine();
		Navigation();
		FixedRaycasts();
		Resetting();
		AntiRollBars();

	}

	void Engine(){
		
		//speed.
		speed = rigid.velocity.magnitude * 3.0f;
		
		//Acceleration Calculation.
		acceleration = 0f;
		acceleration = (transform.InverseTransformDirection(rigid.velocity).z - lastVelocity) / Time.fixedDeltaTime;
		lastVelocity = transform.InverseTransformDirection(rigid.velocity).z;
		
		//Drag Limit Depends On Vehicle Acceleration.
		rigid.drag = Mathf.Clamp((acceleration / 50f), 0f, 1f);
		
		//Steer Limit.
		steerAngle = Mathf.Lerp(defsteerAngle, highSpeedSteerAngle, (speed / highSpeedSteerAngleAtSpeed));

		FrontLeftWheelCollider.steerAngle = ApplySteering();
		FrontRightWheelCollider.steerAngle = ApplySteering();

		float wheelRPM = ((Mathf.Abs((FrontLeftWheelCollider.rpm * FrontLeftWheelCollider.radius) + (FrontRightWheelCollider.rpm * FrontRightWheelCollider.radius)) / 2f) / 3.25f);

		engineRPM = Mathf.Clamp((Mathf.Lerp(0 - (minimumEngineRPM * (currentGear + 1)), maximumEngineRPM,wheelRPM / (gearSpeed[currentGear] * 1.25f)) + minimumEngineRPM), minimumEngineRPM, maximumEngineRPM);

		//Engine Audio Volume.
		if(engineAudio){
			engineAudio.GetComponent<AudioSource>().pitch = Mathf.Lerp (engineAudio.GetComponent<AudioSource>().pitch, Mathf.Lerp (1f, 2f, (engineRPM - minimumEngineRPM / 1.5f) / (maximumEngineRPM + minimumEngineRPM)), Time.deltaTime * 5);
			if(!changingGear)
				engineAudio.GetComponent<AudioSource>().volume = Mathf.Lerp (engineAudio.GetComponent<AudioSource>().volume, Mathf.Clamp (motorInput - brakeInput, .35f, .85f), Time.deltaTime*  5);
			else
				engineAudio.GetComponent<AudioSource>().volume = Mathf.Lerp (engineAudio.GetComponent<AudioSource>().volume, .35f, Time.deltaTime*  5);
		}

		//Applying WheelCollider Motor Torques Depends On Wheel Type Choice.
		switch(_wheelTypeChoise){

		case WheelType.FWD:
			FrontLeftWheelCollider.motorTorque = ApplyWheelMotorTorque();
			FrontRightWheelCollider.motorTorque = ApplyWheelMotorTorque();
			break;
		case WheelType.RWD:
			RearLeftWheelCollider.motorTorque = ApplyWheelMotorTorque();
			RearRightWheelCollider.motorTorque = ApplyWheelMotorTorque();
			break;
		case WheelType.AWD:
			FrontLeftWheelCollider.motorTorque = ApplyWheelMotorTorque();
			FrontRightWheelCollider.motorTorque = ApplyWheelMotorTorque();
			RearLeftWheelCollider.motorTorque = ApplyWheelMotorTorque();
			RearRightWheelCollider.motorTorque = ApplyWheelMotorTorque();
			break;

		}

		// Apply the brake torque values to the rear wheels.
		FrontLeftWheelCollider.brakeTorque = ApplyWheelBrakeTorque() / 1f;
		FrontRightWheelCollider.brakeTorque = ApplyWheelBrakeTorque() / 1f;
		RearLeftWheelCollider.brakeTorque = ApplyWheelBrakeTorque();
		RearRightWheelCollider.brakeTorque = ApplyWheelBrakeTorque();

	}

	public float ApplyWheelMotorTorque(){

		float torque = 0;

		if(changingGear){
			torque = 0;
		}else{
			if(!reversing)
				torque = (engineTorque * Mathf.Clamp(motorInput - (brakeInput / 1.5f), 0f, 1f)) * engineTorqueCurve[currentGear].Evaluate(speed);
			else
				torque = (-engineTorque * Mathf.Clamp(motorInput, 0f, 1f)) * engineTorqueCurve[currentGear].Evaluate(speed);
		}

		return torque;

	}

	#region Applying Torque, Brake, Steering
	public float ApplyWheelBrakeTorque(){
		
		float torque = 0;

		if(!reversing)
			torque = brakeTorque * brakeInput;
		else
			torque = 0;
		
		return torque;
		
	}

	public float ApplySteering(){
		
		float steering = 0;
		
		steering = Mathf.Clamp((steerAngle * steerInput), -steerAngle, steerAngle);
		
		return steering;
		
	}
	#endregion

	public void AntiRollBars (){
		
		WheelHit FrontWheelHit;
		
		float travelFL = 1.0f;
		float travelFR = 1.0f;
		
		bool groundedFL= FrontLeftWheelCollider.GetGroundHit(out FrontWheelHit);
		
		if (groundedFL)
			travelFL = (-FrontLeftWheelCollider.transform.InverseTransformPoint(FrontWheelHit.point).y - FrontLeftWheelCollider.radius) / FrontLeftWheelCollider.suspensionDistance;
		
		bool groundedFR= FrontRightWheelCollider.GetGroundHit(out FrontWheelHit);
		
		if (groundedFR)
			travelFR = (-FrontRightWheelCollider.transform.InverseTransformPoint(FrontWheelHit.point).y - FrontRightWheelCollider.radius) / FrontRightWheelCollider.suspensionDistance;
		
		float antiRollForceFront= (travelFL - travelFR) * antiRoll;
		
		if (groundedFL)
			rigid.AddForceAtPosition(FrontLeftWheelCollider.transform.up * -antiRollForceFront, FrontLeftWheelCollider.transform.position); 
		if (groundedFR)
			rigid.AddForceAtPosition(FrontRightWheelCollider.transform.up * antiRollForceFront, FrontRightWheelCollider.transform.position); 
		
		WheelHit RearWheelHit;
		
		float travelRL = 1.0f;
		float travelRR = 1.0f;
		
		bool groundedRL= RearLeftWheelCollider.GetGroundHit(out RearWheelHit);
		
		if (groundedRL)
			travelRL = (-RearLeftWheelCollider.transform.InverseTransformPoint(RearWheelHit.point).y - RearLeftWheelCollider.radius) / RearLeftWheelCollider.suspensionDistance;
		
		bool groundedRR= RearRightWheelCollider.GetGroundHit(out RearWheelHit);
		
		if (groundedRR)
			travelRR = (-RearRightWheelCollider.transform.InverseTransformPoint(RearWheelHit.point).y - RearRightWheelCollider.radius) / RearRightWheelCollider.suspensionDistance;
		
		float antiRollForceRear= (travelRL - travelRR) * antiRoll;
		
		if (groundedRL)
			rigid.AddForceAtPosition(RearLeftWheelCollider.transform.up * -antiRollForceRear, RearLeftWheelCollider.transform.position); 
		if (groundedRR)
			rigid.AddForceAtPosition(RearRightWheelCollider.transform.up * antiRollForceRear, RearRightWheelCollider.transform.position);
		
		if (groundedRR && groundedRL)
			rigid.AddRelativeTorque((Vector3.up * (steerInput * motorInput)) * 2000f);
		
	}

	public void ShiftGears (){
			
		if(currentGear < totalGears - 1 && !changingGear){
			if(speed >= (gearSpeed[currentGear] * 1.1f) && FrontLeftWheelCollider.rpm >= 0){
				StartCoroutine("ChangingGear", currentGear + 1);
			}
		}
		
		if(currentGear > 0){
			
			if(!changingGear){
				
				if(speed < (gearSpeed[currentGear - 1] * .9f)){
					StartCoroutine("ChangingGear", currentGear - 1);
				}
				
			}
		}
		
	}
	
	IEnumerator ChangingGear(int gear){
		
		changingGear = true;
		yield return new WaitForSeconds(.25f);
		changingGear = false;
		currentGear = gear;
		
	}
	
	void Navigation (){

		if(!waypointsContainer){
			Debug.LogError("Waypoints Container Couldn't Found!");
			return;
		}
		if(waypointsContainer && waypointsContainer.waypoints.Count < 1){
			Debug.LogError("Waypoints Container Doesn't Have Any Waypoints!");
			return;
		}
			
		// Next waypoint's position.
		Vector3 nextWaypointPosition = transform.InverseTransformPoint( new Vector3(waypointsContainer.waypoints[currentWaypoint].position.x, transform.position.y, waypointsContainer.waypoints[currentWaypoint].position.z));
		navigator.SetDestination(waypointsContainer.waypoints[currentWaypoint].position);

		//Steering Input.
		if(!reversing){
			if(!ignoreWaypointNow)
				steerInput = Mathf.Clamp((transform.InverseTransformDirection(navigator.desiredVelocity).x + newInputSteer), -1f, 1f);
			else
				steerInput = Mathf.Clamp(newInputSteer, -1f, 1f);
		}else{
			steerInput = Mathf.Clamp((-transform.InverseTransformDirection(navigator.desiredVelocity).x + newInputSteer), -1f, 1f);
		}

		if(!inBrakeZone){
			if(speed >= 25)
				brakeInput = Mathf.Lerp(0f, 1f, (Mathf.Abs(steerInput)));
			else
				brakeInput = 0f;
		}else{
			if(speed >= 5)
				brakeInput = Mathf.Lerp(0f, 1f, (speed - maximumSpeedInBrakeZone) / maximumSpeedInBrakeZone);
			else
				brakeInput = 0f;
		}

		if(!inBrakeZone){
			motorInput = Mathf.Clamp(Mathf.Abs(transform.InverseTransformDirection(navigator.desiredVelocity).z) - Mathf.Abs(transform.InverseTransformDirection(navigator.desiredVelocity).x), .5f, 1f) - (Mathf.Abs(newInputSteer) / 2f);
		}else{
			if(speed >= 5)
				motorInput = Mathf.Clamp(Mathf.Abs(transform.InverseTransformDirection(navigator.desiredVelocity).z), 0f, Mathf.Lerp(1f, 0f, (speed - maximumSpeedInBrakeZone) / maximumSpeedInBrakeZone));
			else
				motorInput = Mathf.Clamp(Mathf.Abs(transform.InverseTransformDirection(navigator.desiredVelocity).z), .5f, Mathf.Lerp(1f, 0f, (speed - maximumSpeedInBrakeZone) / maximumSpeedInBrakeZone));
		}

		// Checks for the distance to next waypoint. If it is less than written value, then pass to next waypoint.
		if (nextWaypointPosition.magnitude < nextWaypointPassRadius){
				currentWaypoint ++;
				totalWaypointPassed ++;
			
		// If all waypoints are passed, sets the current waypoint to first waypoint and increase lap.
			if (currentWaypoint >= waypointsContainer.waypoints.Count){
				currentWaypoint = 0;
				lap ++;
			}
		}
			
	}
		
	void Resetting (){

		if(speed <= 15 && transform.InverseTransformDirection(rigid.velocity).z < 1f)
			resetTime += Time.deltaTime;
		
		if(resetTime >= 4)
			reversing = true;
		
		if(resetTime >= 6 || speed >= 25){
			reversing = false;
			resetTime = 0;
		}

		Vector3 thisT = ( new Vector3( transform.localEulerAngles.x, transform.localEulerAngles.y, transform.localEulerAngles.z));
				
		if(thisT.z < 300 && thisT.z > 60 && speed <= 5)
			transform.localEulerAngles = new Vector3( transform.localEulerAngles.x, transform.localEulerAngles.y, 0);
			
	}
		
	public void WheelAlign (){
		
		RaycastHit hit;
		WheelHit CorrespondingGroundHit;
		
		
		//Front Left Wheel Transform.
		Vector3 ColliderCenterPointFL = FrontLeftWheelCollider.transform.TransformPoint( FrontLeftWheelCollider.center );
		FrontLeftWheelCollider.GetGroundHit( out CorrespondingGroundHit );
		
		if(Physics.Raycast( ColliderCenterPointFL, -FrontLeftWheelCollider.transform.up, out hit, (FrontLeftWheelCollider.suspensionDistance + FrontLeftWheelCollider.radius) * transform.localScale.y) && !hit.collider.isTrigger && hit.transform.root != transform){
			FrontLeftWheelTransform.transform.position = hit.point + (FrontLeftWheelCollider.transform.up * FrontLeftWheelCollider.radius) * transform.localScale.y;
			float extension = (-FrontLeftWheelCollider.transform.InverseTransformPoint(CorrespondingGroundHit.point).y - FrontLeftWheelCollider.radius) / FrontLeftWheelCollider.suspensionDistance;
			Debug.DrawLine(CorrespondingGroundHit.point, CorrespondingGroundHit.point + FrontLeftWheelCollider.transform.up * (CorrespondingGroundHit.force / rigid.mass), extension <= 0.0 ? Color.magenta : Color.white);
			Debug.DrawLine(CorrespondingGroundHit.point, CorrespondingGroundHit.point - FrontLeftWheelCollider.transform.forward * CorrespondingGroundHit.forwardSlip, Color.green);
			Debug.DrawLine(CorrespondingGroundHit.point, CorrespondingGroundHit.point - FrontLeftWheelCollider.transform.right * CorrespondingGroundHit.sidewaysSlip, Color.red);
		}else{
			FrontLeftWheelTransform.transform.position = ColliderCenterPointFL - (FrontLeftWheelCollider.transform.up * FrontLeftWheelCollider.suspensionDistance) * transform.localScale.y;
		}
		
		rotationValueFL += FrontLeftWheelCollider.rpm * ( 6 ) * Time.deltaTime;
		FrontLeftWheelTransform.transform.rotation = FrontLeftWheelCollider.transform.rotation * Quaternion.Euler( rotationValueFL, FrontLeftWheelCollider.steerAngle, FrontLeftWheelCollider.transform.rotation.z);
		
		
		//Front Right Wheel Transform.
		Vector3 ColliderCenterPointFR = FrontRightWheelCollider.transform.TransformPoint( FrontRightWheelCollider.center );
		FrontRightWheelCollider.GetGroundHit( out CorrespondingGroundHit );
		
		if(Physics.Raycast( ColliderCenterPointFR, -FrontRightWheelCollider.transform.up, out hit, (FrontRightWheelCollider.suspensionDistance + FrontRightWheelCollider.radius) * transform.localScale.y) && !hit.collider.isTrigger && hit.transform.root != transform){
			FrontRightWheelTransform.transform.position = hit.point + (FrontRightWheelCollider.transform.up * FrontRightWheelCollider.radius) * transform.localScale.y;
			float extension = (-FrontRightWheelCollider.transform.InverseTransformPoint(CorrespondingGroundHit.point).y - FrontRightWheelCollider.radius) / FrontRightWheelCollider.suspensionDistance;
			Debug.DrawLine(CorrespondingGroundHit.point, CorrespondingGroundHit.point + FrontRightWheelCollider.transform.up * (CorrespondingGroundHit.force / rigid.mass), extension <= 0.0 ? Color.magenta : Color.white);
			Debug.DrawLine(CorrespondingGroundHit.point, CorrespondingGroundHit.point - FrontRightWheelCollider.transform.forward * CorrespondingGroundHit.forwardSlip, Color.green);
			Debug.DrawLine(CorrespondingGroundHit.point, CorrespondingGroundHit.point - FrontRightWheelCollider.transform.right * CorrespondingGroundHit.sidewaysSlip, Color.red);
		}else{
			FrontRightWheelTransform.transform.position = ColliderCenterPointFR - (FrontRightWheelCollider.transform.up * FrontRightWheelCollider.suspensionDistance) * transform.localScale.y;
		}
		
		rotationValueFR += FrontRightWheelCollider.rpm * ( 6 ) * Time.deltaTime;
		FrontRightWheelTransform.transform.rotation = FrontRightWheelCollider.transform.rotation * Quaternion.Euler( rotationValueFR, FrontRightWheelCollider.steerAngle, FrontRightWheelCollider.transform.rotation.z);
		
		
		//Rear Left Wheel Transform.
		Vector3 ColliderCenterPointRL = RearLeftWheelCollider.transform.TransformPoint( RearLeftWheelCollider.center );
		RearLeftWheelCollider.GetGroundHit( out CorrespondingGroundHit );
		
		if(Physics.Raycast( ColliderCenterPointRL, -RearLeftWheelCollider.transform.up, out hit, (RearLeftWheelCollider.suspensionDistance + RearLeftWheelCollider.radius) * transform.localScale.y) && !hit.collider.isTrigger && hit.transform.root != transform){
			RearLeftWheelTransform.transform.position = hit.point + (RearLeftWheelCollider.transform.up * RearLeftWheelCollider.radius) * transform.localScale.y;
			float extension = (-RearLeftWheelCollider.transform.InverseTransformPoint(CorrespondingGroundHit.point).y - RearLeftWheelCollider.radius) / RearLeftWheelCollider.suspensionDistance;
			Debug.DrawLine(CorrespondingGroundHit.point, CorrespondingGroundHit.point + RearLeftWheelCollider.transform.up * (CorrespondingGroundHit.force / rigid.mass), extension <= 0.0 ? Color.magenta : Color.white);
			Debug.DrawLine(CorrespondingGroundHit.point, CorrespondingGroundHit.point - RearLeftWheelCollider.transform.forward * CorrespondingGroundHit.forwardSlip, Color.green);
			Debug.DrawLine(CorrespondingGroundHit.point, CorrespondingGroundHit.point - RearLeftWheelCollider.transform.right * CorrespondingGroundHit.sidewaysSlip, Color.red);
		}else{
			RearLeftWheelTransform.transform.position = ColliderCenterPointRL - (RearLeftWheelCollider.transform.up * RearLeftWheelCollider.suspensionDistance) * transform.localScale.y;
		}
		
		RearLeftWheelTransform.transform.rotation = RearLeftWheelCollider.transform.rotation * Quaternion.Euler( rotationValueRL, 0, RearLeftWheelCollider.transform.rotation.z);
		rotationValueRL += RearLeftWheelCollider.rpm * ( 6 ) * Time.deltaTime;
		
		
		//Rear Right Wheel Transform.
		Vector3 ColliderCenterPointRR = RearRightWheelCollider.transform.TransformPoint( RearRightWheelCollider.center );
		RearRightWheelCollider.GetGroundHit( out CorrespondingGroundHit );
		
		if(Physics.Raycast( ColliderCenterPointRR, -RearRightWheelCollider.transform.up, out hit, (RearRightWheelCollider.suspensionDistance + RearRightWheelCollider.radius) * transform.localScale.y) && !hit.collider.isTrigger && hit.transform.root != transform){
			RearRightWheelTransform.transform.position = hit.point + (RearRightWheelCollider.transform.up * RearRightWheelCollider.radius) * transform.localScale.y;
			float extension = (-RearRightWheelCollider.transform.InverseTransformPoint(CorrespondingGroundHit.point).y - RearRightWheelCollider.radius) / RearRightWheelCollider.suspensionDistance;
			Debug.DrawLine(CorrespondingGroundHit.point, CorrespondingGroundHit.point + RearRightWheelCollider.transform.up * (CorrespondingGroundHit.force / rigid.mass), extension <= 0.0 ? Color.magenta : Color.white);
			Debug.DrawLine(CorrespondingGroundHit.point, CorrespondingGroundHit.point - RearRightWheelCollider.transform.forward * CorrespondingGroundHit.forwardSlip, Color.green);
			Debug.DrawLine(CorrespondingGroundHit.point, CorrespondingGroundHit.point - RearRightWheelCollider.transform.right * CorrespondingGroundHit.sidewaysSlip, Color.red);
		}else{
			RearRightWheelTransform.transform.position = ColliderCenterPointRR - (RearRightWheelCollider.transform.up * RearRightWheelCollider.suspensionDistance) * transform.localScale.y;
		}
		
		RearRightWheelTransform.transform.rotation = RearRightWheelCollider.transform.rotation * Quaternion.Euler( rotationValueRR, 0, RearRightWheelCollider.transform.rotation.z);
		rotationValueRR += RearRightWheelCollider.rpm * ( 6 ) * Time.deltaTime;
		
	}
		
	void FixedRaycasts(){
		
		Vector3 fwd = transform.TransformDirection ( new Vector3(0, 0, 1));
		Vector3 pivotPos = new Vector3(transform.localPosition.x, FrontLeftWheelCollider.transform.position.y, transform.localPosition.z);
		RaycastHit hit;
		
		// New bools effected by fixed raycasts.
		bool  tightTurn = false;
		bool  wideTurn = false;
		bool  sideTurn = false;
		bool  tightTurn1 = false;
		bool  wideTurn1 = false;
		bool  sideTurn1 = false;
		
		// New input steers effected by fixed raycasts.
		float newinputSteer1 = 0.0f;
		float newinputSteer2 = 0.0f;
		float newinputSteer3 = 0.0f;
		float newinputSteer4 = 0.0f;
		float newinputSteer5 = 0.0f;
		float newinputSteer6 = 0.0f;
		
		// Drawing Rays.
		Debug.DrawRay (pivotPos, Quaternion.AngleAxis(25, transform.up) * fwd * wideRayLength, Color.white);
		Debug.DrawRay (pivotPos, Quaternion.AngleAxis(-25, transform.up) * fwd * wideRayLength, Color.white);
		
		Debug.DrawRay (pivotPos, Quaternion.AngleAxis(7, transform.up) * fwd * tightRayLength, Color.white);
		Debug.DrawRay (pivotPos, Quaternion.AngleAxis(-7, transform.up) * fwd * tightRayLength, Color.white);
		
		Debug.DrawRay (pivotPos, Quaternion.AngleAxis(90, transform.up) * fwd * sideRayLength, Color.white);
		Debug.DrawRay (pivotPos, Quaternion.AngleAxis(-90, transform.up) * fwd * sideRayLength, Color.white);
		
		// Wide Raycasts.
		if (Physics.Raycast (pivotPos, Quaternion.AngleAxis(25, transform.up) * fwd, out hit, wideRayLength, obstacleLayers) && !hit.collider.isTrigger && hit.transform.root != transform) {
			Debug.DrawRay (pivotPos, Quaternion.AngleAxis(25, transform.up) * fwd * wideRayLength, Color.red);
			newinputSteer1 = Mathf.Lerp (-.5f, 0, (hit.distance / wideRayLength));
			wideTurn = true;
		}
		
		else{
			newinputSteer1 = 0;
			wideTurn = false;
		}
		
		if (Physics.Raycast (pivotPos, Quaternion.AngleAxis(-25, transform.up) * fwd, out hit, wideRayLength, obstacleLayers) && !hit.collider.isTrigger && hit.transform.root != transform) {
			Debug.DrawRay (pivotPos, Quaternion.AngleAxis(-25, transform.up) * fwd * wideRayLength, Color.red);
			newinputSteer4 = Mathf.Lerp (.5f, 0, (hit.distance / wideRayLength));
			wideTurn1 = true;
		}
		
		else{
			newinputSteer4 = 0;
			wideTurn1 = false;
		}
		
		// Tight Raycasts.
		if (Physics.Raycast (pivotPos, Quaternion.AngleAxis(7, transform.up) * fwd, out hit, tightRayLength, obstacleLayers) && !hit.collider.isTrigger && hit.transform.root != transform) {
			Debug.DrawRay (pivotPos, Quaternion.AngleAxis(7, transform.up) * fwd * tightRayLength , Color.red);
			newinputSteer3 = Mathf.Lerp (-1, 0, (hit.distance / tightRayLength));
			tightTurn = true;
		}
		
		else{
			newinputSteer3 = 0;
			tightTurn = false;
		}
		
		if (Physics.Raycast (pivotPos, Quaternion.AngleAxis(-7, transform.up) * fwd, out hit, tightRayLength, obstacleLayers) && !hit.collider.isTrigger && hit.transform.root != transform) {
			Debug.DrawRay (pivotPos, Quaternion.AngleAxis(-7, transform.up) * fwd * tightRayLength, Color.red);
			newinputSteer2 = Mathf.Lerp (1, 0, (hit.distance / tightRayLength));
			tightTurn1 = true;
		}
		
		else{
			newinputSteer2 = 0;
			tightTurn1 = false;
		}
		
		// Side Raycasts.
		if (Physics.Raycast (pivotPos, Quaternion.AngleAxis(90, transform.up) * fwd, out hit, sideRayLength, obstacleLayers) && !hit.collider.isTrigger && hit.transform.root != transform) {
			Debug.DrawRay (pivotPos, Quaternion.AngleAxis(90, transform.up) * fwd * sideRayLength , Color.red);
			newinputSteer5 = Mathf.Lerp (-1, 0, (hit.distance / sideRayLength));
			sideTurn = true;
		}
		
		else{
			newinputSteer5 = 0;
			sideTurn = false;
		}
		
		if (Physics.Raycast (pivotPos, Quaternion.AngleAxis(-90, transform.up) * fwd, out hit, sideRayLength, obstacleLayers) && !hit.collider.isTrigger && hit.transform.root != transform) {
			Debug.DrawRay (pivotPos, Quaternion.AngleAxis(-90, transform.up) * fwd * sideRayLength, Color.red);
			newinputSteer6 = Mathf.Lerp (1, 0, (hit.distance / sideRayLength));
			sideTurn1 = true;
		}
		
		else{
			newinputSteer6 = 0;
			sideTurn1 = false;
		}
		
		if(wideTurn || wideTurn1 || tightTurn || tightTurn1 || sideTurn || sideTurn1)
			raycasting = true;
		else
			raycasting = false;
		
		if(raycasting)
			newInputSteer = (newinputSteer1 + newinputSteer2 + newinputSteer3 + newinputSteer4 + newinputSteer5 + newinputSteer6);
		else
			newInputSteer = 0;
		
		if(raycasting && Mathf.Abs(newInputSteer) > .5f)
			ignoreWaypointNow = true;
		else
			ignoreWaypointNow = false;
		
	}
		
	public void SkidAudio (){
		
		if(!skidClip)
			return;
		
		WheelHit CorrespondingGroundHitF;
		FrontRightWheelCollider.GetGroundHit( out CorrespondingGroundHitF );
		
		WheelHit CorrespondingGroundHitR;
		RearRightWheelCollider.GetGroundHit( out CorrespondingGroundHitR );
		
		if(Mathf.Abs(CorrespondingGroundHitF.sidewaysSlip) > .25f || Mathf.Abs(CorrespondingGroundHitR.forwardSlip) > .5f || Mathf.Abs(CorrespondingGroundHitF.forwardSlip) > .5f){
			if(rigid.velocity.magnitude > 1f)
				skidAudio.volume = Mathf.Abs(CorrespondingGroundHitF.sidewaysSlip) + ((Mathf.Abs(CorrespondingGroundHitF.forwardSlip) + Mathf.Abs(CorrespondingGroundHitR.forwardSlip)) / 4f);
			else
				skidAudio.volume -= Time.deltaTime;
		}else{
			skidAudio.volume -= Time.deltaTime;
		}
		
	}
		
	void SmokeInstantiateRate () {

		for(int i = 0; i < allWheelColliders.Length; i++){
		
		WheelHit CorrespondingGroundHit;
		allWheelColliders[i].GetGroundHit(out CorrespondingGroundHit);
					
			if(Mathf.Abs(CorrespondingGroundHit.sidewaysSlip) > .25f || Mathf.Abs(CorrespondingGroundHit.forwardSlip) > .5f){
				if(!wheelParticles[i].enableEmission && speed > 1)
					wheelParticles[i].enableEmission = true;
			}else{
				if(wheelParticles[i].enableEmission)
					wheelParticles[i].enableEmission = false;
			}

		}
		
	}

	public void Chassis (){
		
		verticalLean = Mathf.Clamp(Mathf.Lerp (verticalLean, rigid.angularVelocity.x * 4f, Time.deltaTime * 3f), -3.0f, 3.0f);
		
		WheelHit CorrespondingGroundHit;
		FrontRightWheelCollider.GetGroundHit(out CorrespondingGroundHit);
		
		float normalizedLeanAngle = Mathf.Clamp(CorrespondingGroundHit.sidewaysSlip, -1f, 1f);
		
		if(normalizedLeanAngle > 0f)
			normalizedLeanAngle = 1;
		else
			normalizedLeanAngle = -1;
		
		horizontalLean = Mathf.Clamp(Mathf.Lerp (horizontalLean, (Mathf.Abs (transform.InverseTransformDirection(rigid.angularVelocity).y) * -normalizedLeanAngle) * 4f, Time.deltaTime * 3f), -3.0f, 3.0f);
		
		Quaternion target = Quaternion.Euler(verticalLean, chassis.transform.localRotation.y + (rigid.angularVelocity.z), horizontalLean);
		chassis.transform.localRotation = target;
		
		rigid.centerOfMass = new Vector3((COM.localPosition.x) * transform.localScale.x , (COM.localPosition.y) * transform.localScale.y , (COM.localPosition.z) * transform.localScale.z);
		
	}
		
	void OnCollisionEnter( Collision collision ){
		
		if (collision.contacts.Length > 0){
			
			if(collision.relativeVelocity.magnitude > 5 && crashClips.Length > 0){
				if (collision.contacts[0].thisCollider.gameObject.layer != LayerMask.NameToLayer("Wheel") ){
					crashAudio = CreateAudioSource("crashSound", 5f, 1f, crashClips[UnityEngine.Random.Range(0, crashClips.Length)], false, true, true);
				}
			}
			
		}
		
	}

	void OnTriggerEnter ( Collider other ){

		if(other.gameObject.GetComponent<SAICBrakeZone>()){
			inBrakeZone = true;
			maximumSpeedInBrakeZone = other.gameObject.GetComponent<SAICBrakeZone>().targetSpeed;
		}
		
	}
		
	void OnTriggerExit ( Collider other  ){
			 
		if(other.gameObject.GetComponent<SAICBrakeZone>()){
			inBrakeZone = false;
			maximumSpeedInBrakeZone = 0;
		}
			
	}
		
}