using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

using TMPro; // TextMeshProUGUI
using UnityEngine.UI; // Button[]

public class playerScript : UdonSharpBehaviour
{
    [Header("Objects that happens only for owner of this GameObject")]
    //stupid 'this' reference for networking function

    [Header("UdonBehaviour Script References")]
    public GameObject self;
    public UdonBehaviour parent; //table
    public UdonBehaviour ambassadorPanel; //table

    // for increased readability
    private VRC.Udon.Common.Interfaces.NetworkEventTarget Owner = VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner;
    private VRC.Udon.Common.Interfaces.NetworkEventTarget All = VRC.Udon.Common.Interfaces.NetworkEventTarget.All;

    [UdonSynced]
    [HideInInspector] public int playerID;

    [UdonSynced]
    [HideInInspector] public int PanelID;

    [UdonSynced, FieldChangeCallback(nameof(playerNameChange))] //local changes
    [HideInInspector] private string playerName; // global changes

    [UdonSynced, FieldChangeCallback(nameof(playerIskChange))] //local changes
    public int playerIsk; // global changes

    [UdonSynced, FieldChangeCallback(nameof(claimedCardChange))] //local changes
    private int claimedCard; // global changes

    [UdonSynced, FieldChangeCallback(nameof(oldCardChange))] //local changes
    private int oldCard; // global changes

    [UdonSynced, FieldChangeCallback(nameof(newCardChange))] //local changes
    private int newCard; // global changes

    [UdonSynced, FieldChangeCallback(nameof(cardChange1))] //local changes
    private int card1; // global changes
    public GameObject cardFace1;

    [UdonSynced, FieldChangeCallback(nameof(cardChange2))] //local changes
    private int card2; // global changes
    public GameObject cardFace2;

    [Header("Player GameObjects")]
    public GameObject playerInfo;
    public GameObject JoinLeave;

    public GameObject gamePanel;

    [Header("Player turn Objects (for initializing)")]
    public GameObject playerTurnObject;
    public GameObject playerActions;
    public GameObject playerPickPlayer;

    public GameObject extraPanels;
    public GameObject playedAmbassador;

    private Transform playerJoin;
    private Transform playerLeave;

    [Header("Text Objects")]
    public TextMeshProUGUI playerNameIn;
    public TextMeshProUGUI playerNameOut;
    public TextMeshProUGUI playerCoinsIn;
    public TextMeshProUGUI playerCoinsOut;

    void Start()
    {
        playerTurnObject.SetActive(false);
        JoinLeave.SetActive(true);

        if (JoinLeave != null)
        {
            playerJoin = JoinLeave.transform.GetChild(0);
            playerLeave = JoinLeave.transform.GetChild(1);
        }
    }

    //networked owner
    public void playerTurn()
    {
        playerTurnObject.SetActive(true);

        playerActions.SetActive(true);
        playerPickPlayer.SetActive(false);
        playedAmbassador.SetActive(false);
        extraPanels.SetActive(false);
    }

    //networked owner
    public void Reset()
    {
        playerTurnObject.SetActive(false);
        playerActions.SetActive(false);
        playerPickPlayer.SetActive(false);
        playedAmbassador.SetActive(false);
        extraPanels.SetActive(false);
        cardFace1.SetActive(false);
        cardFace2.SetActive(false);
    }

    //networked owner
    public void PlayerTurnAmbassador()
    {
        playerTurnObject.SetActive(true);

        playerActions.SetActive(false);
        playerPickPlayer.SetActive(false);
        extraPanels.SetActive(true);

        ambassadorPanel.SendCustomNetworkEvent(Owner, "Ambassador");
        playedAmbassador.SetActive(true);
    }

    //networked Owner: assign cards, then reveal if player is owner
    public void revealCards()
    {
        cardFace1.SetActive(true);
        cardFace2.SetActive(true);

        // 2 calls, reduce to 1 please
        cardFace1.GetComponent<UdonBehaviour>().SendCustomNetworkEvent(Owner, "updateCard");
        cardFace2.GetComponent<UdonBehaviour>().SendCustomNetworkEvent(Owner, "updateCard");
    }

    public void PlayerDrawNewCard()
    {
        // claimedCard 1|2

        if (claimedCard == 1)
        {
            cardChange1 = newCard;
            oldCardChange = card1;
            cardFace1.GetComponent<UdonBehaviour>().SendCustomNetworkEvent(Owner, "updateCard");
        }

        if (claimedCard == 2)
        {
            cardChange2 = newCard;
            oldCardChange = card2;
            cardFace2.GetComponent<UdonBehaviour>().SendCustomNetworkEvent(Owner, "updateCard");
        }

    }

    //networked owner
    public void join()
    {
        var player = Networking.LocalPlayer;

        Networking.SetOwner(player, self);

        playerID = player.playerId;

        playerNameChange = player.displayName;

        // sync variable for everyone
        RequestSerialization();

        SendCustomNetworkEvent(All, "PlayerJoin");
    }

    //network all
    public void PlayerJoin()
    {
        var player = Networking.LocalPlayer;

        playerJoin.gameObject.SetActive(false);
        if (player.IsOwner(self))
            playerLeave.gameObject.SetActive(true);
    }

    //networked owner
    public void leave()
    {
        playerNameChange = "";
        playerID = 0;

        RequestSerialization();

        SendCustomNetworkEvent(All, "PlayerLeave");
    }

    //networked all
    public void PlayerLeave()
    {
        playerJoin.gameObject.SetActive(true);
        playerLeave.gameObject.SetActive(false);
    }



    //-------- !onchange callbacks below ! -------------------------------------------------------  !onchange callbacks below !  ----------------

    //onChange playerNameChange
    public string playerNameChange
    {
        set
        {
            playerNameIn.text = value;
            playerNameOut.text = value;

            playerName = value;
        }
        get => playerName;
    }

    //onChange playerNameChange
    public int cardChange1
    {
        set
        {
            card1 = value;
            RequestSerialization();
        }
        get => card1;
    }

    //onChange playerNameChange
    public int cardChange2
    {
        set
        {
            card2 = value;
            RequestSerialization();
        }
        get => card2;
    }

    public int playerIskChange
    {
        set
        {
            playerIsk = value;
            RequestSerialization();

            playerCoinsIn.text = value.ToString();
            playerCoinsOut.text = value.ToString();
        }
        get => playerIsk;
    }

    public int claimedCardChange
    {
        set
        {
            claimedCard = value;
            RequestSerialization();
        }
        get => claimedCard;
    }

    public int oldCardChange
    {
        set
        {
            oldCard = value;
            RequestSerialization();
        }
        get => oldCard;
    }

    public int newCardChange
    {
        set
        {
            newCard = value;
            RequestSerialization();
        }
        get => newCard;
    }

}
