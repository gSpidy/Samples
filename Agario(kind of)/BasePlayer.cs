using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UniRx;
using UniRx.Triggers;
using UnityEngine;

public class BasePlayer : MonoBehaviour
{
	public string playerName;
	public Sprite skinSprite;
	public Color coolorTint = Color.white;

	protected ReactiveCollection<Blob> mahBlobs = new ReactiveCollection<Blob>();
	
	//центр всех частей игрока, вычисляется один раз за кадр
	private IObservable<Vector2> _observableCenter;
	public IObservable<Vector2> ObservableCenter => _observableCenter ?? (_observableCenter =
											mahBlobs.ObserveCountChanged(true) //при изменении количества частей
												.Select(_ => mahBlobs
													.Where(x=>x) //где часть не уничтожена
													.Select(x => Observable.EveryFixedUpdate()
														.Select(__ => (Vector2) x.transform.position)
														.StartWith((Vector2) x.transform.position)
														.DistinctUntilChanged())
													.CombineLatest()
													.SampleFrame(1,FrameCountType.FixedUpdate) //только 1 раз в кадр
													.Select(x => x.Aggregate((v1, v2) => v1 + v2) / x.Count))
												.Switch()
												.Replay(1)
												.RefCount());
	
	private IObservable<int> _totalSize;

	public IObservable<int> TotalSize => _totalSize ?? (_totalSize =
		                                     mahBlobs.ObserveCountChanged(true)
			                                     .Select(_ => mahBlobs
				                                     .Where(x=>x)
				                                     .Select(x => x.SizeObs.TakeUntilDestroy(x))
				                                     .Concat(new []{Observable.Return(0)})
				                                     .CombineLatest()
				                                     .Select(x =>
				                                     {
					                                     return x.Sum();
				                                     }))
			                                     .Switch()
			                                     .DistinctUntilChanged()
			                                     .Replay(1)
			                                     .RefCount());
	
	public Vector2ReactiveProperty MoveTarget = new Vector2ReactiveProperty();


	// Use this for initialization
	protected virtual void Awake ()
	{
		ObservableCenter.Subscribe(pos => transform.position = pos).AddTo(this);

		//TotalSize игрока изменяется линейно, а масштаб меняется логарифмически, чтобы с увеличением totalsize игрок рос всё медленнее и медленее
		TotalSize
			.Select(x => Vector3.one * (Mathf.Log(x*.01f+1)*20+1))          //(1f + Mathf.Log(1f + .1f * x)))
			.Subscribe(x => transform.localScale = x)
			.AddTo(this);
	}

	public void ClearBlobs()
	{
		mahBlobs.ToList().ForEach(x=>Destroy(x.gameObject));
		mahBlobs.Clear();	
	}
	
	public virtual void Respawn()
	{
		var blob = GameController.Instance.blobPrefab.InstantiateMe(GameController.Instance.RandomPosInBounds(),
			Quaternion.identity);

		blob.Sprite = skinSprite;
		blob.Tint = coolorTint;
		blob.Size = 2;
		
		blob.moveTargetRef = MoveTarget;
		
		mahBlobs.Add(blob);
		
		blob.OnDestroyAsObservable().Take(1)
			.Subscribe(_ => mahBlobs.Remove(blob))
			.AddTo(this);
	}
	
	protected async void ChangeDirection(Vector2 dir)
	{
		var c = await ObservableCenter.First();
		var sz = await TotalSize.First();
		
		MoveTarget.Value = c + dir * (float)sz/4;
	}

	public async void SplitAll()
	{
		if(await TotalSize.First()<10) return;

		mahBlobs
			.ToList()
			.ToObservable()
			.Zip(Observable.IntervalFrame(1), (x, _) => x)
			.Where(x=>x)
			.SelectMany(x =>
			{
				var blob = x.Split();
				blob.moveTargetRef = MoveTarget;
				mahBlobs.Add(blob);
				
				return blob.OnDestroyAsObservable()
					.Take(1)
					.Select(_=>blob);
			})
			.Subscribe(x=>mahBlobs.Remove(x))
			.AddTo(this);
	}
}
