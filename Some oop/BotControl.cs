using System;
using System.Collections;
using System.Collections.Generic;
using Swipe;
using UniRx;
using UniRx.Triggers;
using UnityEngine;

public class BotControl : GameCharacter
{
	[SerializeField]
	private Collider m_sensor; 
	
	private bool wanderDir;
	
	// Use this for initialization
	void Start ()
	{
		wanderDir = UnityEngine.Random.value < .5f;

		Observable.Timer(TimeSpan.FromSeconds(UnityEngine.Random.value),TimeSpan.FromSeconds(.8f))
			.Do(_=>CheckDestroy())
			.Where(_=>CurHp>0)
			.SelectMany(_ =>
			{
				if(!target.Value) return Wander();

				var tgtdir = target.Value.transform.position.x < transform.position.x ? Dir.left : Dir.right;

				return CanMoveTo(tgtdir) ? Move(tgtdir, .5f) : Attack(tgtdir, .4f, .2f);
			})
			.TakeUntil(IsDead.Where(x=>x))
			.Subscribe()
			.AddTo(this);

		m_sensor.OnTriggerEnterAsObservable()
			.Select(c => c.GetComponent<GameCharacter>())
			.Where(x => x && x.faction != faction)
			.Subscribe(other =>
			{
				target.Value = other;
			})
			.AddTo(this);

	}

	private void CheckDestroy()
	{
		if(!PlayerControl.Current) return;
		if(transform.position.y-PlayerControl.Current.transform.position.y>17) Destroy(gameObject);
	}

	IObservable<Unit> Wander()
	{
		var dir = wanderDir? Dir.right:Dir.left;

		var rcpos = (dir == Dir.left
			? Level.Instance.PreviousAnchor(transform.position)
			: Level.Instance.NextAnchor(transform.position))?.position;

		if (!rcpos.HasValue)
		{
			wanderDir = !wanderDir;	
			return Observable.ReturnUnit();
		}
		
		rcpos = new Vector3(rcpos.Value.x,RaycastOrigin.y,rcpos.Value.z);
		
		if (CanMoveTo(dir) &&
			CheckChasm(rcpos.Value)
		) return Move(dir, .5f);

		wanderDir = !wanderDir;
		return Observable.ReturnUnit();
	}

	bool CheckChasm(Vector3 origin) => Physics.Raycast(origin, Vector3.down, Level.Instance.DistBetweenAnchors);
}
