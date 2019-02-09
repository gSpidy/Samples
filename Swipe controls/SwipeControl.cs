using System;
using System.Collections;
using System.Collections.Generic;
using UniRx;
using UniRx.Triggers;
using UnityEngine;
using UnityEngine.UI;

namespace Swipe
{
	public enum Dir
	{
		up,
		down,
		left,
		right
	}

	public class SwipeControl : MonoBehaviourSingleton<SwipeControl>
	{
		public static IObservable<Dir> Direction { get; private set; }
		
		private Image img;
		
		void Awake()
		{
			img = GetComponent<Image>();
			var inst = Instance;
			
			var updobservable = Observable.EveryUpdate();
			
#if UNITY_EDITOR
			var kdobs = Observable.Merge(
				updobservable.Where(_=>Input.GetKeyDown(KeyCode.UpArrow)).Select(_=>Dir.up),
				updobservable.Where(_=>Input.GetKeyDown(KeyCode.DownArrow)).Select(_=>Dir.down),
				updobservable.Where(_=>Input.GetKeyDown(KeyCode.RightArrow)).Select(_=>Dir.right),
				updobservable.Where(_=>Input.GetKeyDown(KeyCode.LeftArrow)).Select(_=>Dir.left)
			);
#endif


			var obs = img.OnPointerDownAsObservable()
				.TakeUntilDestroy(this)
				.SelectMany(p =>
				{
					var ppos = p.position;
					return img.OnPointerUpAsObservable().Where(p1 => p.pointerId == p1.pointerId).Take(1)
						.Select(p1 => p1.position - ppos);
				})
				.Where(v => v.magnitude > 50)
				.Select(v => v.normalized)
				.Select(v =>
				{
					var fw = Vector3.Project(v, Vector3.up);
					var rg = Vector3.Project(v, Vector3.right);

					return fw.magnitude > rg.magnitude
						? (fw.y >= 0 ? Dir.up : Dir.down)
						: (rg.x >= 0 ? Dir.right : Dir.left);
				})
#if UNITY_EDITOR
				.Merge(kdobs)
#endif
				.Publish();

			Direction = obs;
			obs.Connect().AddTo(this);
		}
	}
}
