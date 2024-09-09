using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

using TMPro;

public class cardScript : UdonSharpBehaviour
{
    public int id;
    public Texture Ambassador, Assassin, Captain, Contessa, Duke;
    public Renderer m_Renderer;
    public UdonBehaviour parent;

    public TextMeshProUGUI cardNameText;

    private int card;

    void Start() { }

    public void updateCard()
    {
        if (id == 1)
            card = (int)parent.GetProgramVariable("card1");
        else if (id == 2)
            card = (int)parent.GetProgramVariable("card2");

        switch (card)
        {
            case 0:
                m_Renderer.material.SetTexture("_MainTex", Ambassador);
                cardNameText.text = "Ambassador";
                break;
            case 1:
                m_Renderer.material.SetTexture("_MainTex", Assassin);
                cardNameText.text = "Assassin";
                break;
            case 2:
                m_Renderer.material.SetTexture("_MainTex", Captain);
                cardNameText.text = "Captain";
                break;
            case 3:
                m_Renderer.material.SetTexture("_MainTex", Contessa);
                cardNameText.text = "Contessa";
                break;
            case 4:
                m_Renderer.material.SetTexture("_MainTex", Duke);
                cardNameText.text = "Duke";
                break;
            default:
            Debug.LogError("CardScript ERROR");
                break;
        }


    }
}
