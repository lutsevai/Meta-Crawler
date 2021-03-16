using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MainCanvasScript : MonoBehaviour
{
    public InputField inDirField;
    public InputField outDirField;

    const string outDir_standard = @"D:\documents\data\meta-two\rt\";

    // Start is called before the first frame update
    void Start()
    {
        outDirField.text = outDir_standard;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void GoButtonClick()
    {
        MetaCrawler meta = new MetaCrawler();
        meta.Crawl(inDirField.text, outDirField.text);
    }
}
