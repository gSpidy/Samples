using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.Text.RegularExpressions;

[Serializable]
public class TranslatedTxt{
    public string text;

    public string Translated {
        get {
            return Translator.Translate(text);
        }
    }

    public static implicit operator string(TranslatedTxt v) {
        return v.text;
    }
}

[CreateAssetMenu(fileName = "NewTranslatorConfig", menuName = "GameConfigs/Base/TranslatorConfig", order = 1)]
public class Translator : ScriptableObject {
    static Translator _instance;
    public static Translator Current {
        get {
            if (!_instance) {
                _instance = Resources.Load<Translator>("TranslatorConfig");
                _instance.initialize();
            }
            return _instance;
        }
    }      

    [Serializable]
    public struct langFile {
        public TextAsset File;
        public SystemLanguage Lang;
    }
    
    public List<langFile> TranslationFiles;


    public Dictionary<string, string> Translations = new Dictionary<string, string>();

    void initialize() {
        LoadTranslation(Application.systemLanguage);
    }    

    public void LoadTranslation(SystemLanguage lang) {
        var idx = TranslationFiles.FindIndex(x => x.Lang == lang);
        if (idx >= 0) LoadTranslation(idx); else LoadTranslation(0);
    }

    public void LoadTranslation(int idx) {
        if (idx >= TranslationFiles.Count) return;

        Translations.Clear();

        var txt = Regex.Replace(TranslationFiles[idx].File.text, "/\\*(?:.|[\\n\\r])*?\\*/", "");
        txt = Regex.Replace(txt, @"\/\/.+[\r\n]", "");        

        txt.Split(new char[] { ';' }, System.StringSplitOptions.RemoveEmptyEntries).ToList()
            .ForEach(s => {
                int qcount = 0;
                int fnd = -1;

                for(var i=0; i<s.Length; i++) {
                    if (s[i] == '"') qcount++;
                    if(s[i]=='=' && qcount%2==0) { fnd = i; break; }
                }

                if (fnd >= 0) {
                    Translations[s.Substring(0, fnd).Trim().Replace("\"","")] = s.Substring(fnd + 1).Trim().Replace("\"", "");
                }                                
            });
                
    }
    
    public static string Translate(string key) {
        return Current.Translations[key];
    }


}
