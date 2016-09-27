using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class CharacterController3D : MonoBehaviour {

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

	void Update() {
		ReadInput();
		UpdateVelocity();
	}

	void FixedUpdate() {
		ResetState();
		ApplyPhysics();
		Move();
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
			Vector3 projection = Vector3.Project(Velocity, _inputVelocity);
			projection = Vector3.ClampMagnitude(projection, Velocity.magnitude);
			Velocity = Vector3.MoveTowards(Velocity, Velocity + _inputVelocity - projection, airAcceleration * Time.deltaTime);
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
