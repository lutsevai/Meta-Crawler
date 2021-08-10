using UnityEngine;
using UnityEngine.UI;



public class MainCanvasScript : MonoBehaviour
{
    public InputField inDirField;
    public InputField outDirField;


    // Start is called before the first frame update
    void Start()
    {
        outDirField.text = Settings.outDir_default;
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
