using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

using VRC.SDK3.Data;

using TMPro;
using UnityEngine.UI; // Button[]

public class actionAmbassador : UdonSharpBehaviour
{
    // for increased readability
    private VRC.Udon.Common.Interfaces.NetworkEventTarget Owner = VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner;

    [Header("other references")]
    public GameObject self;
    public GameObject cardsObject;
    public UdonBehaviour Table;
    public UdonBehaviour PlayerScript;

    [Header("card references")]
    public Sprite[] Sprites;
    private string[] cardNames = new string[] { "Ambassador", "Assassin", "Captain", "Contessa", "Duke" };
    public GameObject cardHolding1;
    public GameObject cardHolding2;

    public UdonBehaviour cardScript1;
    public UdonBehaviour cardScript2;


    [Header("Local card references")]
    public Toggle[] cardToggles;

    [Header("Local Panel references")]
    public GameObject Panel2Life;
    public GameObject Panel1Life;
    private GameObject cardPanel;

    public Button confirm;

    private int singleCard = 0;

    private DataList cards = new DataList();

    [UdonSynced]
    private int deckCard1 = 0;

    [UdonSynced]
    private int deckCard2 = 0;

    void Start()
    {

    }

    public void Ambassador()
    {
        Table.SendCustomNetworkEvent(Owner, "AmbassadorGrab2Cards");
        RequestSerialization();

        cards.Clear();

        cards.Add(deckCard1);
        cards.Add(deckCard2);

        for (int i = 0; i <= 6; i++)
            cardToggles[i].isOn = false;


        if (cardHolding1.activeSelf)
        {
            int test1 = (int)PlayerScript.GetProgramVariable("card1");
            cards.Add(test1);
        }

        if (cardHolding2.activeSelf)
        {
            int test2 = (int)PlayerScript.GetProgramVariable("card2");
            cards.Add(test2);
        }

        if (cardHolding1.activeSelf && !cardHolding2.activeSelf)
        {
            singleCard = 1;
        }
        else if (!cardHolding1.activeSelf && cardHolding2.activeSelf)
        {
            singleCard = 2;
        }

   /*      string test = "";
        for (int i = 0; i < cards.Count; i++)
        {
            if (cards.TryGetValue(i, out DataToken cardlog))
                test += cardlog + ", ";
        }
        Debug.Log("cards: " + test); */

        cardsObject.SetActive(false);
        confirm.interactable = false;
        Panel1Life.SetActive(false);
        Panel2Life.SetActive(false);

        if (cards.Count == 3)
            cardPanel = Panel1Life;
        else if (cards.Count == 4)
            cardPanel = Panel2Life;

        Transform CardsPanel = cardPanel.transform.GetChild(0);

        for (int i = 0; i < CardsPanel.childCount; i++)
        {
            //Debug.Log("currentchild: " + i);

            Transform currentCard = CardsPanel.transform.GetChild(i);

            TextMeshProUGUI cardName = currentCard.GetChild(0).GetComponent<TextMeshProUGUI>();
            Image cardFace = currentCard.GetChild(1).GetComponent<Image>();

            if (cards.TryGetValue(i, out DataToken card))
            {
                //Debug.Log("cardId: " + card);
                cardFace.sprite = Sprites[(int)card];
                cardName.text = cardNames[(int)card];
            }
        }
        cardPanel.SetActive(true);
    }

    public void ConfirmCards()
    {
        DataToken card;

        int keepCard1 = -1;
        int keepCard2 = -1;

        int discardCard1 = -1;
        int discardCard2 = -1;

        if (cards.Count == 4)
        {
            for (int i = 0; i <= 4; i++)
            {
                if (cardToggles[i].isOn)
                {
                    if (cards.TryGetValue(i, out card))
                    {
                        if (keepCard1 == -1)
                            keepCard1 = (int)card;
                        else
                            keepCard2 = (int)card;
                    }
                }
                else
                {
                    if (cards.TryGetValue(i, out card))
                    {
                        if (discardCard1 == -1)
                            discardCard1 = (int)card;
                        else
                            discardCard2 = (int)card;
                    }
                }
            }

            PlayerScript.SetProgramVariable("card1", keepCard1);
            PlayerScript.SetProgramVariable("card2", keepCard2);

            cardScript1.SendCustomNetworkEvent(Owner, "updateCard");
            cardScript2.SendCustomNetworkEvent(Owner, "updateCard");

            cardsObject.SetActive(true);

            Table.SetProgramVariable("ambassadorCard1", discardCard1);
            Table.SetProgramVariable("ambassadorCard2", discardCard2);

            Table.SendCustomNetworkEvent(Owner, "AmbassadorDone");

            RequestSerialization();

            self.SetActive(false);

        }
        else if (cards.Count == 3)
        {
            for (int i = 4; i <= 6; i++)
            {
                if (cardToggles[i].isOn)
                {
                    if (cards.TryGetValue(i - 4, out card))
                        keepCard1 = (int)card;
                }
                else
                {
                    if (cards.TryGetValue(i - 4, out card))
                    {
                        if (discardCard1 == -1)
                            discardCard1 = (int)card;
                        else
                            discardCard2 = (int)card;
                    }
                }
            }

            //get which active card

            if (singleCard == 1)
            {
                PlayerScript.SetProgramVariable("card1", keepCard1);
                cardScript1.SendCustomNetworkEvent(Owner, "updateCard");
            }
            else if (singleCard == 2)
            {
                PlayerScript.SetProgramVariable("card2", keepCard1);
                cardScript2.SendCustomNetworkEvent(Owner, "updateCard");
            }

            cardsObject.SetActive(true);

            Table.SetProgramVariable("ambassadorCard1", discardCard1);
            Table.SetProgramVariable("ambassadorCard2", discardCard2);

            Table.SendCustomNetworkEvent(Owner, "AmbassadorDone");

            RequestSerialization();

            self.SetActive(false);
        }
        Table.SendCustomNetworkEvent(Owner, "NextPlayer");
    }

    private void CardClicked(int card)
    {
        switch (card)
        {
            case 0:
            case 1:
            case 2:
            case 3:
                if (cardToggles[card].isOn == true)
                {
                    bool oneCardActive = false;

                    for (int i = 0; i <= 4; i++)
                    {
                        if (card == i)
                            continue;

                        if (cardToggles[i].isOn == true)
                            oneCardActive = true;
                    }

                    if (oneCardActive)
                    {
                        for (int i = 0; i <= 4; i++)
                        {
                            if (cardToggles[i].isOn == false)
                                cardToggles[i].interactable = false;
                        }

                        confirm.interactable = true;
                    }
                }
                else
                {
                    for (int i = 0; i <= 4; i++)
                        cardToggles[i].interactable = true;

                    confirm.interactable = false;
                }
                break;

            case 4:
            case 5:
            case 6:
                if (cardToggles[card].isOn == true)
                {
                    for (int i = 4; i <= 6; i++)
                    {
                        if (cardToggles[i].isOn == false)
                            cardToggles[i].interactable = false;
                    }
                    confirm.interactable = true;
                }
                else
                {
                    for (int i = 4; i <= 6; i++)
                        cardToggles[i].interactable = true;
                    confirm.interactable = false;
                }
                break;
        }
    }

    public void CardClick1() { CardClicked(0); }
    public void CardClick2() { CardClicked(1); }
    public void CardClick3() { CardClicked(2); }
    public void CardClick4() { CardClicked(3); }
    public void CardClick5() { CardClicked(4); }
    public void CardClick6() { CardClicked(5); }
    public void CardClick7() { CardClicked(6); }
}