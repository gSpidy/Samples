using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using UniRx;
using UnityEngine;

public class GameCamera : MonoBehaviourSingleton<GameCamera>
{
	public float lerpPower = .1f;
	public ReactiveProperty<BasePlayer> lookAtPlayer = new ReactiveProperty<BasePlayer>();

	public void ViewRandomNpc()
	{
		lookAtPlayer.Value = FindObjectsOfType<BasePlayer>()
			.Where(x => x != GameController.Instance.followPlayer)
			.OrderBy(_ => Random.value)
			.First();
	}
	
	// Use this for initialization
	void Start () {
		ViewRandomNpc();

		//Здесь игрок может быть разделен на кучу частей, и камера наблюдает за центром между этими частями, игрок тоже может меняться в любое время
		lookAtPlayer
			.Select(pl => Observable.EveryFixedUpdate()
				.WithLatestFrom(pl.ObservableCenter, (_, pos) => pos))
			.Switch()
			.Select(pos => new Vector3(pos.x, pos.y, transform.position.z))
			.Scan(transform.position, (x, y) => Vector3.Lerp(x, y, lerpPower * Time.fixedDeltaTime))
			.Subscribe(pos => transform.position = pos)
			.AddTo(this);

		var cam = GetComponent<Camera>();
		var origSize = cam.orthographicSize;
				
		lookAtPlayer
			.Select(pl=>pl.TotalSize)
			.Switch()
			.Select(x => origSize * (Mathf.Log(x*.01f+1)*15+1)) //TotalSize игрока изменяется линейно, а камера отдаляется логарифмически
			.Subscribe(x =>
			{
				cam.DOOrthoSize(x, 1f);
			})
			.AddTo(this);

	}
}
