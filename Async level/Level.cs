/*
��� ����� ����������:
���� N ����� ����, ����� ��������� � ������ �����

����� ����� �������� �� ��������� �����, ������ ��������� �����
��������� ������ ���� ���� ������, ����� ������������ ������

� ��������� ����� ��������� ����, ����� �� ����� - �������
*/

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UniRx;
using UnityEngine;
using Random = UnityEngine.Random;

public class Level : MonoBehaviour
{
	public static Level Current { get; private set; }
	
	public Transform startTr,
		endTr,
		playerHideoutsParent,
		mobshideoutsParent;
	
	public List<Transform> OccupiedHideouts { get; } = new List<Transform>();

	public int FrontierLvl { get; private set; } = 0;

	private void OnEnable()
	{
		Current = this;
	}

	// Use this for initialization
	void Start ()
	{
		//teleportin' player to start
		var plr = GameController.CurrentPlayer;
		plr.Agent.Warp(startTr.position);
		GameCamera.Instance.transform.position = plr.cameraTarget.position;

		var path = playerHideoutsParent.ChildsEnumerated().Select(x => x.item)
			.GetEnumerator();

		Observable.ReturnUnit()
			.Select(_ => path.MoveNext())
			.SelectMany(x =>
			{
				if (!x) return Observable.Return(false);

				FrontierLvl++;
				
				plr.Agent.SetDestination(path.Current.position);

				return Observable.Timer(TimeSpan.FromSeconds(2))
					.AsUnitObservable()
					.Concat(plr.ReachedDestination)
					.Last()
					.Select(_ => true);
			})
			.SelectMany(x=>x?SpawnBotsAndWaitThemDie().Select(_=>true):Observable.Return(false))
			.Do(_=>plr.Target.Value=null)
			.RepeatSafe() //��������� ��� ����� ����
			.TakeWhile(x => x) //���� ���������� => onComplete
			.Last() //���� �� ��������� ����� ��������
			.AsUnitObservable()
			.SelectMany(_ =>
			{
				FrontierLvl += 5;
				return SpawnBossAndWaitHimDie().Delay(TimeSpan.FromSeconds(2)); //������� ����� � ���� ���� �������
			})
			.TakeUntilDestroy(this)
			.Subscribe(_ =>
			{
				GameController.Instance.Win(); //������ ������ ������ �����
			},() => path?.Dispose());
	}
	
	public IObservable<Unit> SpawnBotsAndWaitThemDie()
	{
		var plr = GameController.CurrentPlayer;
        var dir = endTr.position - startTr.position;
		
		OccupiedHideouts.Clear();
		
		var res = GameController.Instance.mobsSpawnsParent
			.ChildsEnumerated()
			.Select(x => x.item.position)
			.Where(x => Vector3.Dot(dir, x - plr.transform.position) > 0) //����� ����������� �� ����������� ��������
			.ToList();
		
		res = res.Where(x=>(x - plr.transform.position).magnitude < 70) //� �������� �� ������
			.OrderBy(_ => Random.value)
			.Take(Random.Range(4,6+1))
			.ToList();
			
		return res.Select(sp =>
			{
				return GameController.Instance.mobPrefabs
					.OrderBy(_ => Random.value)
					.First()
					.InstantiateMe(sp, Quaternion.identity); //������� ���� � ����� � �������� ��� ������
			})
			.ToObservable()
			.Do(GameController.Instance.AddHpSliderFor) //��������� ����������� �� ��� ����
			.SelectMany(mob => mob.IsDead.Where(x => x).AsUnitObservable().Take(1)) //������� ���� ��� �������
			.Last(); //����� ��������� ��� �������
	}
	
	public IObservable<Unit> SpawnBossAndWaitHimDie()
	{
		OccupiedHideouts.Clear();
		
		var baws = GameController.Instance.bossesPrefabs
			.OrderBy(_ => Random.value)
			.First()
			.InstantiateMe(endTr.position, Quaternion.identity); //������� ����� � �����
		
		GameController.Instance.AddHpSliderFor(baws);

		return baws.IsDead.Where(x => x).AsUnitObservable().Take(1);
	}
	
	
	
}
