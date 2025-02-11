using TMPro;
using UnityEngine;

public class FillWord : MonoBehaviour
{
    public bool filled = false;
    public TextMeshProUGUI text;
    public CanvasGroup filledBg;
    public GameObject fillingHint;
    // Start is called before the first frame update
    public void init(string name="")
    {
        this.gameObject.name = name;
        this.filled = false;
        this.SetContent("");
    }

    public void SetContent(string _word)
    {
       this.filled = string.IsNullOrEmpty(_word) ? false : true;
       if (this.text != null)
       {
          this.text.text = _word;
       }
       SetUI.Set(this.filledBg, this.filled);
    }

    public void SetHint(bool status)
    {
        //SetUI.Set(this.fillingHint, status);
        if(this.fillingHint != null)
        {
            this.fillingHint.SetActive(status);
        }
    }
}
