using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UniRx;
using UniRx.Triggers;
using UnityEngine;
using UnityEngine.UI;

public class Joystick : MonoBehaviour
{
	public static BehaviorSubject<Vector2> MoveDirection { get; } = new BehaviorSubject<Vector2>(Vector2.zero);

	[SerializeField] private Image m_joy, m_joyHandle;
	
	void Start ()
	{
		InitJoystick();
	}

	private void InitJoystick()
	{
		var joySz = m_joy.rectTransform.sizeDelta / 2 - m_joyHandle.rectTransform.sizeDelta / 2;
		
		var touchInputObs = m_joy.OnPointerDownAsObservable()
			.SelectMany(pd =>
			{
				return Observable.EveryUpdate()
					.Select(_ => (Vector2) m_joy.transform.InverseTransformPoint(pd.position))
					.Select(pos=>new Vector2(pos.x/joySz.x,pos.y/joySz.y))
					.Select(x=>Vector2.ClampMagnitude(x,1))
					.DistinctUntilChanged()
					.TakeUntil(m_joy.OnPointerUpAsObservable().Where(pd1 => pd1.pointerId == pd.pointerId))
					.Concat(Observable.Return(Vector2.zero));
			})
			.Publish()
			.RefCount();

		//joystick emulation with keyz 4 editor
#if UNITY_EDITOR
		var updObs = Observable.EveryUpdate();
		var keysObs = Observable.Merge(new[]
			{
				updObs.Where(_ => Input.GetKey(KeyCode.UpArrow)).Select(_ => new Vector2(0, 1)),
				updObs.Where(_ => Input.GetKey(KeyCode.DownArrow)).Select(_ => new Vector2(0, -1)),
				updObs.Where(_ => Input.GetKey(KeyCode.LeftArrow)).Select(_ => new Vector2(-1, 0)),
				updObs.Where(_ => Input.GetKey(KeyCode.RightArrow)).Select(_ => new Vector2(1, 0))
			})
			.Publish()
			.RefCount();
#endif

		var inputObs = touchInputObs
#if UNITY_EDITOR
			.Merge(keysObs, keysObs.ThrottleFrame(3).Select(_ => Vector2.zero)) //return to v2zero if no input in past 3 frames
#endif
			.DistinctUntilChanged()
			.Publish()
			.RefCount();

		//movin' joystick handle
		inputObs
			.Select(x => new Vector2(x.x * joySz.x, x.y * joySz.y))
			.Subscribe(x => m_joyHandle.transform.localPosition = x)
			.AddTo(this);

		//publishin' to static subject
		inputObs
			.Subscribe(MoveDirection)
			.AddTo(this);
	}	
}