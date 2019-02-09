using System;
using UniRx;
using UnityEngine;

public class Block : Damageable {
	protected override IDisposable Die()
	{
		var psys = Level.Instance.explosionPrefab.InstantiateMe(transform.position, Quaternion.identity);
		psys.GetComponent<ParticleSystemRenderer>().material = GetComponentInChildren<Renderer>().material;

		Observable.Timer(TimeSpan.FromSeconds(1))
			.Subscribe(_ => Destroy(psys.gameObject))
			.AddTo(psys);
		
		
		Destroy(gameObject);

		return null;
	}
}
