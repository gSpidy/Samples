using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Swipe;
using UniRx;
using UniRx.Triggers;
using UnityEngine;

public class PlayerControl : GameCharacter
{
	private static PlayerControl _current;
	public static PlayerControl Current
	{
		get
		{
			if (!_current)
				_current = FindObjectOfType<PlayerControl>();

			return _current;
		}
	}

	private void Start()
	{
		var blockInput = false;
		
		SwipeControl.Direction
			.Where(_=>!blockInput)
			.SelectMany(dir =>
			{
				blockInput = true;

				if (CanMoveTo(dir)) return Move(dir, .5f);
				
				SoundController.Play("swing");
				return Attack(dir, .2f, .2f);
			})
			.TakeUntil(IsDead.Where(x=>x))
			.Subscribe(_=>blockInput = false)
			.AddTo(this);

		Rb.OnCollisionEnterAsObservable()
			.Select(x => x.collider.GetComponent<ICollisionTriggerable>())
			.Where(x => x != null)
			.Subscribe(ctr => ctr.Trigger())
			.AddTo(this);
		
		//botkill reward and sound
		var botkillObs = target
			.Where(x => x)
			.Select(tgt => tgt.IsDead.Where(x => x).Take(1))
			.Switch()
			.Do(_ =>
			{
				Money.Current += GameController.Instance.botKillReward;
			})
			.Publish();
		
		var thr = botkillObs.Throttle(TimeSpan.FromSeconds(1.4f));

		botkillObs.Buffer(thr)
			.Where(x => x.Count > 1)
			.Subscribe(_ => SoundController.Play("botkill"))
			.AddTo(this);

		botkillObs.Connect().AddTo(this);

	}

	protected override IDisposable Die()
	{
		base.Die()?.Dispose();

		Observable.Timer(TimeSpan.FromSeconds(1.5f))
			.Subscribe(_ => GameController.Instance.Lose())
			.AddTo(this);
		
		return null;
	}
}
