using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TranslatedText : MonoBehaviour {

    public TranslatedTxt txt;

	// Use this for initialization
	void Start () {
        GetComponent<Text>().text = Translator.Translate(txt);
	}
	
}
