using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

using UnityEngine.UI; // Button[]
using TMPro; // TextMeshProUGUI

public class PlayerChallengedScript : UdonSharpBehaviour
{
    public GameObject Self;
    public GameObject PlayerCards;
    public UdonBehaviour PlayerScript;
    public UdonBehaviour Table;

    [Header("discard card references")]
    public GameObject cardHolding1;
    public GameObject cardHolding2;

    // for increased readability
    private VRC.Udon.Common.Interfaces.NetworkEventTarget Owner = VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner;
    private VRC.Udon.Common.Interfaces.NetworkEventTarget All = VRC.Udon.Common.Interfaces.NetworkEventTarget.All;

    public GameObject PlayerDiscard;
    public GameObject PlayerReveal;
    public GameObject playerAssassinated;
    public GameObject PlayerCoupObject;
    public GameObject Player2HP;
    public GameObject Player1HP;

    [UdonSynced, FieldChangeCallback(nameof(ClaimedCardChange))] //local changes
    private int claimedCard; // global changes

    [UdonSynced, FieldChangeCallback(nameof(ForcedDiscardChange))] //local changes
    private int forcedDiscard; // global changes

    [Header("Claimed card references")]
    public TextMeshProUGUI claimedCardText;
    public Renderer claimedCardRenderer;
    public Texture[] MeshTextures;

    [Header("Card 1 references")]
    public Image cardFace1;
    public Image cardMark1;
    public Toggle cardToggle1;
    private int card1;
    public TextMeshProUGUI card1Name;

    [Header("Card 2 references")]
    public Image cardFace2;
    public Image cardMark2;
    public Toggle cardToggle2;
    private int card2;
    public TextMeshProUGUI card2Name;

    [Header("Card One Life references")]
    public Image cardFaceOneLife;
    public Image cardMarkOneLife;
    public Toggle cardToggleOneLife;
    private int cardOneLife;
    public TextMeshProUGUI cardOneLifeName;

    public Sprite[] cardFaces;
    public Sprite[] marks;

    [Header("Button References")]
    public Button confirm;

    private bool loseTwoCards = false;
    private bool loseTwoCardsDone = false;

    private string[] cardName = new string[] { "Ambassador", "Assassin", "Captain", "Contessa", "Duke" };

    // networked owner: start
    public void PlayerChallenged()
    {
        int playerID = (int)PlayerScript.GetProgramVariable("PanelID");
        int[] playerHPList = (int[])Table.GetProgramVariable("playerHPList");
        int hp = playerHPList[playerID];

        cardToggle1.isOn = false;
        cardToggle2.isOn = false;
        cardToggleOneLife.isOn = false;

        if (hp == 2)
        {
            card1 = (int)PlayerScript.GetProgramVariable("card1");
            card2 = (int)PlayerScript.GetProgramVariable("card2");

            cardFace1.sprite = cardFaces[card1];
            cardFace2.sprite = cardFaces[card2];

            card1Name.text = cardName[card1];
            card2Name.text = cardName[card2];
        }
        else if (hp == 1)
        {
            if (cardHolding1.activeSelf)
                cardOneLife = (int)PlayerScript.GetProgramVariable("card1");
            else if (cardHolding2.activeSelf)
                cardOneLife = (int)PlayerScript.GetProgramVariable("card2");

            cardFaceOneLife.sprite = cardFaces[cardOneLife];
            cardOneLifeName.text = cardName[cardOneLife];
        }

        Player2HP.SetActive(hp == 2);
        Player1HP.SetActive(hp == 1);

        LoseCardSettings(forcedDiscard);

        PlayerCards.SetActive(false);
        Self.SetActive(true);
    }

    // leaving this script
    public void ConfirmCard()
    {
        if (forcedDiscard == 0)
        {
            int selectedCard = 0;

            if (cardToggle1.isOn == true)
                selectedCard = card1;
            else if (cardToggle2.isOn == true)
                selectedCard = card2;
            else if (cardToggleOneLife.isOn == true)
                selectedCard = cardOneLife;

            if (selectedCard == claimedCard)
            {
                if (cardToggle1.isOn == true)
                    PlayerScript.SetProgramVariable("claimedCard", 1);
                if (cardToggle2.isOn == true)
                    PlayerScript.SetProgramVariable("claimedCard", 2);

                Table.SendCustomNetworkEvent(Owner, "ChallengeSuccess");

                PlayerCards.SetActive(true);
                Self.SetActive(false);
            }
            else
            {
                PlayerLoseCard();
            }
        }
        else
        {
            PlayerLoseCard();
        }
    }

    private void PlayerLoseCard()
    {
        int playerID = (int)PlayerScript.GetProgramVariable("PanelID");
        int[] playerHPList = (int[])Table.GetProgramVariable("playerHPList");
        int hp = playerHPList[playerID];

        hp--;
        playerHPList[playerID] = hp;

        if (hp == 0)
        {
            Table.SetProgramVariable("playerDeadId", (int)PlayerScript.GetProgramVariable("PanelID"));
            Table.SendCustomNetworkEvent(Owner, "PlayerDead");
        }

        Table.SetProgramVariable("playerHPList", playerHPList);

        // BUG contessa block into called bluff leads into triple kill

        bool playedAssassin = (int)Table.GetProgramVariable("playerActionPlayed") == 5;

        // player lose card because of assassin plays
        if (playedAssassin)
        {
            if (loseTwoCards == false)
            {
                bool claimedContessa = (int)Table.GetProgramVariable("claimedCard") == 3;
                bool calledBluff = (bool)Table.GetProgramVariable("playerCalledBluff");
                bool challengeSuccess = (bool)Table.GetProgramVariable("challengeSuccess");

                // player attempts assassinate, gets challenged, does not have assassin
                if (!claimedContessa && calledBluff && !challengeSuccess)
                {
                    int isk = (int)PlayerScript.GetProgramVariable("playerIsk");
                    isk = isk + 3;

                    PlayerScript.SetProgramVariable("playerIsk", isk);
                }

                // players pass on an assassination, targeted player get assassinated
                // player claims contessa, gets challenged, has card
                else if ((!claimedContessa && !calledBluff && !challengeSuccess) ||
                         (claimedContessa && calledBluff && challengeSuccess))
                {
                    Debug.Log("losetwo = false");
                    loseTwoCards = false;
                }

                // player claims assassin, gets challenged, player has card (instakill)
                // player claims contessa, gets challenged, does not have card (instakill)
                else if ((!claimedContessa && calledBluff && challengeSuccess) ||
                         (claimedContessa && calledBluff && !challengeSuccess))
                {
                    Debug.Log("losetwo = true");
                    loseTwoCards = true;
                }
            }
            else
            {
                loseTwoCards = false;
            }
        }

        SendCustomNetworkEvent(All, "DiscardCard");

        if (loseTwoCards == true && loseTwoCardsDone == false)
        {
            loseTwoCardsDone = true;
            // player fails a challenge, proceed to assassinate
            ForcedDiscardChange = 2;
            PlayerChallenged();

            Debug.Log("assasinate continue");
        }
        else
        {
            loseTwoCards = false;
            Debug.Log("assasinate end");
            Table.SendCustomNetworkEvent(Owner, "ChallengeContinue");

            PlayerCards.SetActive(true);
            Self.SetActive(false);
        }
    }

    //networked All
    public void DiscardCard()
    {
        if (cardToggle1.isOn == true)
            cardHolding1.SetActive(false);
        else if (cardToggle2.isOn == true)
            cardHolding2.SetActive(false);
        else if (cardToggleOneLife.isOn == true)
        {
            cardHolding1.SetActive(false);
            cardHolding2.SetActive(false);
        }
    }



    // card and confirm button validation functions

    public void CardClick1()
    {
        if (cardToggle1.isOn == true)
            if (cardToggle2.isOn == true)
                cardToggle2.isOn = false;
        confirmActive();
    }
    public void CardClick2()
    {
        if (cardToggle2.isOn == true)
            if (cardToggle1.isOn == true)
                cardToggle1.isOn = false;

        confirmActive();
    }
    public void CardClickOneLife()
    {
        confirm.interactable = cardToggleOneLife.isOn;
    }
    private void confirmActive()
    {
        bool onlyOneActive = (cardToggle1.isOn == true && cardToggle2.isOn == false) || (cardToggle1.isOn == false && cardToggle2.isOn == true);
        confirm.interactable = onlyOneActive;
    }



    // display correct text and settings

    private void LoseCardSettings(int setting)
    {
        PlayerReveal.SetActive(setting == 0);
        PlayerDiscard.SetActive(setting == 1);
        playerAssassinated.SetActive(setting == 2);
        PlayerCoupObject.SetActive(setting == 3);

        if (setting == 0)
        {
            claimedCardRenderer.material.SetTexture("_MainTex", MeshTextures[claimedCard]);
            claimedCardText.text = cardName[claimedCard];

            cardMark1.sprite = marks[claimedCard == card1 ? 1 : 0];
            cardMark2.sprite = marks[claimedCard == card2 ? 1 : 0];
            cardMarkOneLife.sprite = marks[claimedCard == cardOneLife ? 1 : 0];
        }
        else
        {
            cardMark1.sprite = marks[0];
            cardMark2.sprite = marks[0];
        }

    }

    public int ClaimedCardChange
    {
        set
        {
            claimedCard = value;
            RequestSerialization();
        }
        get => claimedCard;
    }

    public int ForcedDiscardChange
    {
        set
        {
            forcedDiscard = value;
            RequestSerialization();
        }
        get => forcedDiscard;
    }
}