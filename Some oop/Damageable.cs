using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Damageable : MonoBehaviour
{
	public AudioClip hitSound;
	
	public int maxHp = 1;

	[NonSerialized]private int _curHp=1;

	protected int CurHp
	{
		get { return _curHp; }
		set
		{
			if(_curHp<=0) return;
			
			if(hitSound) SoundController.Play(hitSound.name);
			
			_curHp = value;
			if (_curHp <= 0)
				Die();
		}
	}
	
	// Use this for initialization
	protected virtual void Awake ()
	{
		_curHp = maxHp;
	}

	public void GetHit()
	{
		CurHp -= 1;
	}

	protected abstract IDisposable Die();
}
