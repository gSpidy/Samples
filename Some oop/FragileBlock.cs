using System;
using DG.Tweening;
using UniRx;
using UnityEngine;

public class FragileBlock : Block, ICollisionTriggerable
{
	public float timeToDestroy = 2;

	[NonSerialized] private bool triggered = false;
	public void Trigger()
	{
		if(triggered) return;
		triggered = true;
		
		GetComponentInChildren<Renderer>()?.material
			.DOColor(new Color(0.22f, 0.13f, 0.08f), timeToDestroy).SetEase(Ease.InCubic);
		
		Observable.Timer(TimeSpan.FromSeconds(timeToDestroy))
			.Subscribe(_ => GetHit())
			.AddTo(this);
	}
}
