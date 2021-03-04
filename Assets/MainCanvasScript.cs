using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MainCanvasScript : MonoBehaviour
{
    public InputField dirField;
    public GameObject metaCrawl;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void GoButtonClick()
    {
        MetaCrawlerScript meta = metaCrawl.GetComponent<MetaCrawlerScript>();
        meta.Crawl(dirField.text);
    }
}
