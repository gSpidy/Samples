using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UniRx;
using UniRx.Triggers;
using UnityEngine;
using UnityStandardAssets.CrossPlatformInput;

public class PlayerControl : MonoBehaviour
{
	[SerializeField] private int m_baseHp = 3;
	[SerializeField] private float m_baseFuel = 100;
	[SerializeField] private float m_baseHandling = 2;
	[SerializeField] private float m_baseAcceleration = 15f;
	
	[NonSerialized]public int maxHp = 3;
	[NonSerialized]public float maxFuel = 150;
	[NonSerialized]public float handling = 2;
	private float _acceleration = 15f;

	public AudioSource rotorAudioSource;

	public Sprite targetWpSprite;
	
	public Transform skinsParent;
	public List<GameObject> availableSkins;
	
	public IntReactiveProperty CurHP { get; }=new IntReactiveProperty(1);
	public FloatReactiveProperty Fuel { get; }=new FloatReactiveProperty(1);

	[NonSerialized]private ReadOnlyReactiveProperty<bool> _isFalling;

	public ReadOnlyReactiveProperty<bool> isFalling => _isFalling ?? (_isFalling = Observable.CombineLatest(
			                                                   CurHP.Select(x => x <= 0), //out of HP
			                                                   Fuel.Select(x => x <= 0), //out of fuel
			                                                   (oohp, oofuel) => oohp || oofuel
		                                                   )
		                                                   .ToReadOnlyReactiveProperty(false));

	public ReactiveProperty<Transform> TargetWP = new ReactiveProperty<Transform>((Transform)null);

	[NonSerialized] private Rigidbody _rb;
	public Rigidbody Rb => _rb ?? (_rb = GetComponent<Rigidbody>());

	[NonSerialized] private List<Rotor> _rotors;
	
	public void SetStats()
	{
		var skinId = PlayerPrefs.GetInt("skinselected", 0);
		
		skinsParent.ChildsEnumerated()
			.ToList()
			.ForEach(x=>Destroy(x.item.gameObject));

		_rotors = availableSkins[Mathf.Clamp(PlayerPrefs.GetInt("skinselected", 0), 0, availableSkins.Count - 1)]
			.InstantiateMe(skinsParent.position, skinsParent.rotation, skinsParent)
			.GetComponentsInChildren<Rotor>()
			.ToList();

		_acceleration = Mathf.Lerp(m_baseAcceleration, m_baseAcceleration * 2, (float) PlayerPrefs.GetInt($"Speed{skinId}", 0) / 9);
		maxFuel = m_baseFuel  * .4f * (3f + PlayerPrefs.GetInt($"Fuel{skinId}", 0));
		handling = m_baseHandling + .5f * PlayerPrefs.GetInt($"Handiness{skinId}", 0);
		maxHp = m_baseHp + PlayerPrefs.GetInt($"Durability{skinId}", 0);
		
		if(CurHP.Value > maxHp)
			CurHP.Value = maxHp;
		if(Fuel.Value > maxFuel)
			Fuel.Value = maxFuel;
	}
	
	void Start ()
	{
		SetStats();
		CurHP.Value = maxHp;
		Fuel.Value = maxFuel;
		
		//input
		var rawInput = Observable.EveryFixedUpdate()
			.Select(_ =>
				new Vector3(
					CrossPlatformInputManager.GetAxis("Horizontal"),
					CrossPlatformInputManager.GetAxis("Mouse ScrollWheel"),
					CrossPlatformInputManager.GetAxis("Vertical")
					))
			.Select(v=>Vector3.ClampMagnitude(v,1))
			.WithLatestFrom(isFalling,(input,falling)=>falling?Vector3.down:input)
			.Scan((lerped,real)=>Vector3.Lerp(lerped,real,Time.fixedDeltaTime*handling)) 
			.Publish();
		
		//movement
		rawInput
			.Select(raw => 
				Quaternion.LookRotation(Vector3.ProjectOnPlane(Camera.main.transform.forward, Vector3.up)) * raw)
			.Subscribe(input =>
			{
				Rb.AddForce(input*_acceleration);
				Fuel.Value -= Time.fixedDeltaTime;
			})
			.AddTo(this);
		
		//rotation
		rawInput
			.Select(inp =>
			{
				var camForwardRt = Quaternion.LookRotation(Vector3.ProjectOnPlane(Camera.main.transform.forward, Vector3.up));

				var inpRt = Quaternion.Euler(
					Mathf.LerpUnclamped(0, 25, inp.z), 0,
					Mathf.LerpUnclamped(0, 45, -inp.x));

				/*var inpRtX = Quaternion.AngleAxis(Mathf.LerpUnclamped(0, 45, -inp.x), camForwardRt * Vector3.forward);
				var inpRtY = Quaternion.AngleAxis(Mathf.LerpUnclamped(0, 45, inp.z), camForwardRt * Vector3.right);*/

				return camForwardRt * inpRt;
			})
			.WithLatestFrom(isFalling,(rt,isFalling)=>isFalling?transform.rotation:rt)
			.Subscribe(desiredRt =>
			{
				transform.rotation = Quaternion.Slerp(transform.rotation,desiredRt,Time.fixedDeltaTime);		
			}).AddTo(this);

		//rotor speed and sound
		rawInput
			.Select(inp => Vector3.ProjectOnPlane(inp, Vector3.up).magnitude + inp.y * .5f +.8f)
			.Select(rSpd => (float)Math.Round(rSpd,3))
			.DistinctUntilChanged()
			.Subscribe(rSpd =>
			{
				//print(rSpd);

				rotorAudioSource.pitch = rSpd;
				_rotors.ForEach(x=>x.TorqueMultiplier = rSpd);
			}).AddTo(this);

		rawInput.Connect().AddTo(this);


		// collision HP --
		this.OnCollisionEnterAsObservable()
			.Where(c => ((1 << c.gameObject.layer) & LayerMask.GetMask("Car", "LandingPads")) == 0)
			.AsUnitObservable()
			.ThrottleFirst(TimeSpan.FromSeconds(2))
			.Subscribe(_ => CurHP.Value--)
			.AddTo(this);

		var mark = new GameObject("TargetWaypoint").AddComponent<MapMark>();
		mark.MarkSprite = targetWpSprite;

		TargetWP
			.Where(x => x)
			.Subscribe(tgt=>mark.transform.position = tgt.position)
			.AddTo(this);

	}
}