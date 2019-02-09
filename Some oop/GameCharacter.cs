using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Swipe;
using UniRx;
using UniRx.Triggers;
using UnityEngine;

public class GameCharacter : Damageable
{
	public AudioClip deathSound;
	
	public string faction = "DefaultFaction";
	
	protected ReactiveProperty<GameCharacter> target { get; } = new ReactiveProperty<GameCharacter>(null as GameCharacter); 
	
	private float? _centerOffset;
	protected Vector3 RaycastOrigin => transform.position +
	                                 Vector3.up * (_centerOffset ??
	                                               (_centerOffset = GetComponent<Collider>().bounds.extents.y).Value);

	[NonSerialized]private Animator _anim;
	protected Animator Anim => _anim ?? (_anim = GetComponentInChildren<Animator>());
	
	[NonSerialized]private Rigidbody _rb;
	protected Rigidbody Rb => _rb ?? (_rb = GetComponentInChildren<Rigidbody>());

	public ReadOnlyReactiveProperty<bool> Grounded { get; private set; }
	public ReadOnlyReactiveProperty<int> ObservableHP { get; private set; }
	public ReadOnlyReactiveProperty<bool> IsDead { get; private set; }
	
	// Use this for initialization
	protected override void Awake()
	{
		base.Awake();
		
		var hinf = new RaycastHit();
		Grounded = Observable.EveryFixedUpdate()
			.Select(_ => Physics.Raycast(RaycastOrigin, Vector3.down, out hinf, Level.Instance.DistBetweenAnchors, LayerMask.GetMask("Bounds","Blocks","Characters")))
			.ToReadOnlyReactiveProperty(true);

		Grounded.AddTo(this);
		
		Grounded.Subscribe(x => Anim.SetBool("Falling", !x));

		ObservableHP = this.ObserveEveryValueChanged(x => x.CurHp)
			.ToReadOnlyReactiveProperty(CurHp);

		IsDead = ObservableHP
			.Select(x => x <= 0)
			.ToReadOnlyReactiveProperty(false);

		Rb.OnCollisionEnterAsObservable()
			.Select(c => c.collider.GetComponentInParent<GameCharacter>())
			.Where(gc => gc && (gc.transform.position - transform.position).y < -Level.Instance.DistBetweenAnchors * .5f)
			.TakeUntil(IsDead.Where(x => x))
			.Subscribe(gc =>
			{
				target.Value = gc;
				gc.GetHit();
				Rb.AddForce(Vector3.up*3.5f,ForceMode.Impulse);
			})
			.AddTo(this);
	}
	
	protected bool CanMoveTo(Dir dir)
	{
		if (!Grounded.Value) return false;
		if (dir == Dir.up || dir == Dir.down) return false;
		
		var rcDir = new Vector3();
		switch (dir)
		{
			case Dir.left:
				rcDir = Vector3.left;
				break;
			case Dir.right:
				rcDir = Vector3.right;
				break;
		}

		return !Physics.Raycast(RaycastOrigin, rcDir, Level.Instance.DistBetweenAnchors, LayerMask.GetMask("Characters","Bounds","Blocks"));
	}

	protected void RotateTo(Dir dir)
	{
		switch (dir)
		{
			case Dir.left:
				Anim.transform.DOLocalRotate(Vector3.up *270, .25f);
				
				break;
			case Dir.right:				
				Anim.transform.DOLocalRotate(Vector3.up *90, .25f);				
				break;
		}
	}

	private Tween movingSequence;
	protected IObservable<Unit> Move(Dir dir, float duration)
	{
		RotateTo(dir);

		var res = Observable.ReturnUnit();
		
		if (dir == Dir.up || dir == Dir.down) return res;

		return res
			.Do(_ =>
			{
				var tgt = null as Transform;

				switch (dir)
				{
					case Dir.left:
						tgt = Level.Instance.PreviousAnchor(transform.position);
						break;
					case Dir.right:
						tgt = Level.Instance.NextAnchor(transform.position);
						break;
				}


				Rb.DOKill();
				movingSequence?.Kill();
				movingSequence=DOTween.Sequence()
					.AppendCallback(() => Anim.SetBool("Moving", true))
					.Append(Rb.DOMoveX(tgt.position.x, duration).SetEase(Ease.OutSine))
					.AppendInterval(duration*.5f)
					.AppendCallback(() => Anim.SetBool("Moving", false));
			})
			.Delay(TimeSpan.FromSeconds(duration * .5f));
	}

	protected IObservable<Unit> Attack(Dir dir,float delay,float duration)
	{
		RotateTo(dir);
		
		var atkName = "AttackForward";
		var rcDir = new Vector3();
		var dst = Level.Instance.DistBetweenAnchors;
		switch (dir)
		{
			case Dir.left:
				rcDir = Vector3.left;
				break;
			case Dir.right:
				rcDir = Vector3.right;
				break;
			case Dir.up:
				rcDir = Vector3.up;
				atkName = "AttackUp";
				dst *= 1.5f;
				break;
			case Dir.down:
				rcDir = Vector3.down;
				atkName = "AttackDown";
				dst *= 1.5f;
				break;
		}

		Anim.SetTrigger(atkName); //attack anim

		return Observable.ReturnUnit()
			.Delay(TimeSpan.FromSeconds(delay))
			.Do(_ =>
			{
				RaycastHit hinf;
				if (Physics.Raycast(RaycastOrigin, rcDir, out hinf, dst))
				{
					var dmg = hinf.collider.GetComponent<Damageable>();
					if(!dmg) return;

					if (dmg is GameCharacter)
					{
						if((dmg as GameCharacter).faction == faction) return;
						
						target.Value = dmg as GameCharacter;
					}
					
					dmg.GetHit();
				}
			})
			.Delay(TimeSpan.FromSeconds(duration))
			.TakeUntil(IsDead.Where(x => x));
	}

	protected override IDisposable Die()
	{
		Rb.DOKill();
		Rb.gameObject.layer = LayerMask.NameToLayer("Ignore Raycast");
		
		Anim.SetTrigger("Die");
		
		if(deathSound) SoundController.Play(deathSound.name);

		return transform.ObserveEveryValueChanged(x=>x.position.y) // если труп куда-то улетел ...
			.AsUnitObservable()
			.SkipUntil(Observable.TimerFrame(2))
			.Delay(TimeSpan.FromSeconds(.5f))
			.Merge(Observable.Timer(TimeSpan.FromSeconds(2)).AsUnitObservable()) //...  или прошло 2 секунды ...
			.Take(1)
			.Subscribe(_ =>
			{
				DOTween.Sequence()
					.Append(GetComponentInChildren<Renderer>()?.material.DOFade(0, 1f)) //... фейдим материал в 0 и удаляем геймобджект
					.AppendCallback(() => Destroy(gameObject));
			})
			.AddTo(this);
	}
}
