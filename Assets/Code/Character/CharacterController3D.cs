﻿using UnityEngine;
using UnityEngine.Networking;

[RequireComponent(typeof(CharacterController))]
public class CharacterController3D : NetworkBehaviour {

	public Vector3 Velocity { get; set; }
	public bool Grounded { get; private set; }
	public bool Jumping { get; private set; }

	public float maxSpeed;
	public float sprintFactor;
	public float groundAcceleration;
	public float airAcceleration;
	public float jumpHeight;
	public float bounceFactor;
	public float mouseSensitivity;
	public float maxViewAngle;

	public Transform head;

	private CharacterController _controller;
	private Transform _transform;

	private Vector3 _inputVelocity;
	private float _headAngle;

	void Awake() {
		_controller = GetComponent<CharacterController>();
		_transform = transform;
	}

	void Start() {
		Cursor.lockState = CursorLockMode.Locked;
		Cursor.visible = false;
	}

	void OnEnable() {
		Grounded = false;
		Jumping = false;

		_headAngle = head.eulerAngles.y;
	}

	public override void OnStartLocalPlayer() {
		// Enables the camera on the local player
		head.GetComponentInChildren<Camera>(true).gameObject.SetActive(true);
	}

	void Update() {
		if (isLocalPlayer) {
			ReadInput();
			UpdateVelocity();
		}
	}

	void FixedUpdate() {
		if (isLocalPlayer) {
			ResetState();
			ApplyPhysics();
			Move();
		}
	}

	private void ReadInput() {
		// Rotation
		float mouseX = Input.GetAxis("Mouse X");
		float mouseY = Input.GetAxis("Mouse Y");

		//	-> Horizontal
		Vector3 angles = _transform.localEulerAngles;
		angles.y += mouseX * mouseSensitivity * Time.deltaTime;
		_transform.localEulerAngles = angles;

		//	-> Vertical
		_headAngle += mouseY * mouseSensitivity * Time.deltaTime;
		_headAngle = Mathf.Clamp(_headAngle, -maxViewAngle, maxViewAngle);
		angles = head.localEulerAngles;
		angles.x = -_headAngle;
		head.localEulerAngles = angles;


		// Movement
		float horizontal = Input.GetAxis("Horizontal");
		float vertical = Input.GetAxis("Vertical");

		//	-> Input velocity
		float speed = maxSpeed;
		if (Grounded && Input.GetButton("Sprint"))
			speed *= sprintFactor;
		_inputVelocity = transform.forward * vertical * speed;
		_inputVelocity += transform.right * horizontal * speed;
		_inputVelocity = Vector3.ClampMagnitude(_inputVelocity, speed);

		// Jump
		if (Input.GetButtonDown("Jump") && Grounded)
			Jumping = true;
	}

	public void UpdateVelocity() {
		// Movement
		if (Grounded)
			Velocity = Vector3.MoveTowards(Velocity, _inputVelocity, groundAcceleration * Time.deltaTime);
		else if (_inputVelocity != Vector3.zero) {
			Vector3 planarVelocity = Vector3.ProjectOnPlane(Velocity, _transform.up);
			Vector3 projection = Vector3.Project(planarVelocity, _inputVelocity);
			Vector3 newVelocity = Velocity;
			if (projection.sqrMagnitude > maxSpeed + maxSpeed && Vector3.Dot(projection, _inputVelocity) > 0) {
				newVelocity += Vector3.ProjectOnPlane(_inputVelocity, planarVelocity);
			}
			else
				newVelocity += _inputVelocity;
			Velocity = Vector3.MoveTowards(Velocity, newVelocity, airAcceleration * Time.deltaTime);
		}

		// Jump
		if (Jumping && Grounded) {
			float jumpSpeed = Mathf.Sqrt(2 * Physics.gravity.magnitude * jumpHeight);
			Velocity = Vector3.ProjectOnPlane(Velocity, Physics.gravity) - Physics.gravity.normalized * jumpSpeed;
		}
	}

	private void ResetState() {
		Grounded = false;
	}

	private void ApplyPhysics() {
		Velocity += Physics.gravity * Time.deltaTime;
	}

	private void Move() {
		_controller.Move(Velocity * Time.deltaTime);
	}

	void OnControllerColliderHit(ControllerColliderHit hit) {
		float angle = Vector3.Angle(Velocity, hit.normal);
		if (angle <= _controller.slopeLimit * Mathf.Rad2Deg) {
			Grounded = true;
			Jumping = false;
		}

		Vector3 bounce = bounceFactor * Vector3.Project(Velocity, hit.normal);
		Velocity = Vector3.ProjectOnPlane(Velocity, hit.normal) - bounce;
	}
}
