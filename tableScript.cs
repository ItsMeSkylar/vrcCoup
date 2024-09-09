
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using VRC.SDK3.Data;

public class tableScript : UdonSharpBehaviour
{
    [Header("UdonBehaviour Script References")]
    public GameObject startButton;
    public GameObject players;

    public UdonBehaviour gameAnnouncement;
    public UdonBehaviour playerIndicators;

    // directref fast, GetComponent\<T>() slow
    [Header("Objects that must happen for everyone")]
    public UdonBehaviour[] playerScripts;
    public GameObject[] joinLeaveGameObject;
    public GameObject[] cardsGameObject;
    public GameObject[] gamePanel;
    public UdonBehaviour[] CounterActions;
    public UdonBehaviour[] PlayerChallenged;
    public UdonBehaviour[] ActionAmbassador;
    public GameObject[] playerChallengedObject;

    // for increased readability
    private VRC.Udon.Common.Interfaces.NetworkEventTarget Owner = VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner;
    private VRC.Udon.Common.Interfaces.NetworkEventTarget All = VRC.Udon.Common.Interfaces.NetworkEventTarget.All;

    private int playerPanelTotalCount;

    private int currentPlayer = 0;

    [UdonSynced, FieldChangeCallback(nameof(playersLeftChange))] //local changes
    private int playersLeft = 0; // global changes

    [UdonSynced, FieldChangeCallback(nameof(playerSkipCountChange))]
    private int playerSkipCount;

    [UdonSynced, FieldChangeCallback(nameof(playerChallengePanelIDChange))]
    private int playerChallengePanelID;

    [UdonSynced, FieldChangeCallback(nameof(playerClaimIDChange))]
    private int playerClaimID;

    [UdonSynced, FieldChangeCallback(nameof(playerActionPlayedChange))]
    private int playerActionPlayed = 0;

    private bool playerActionBlocked = false;

    [UdonSynced, FieldChangeCallback(nameof(playerSelectedChange))]
    private int playerSelected = 0;
    private bool playerIsSelected = false;

    [UdonSynced, FieldChangeCallback(nameof(challengeSuccessChange))]
    private bool challengeSuccess = false;

    [UdonSynced, FieldChangeCallback(nameof(playerCalledBluffChange))]
    private bool playerCalledBluff = false;

    [UdonSynced, FieldChangeCallback(nameof(claimedCardChange))]
    private int claimedCard = 0;

    [UdonSynced, FieldChangeCallback(nameof(playerDeadIdChange))]
    private int playerDeadId = 0;

    [UdonSynced]
    private int ambassadorCard1 = 0;
    [UdonSynced]
    private int ambassadorCard2 = 0;

    private string[] cardName = new string[] { "Ambassador", "Assassin", "Captain", "Contessa", "Duke" };
    private int[] cardID = new int[] { 0, 1, 2, 3, 4 };

    private DataList deck = new DataList();

    // player variables

    [UdonSynced, FieldChangeCallback(nameof(playerNameListChange))] //local changes
    private string[] playerNameList = new string[] { "", "", "", "", "", "" }; // global changes

    // variables for quickly getting info for selecting players
    [UdonSynced, FieldChangeCallback(nameof(playerIsAliveListChange))] //local changes
    private bool[] playerIsAliveList = new bool[] { false, false, false, false, false, false }; // global changes

    private int[] playerHPList = new int[6] { 0, 0, 0, 0, 0, 0 };

    // testing only
    private string LogDeck()
    {
        string ret = "";

        for (int i = 0; i < deck.Count; i++)
            if (deck.TryGetValue(i, out DataToken card))
                ret = ret + card + ", ";

        return ret;
    }

    // init
    void Start()
    {
        if (startButton == null)
            Debug.LogWarning("startButton is not assigned");
        if (players == null)
            Debug.LogWarning("players is not assigned");

        playerPanelTotalCount = players.transform.childCount;
        playerSkipCount = 0;
        var unassigned = false;

        for (int i = 0; i < playerPanelTotalCount; i++)
        {
            if (playerScripts[i] == null)
            {
                unassigned = true;

                //Table.players.player[i].UdonBehaviour
                playerScripts[i] = this.gameObject.transform.GetChild(1).GetChild(i).GetComponent<UdonBehaviour>();
            }
            playerScripts[i].SetProgramVariable("PanelID", i);

            if (joinLeaveGameObject[i] == null)
            {
                unassigned = true;

                // Table.players.player[i].JoinLeave
                joinLeaveGameObject[i] = this.gameObject.transform.GetChild(1).GetChild(i).GetChild(2).gameObject;
            }

            if (cardsGameObject[i] == null)
            {
                unassigned = true;
                // Table.players.player[i].GamePanel.Cards
                cardsGameObject[i] = this.gameObject.transform.GetChild(1).GetChild(i).GetChild(1).GetChild(0).gameObject;
            }

            if (gamePanel[i] == null)
            {
                unassigned = true;
                // Table.players.player[i].GamePanel
                gamePanel[i] = this.gameObject.transform.GetChild(1).GetChild(i).GetChild(1).gameObject;
            }

            if (CounterActions[i] == null)
            {
                unassigned = true;
                // Table.players.player[i].GamePanel.PlayerCounterActions
                CounterActions[i] = this.gameObject.transform.GetChild(1).GetChild(i).GetChild(1).GetChild(3).GetComponent<UdonBehaviour>();
            }

            if (PlayerChallenged[i] == null)
            {
                unassigned = true;
                // Table.players.player[i].GamePanel.PlayerChallenged
                PlayerChallenged[i] = this.gameObject.transform.GetChild(1).GetChild(i).GetChild(1).GetChild(4).GetComponent<UdonBehaviour>();
            }

            if (playerChallengedObject[i] == null)
            {
                unassigned = true;
                // Table.players.player[i].GamePanel.PlayerChallenged
                playerChallengedObject[i] = this.gameObject.transform.GetChild(1).GetChild(i).GetChild(1).GetChild(4).gameObject;
            }
            if (ActionAmbassador[i] == null)
            {
                unassigned = true;
                // Table                     .players     .player[i] .GamePanel. playerTurn .extrapanel
                ActionAmbassador[i] = this.gameObject.transform.GetChild(1).GetChild(i).GetChild(1).GetChild(2).GetChild(1).GetChild(1).GetComponent<UdonBehaviour>();
            }

            gamePanel[i].SetActive(false);

            if (unassigned)
                Debug.LogError("WARNING! Missings assignments in 'table', will auto assign based of hierarchy");
        }
        RequestSerialization();
    }

    // game statuses

    //Networked Owner
    public void StartGame()
    {
        playersLeft = 0;
        playerSkipCount = 0;

        deck = populateDeck(deck);

        //get all active players, disable the rest
        for (int i = 0; i < playerPanelTotalCount; i++)
        {
            int playerID = (int)playerScripts[i].GetProgramVariable("playerID");

            if (playerID != 0)
            {
                //add position of player instead if playerID
                playersLeft++;

                playerNameList[i] = (string)playerScripts[i].GetProgramVariable("playerName");

                playerHPList[i] = 2;
                playerIsAliveList[i] = true;

                if (deck.TryGetValue(0, out DataToken setCard1))
                    playerScripts[i].SetProgramVariable("card1", (int)setCard1);
                if (deck.TryGetValue(1, out DataToken setCard2))
                    playerScripts[i].SetProgramVariable("card2", (int)setCard2);

                deck.RemoveRange(0, 2);

                playerScripts[i].RequestSerialization();

                //reveal cards for object owner only
                playerScripts[i].SendCustomNetworkEvent(Owner, "revealCards");

            }
            else
            {
                playerNameList[i] = "Empty";

                playerHPList[i] = 0;
                playerIsAliveList[i] = false;
            }
        }

        SendCustomNetworkEvent(All, "startGameNetworked");

        //randomize who starts
        currentPlayer = Random.Range(0, playersLeft);

        gameAnnouncement.SendCustomNetworkEvent(All, "StartGame");

        int skip = 0;
        for (int i = 0; i <= 6; i++)
        {
            if (playerIsAliveList[i] == false)
            {
                continue;
            }
            else
            {
                if (skip == currentPlayer)
                {
                    playerScripts[i].SendCustomNetworkEvent(Owner, "playerTurn");
                    currentPlayer = i;
                    break;
                }
                else
                {
                    if (playerIsAliveList[i] == true)
                        skip++;
                }
            }
        }
    }

    // networked all
    public void startGameNetworked()
    {
        startButton.SetActive(false);
        // disable joining/leaving ALL PLAYERS
        for (int i = 0; i < 6; i++)
            joinLeaveGameObject[i].SetActive(false);

        // only active players
        for (int i = 0; i < 6; i++)
            if ((int)playerScripts[i].GetProgramVariable("playerID") != 0)
            {
                gamePanel[i].SetActive(true);
                cardsGameObject[i].SetActive(true);

                cardsGameObject[i].transform.GetChild(0).gameObject.SetActive(true);
                cardsGameObject[i].transform.GetChild(1).gameObject.SetActive(true);

                playerScripts[i].SetProgramVariable("playerIsk", 2);
            }
            else
            {
                gamePanel[i].SetActive(false);
                cardsGameObject[i].SetActive(false);
            }
        RequestSerialization();
    }

    // networked Owner
    public void NextPlayer()
    {
        if (playersLeft > 1)
        {

            // reset all turn specific variables
            currentPlayer++;
            playerSkipCount = 0;
            playerIsSelected = false;
            challengeSuccess = false;
            playerCalledBluff = false;
            playerActionBlocked = false;

            bool foundPlayer = false;

            for (int i = 0; i <= 5; i++)
            {
                if (currentPlayer > 5)
                    currentPlayer = 0;

                if (foundPlayer)
                    break;

                bool alive = playerIsAliveList[currentPlayer];

                if (alive)
                {
                    foundPlayer = true;
                    playerScripts[currentPlayer].SendCustomNetworkEvent(Owner, "playerTurn");
                }
                else
                {
                    currentPlayer++;
                }
            }
        }
        else
        {
            SendCustomNetworkEvent(All, "GameOver");
        }
    }

    //networked All
    public void GameOver()
    {
        startButton.SetActive(true);

        //hard reset everything
        for (int i = 0; i < 6; i++)
        {
            joinLeaveGameObject[i].SetActive(true);
            gamePanel[i].SetActive(false);
            playerScripts[i].SendCustomNetworkEvent(Owner, "Reset");
        }
    }



    // ----------------- action challenge continuations below -----------------

    // refer to onchange callback playerSkipCountChange()
    private void ContinueAction()
    {
        int isk;
        int iskTargetPlayer;
        int iskCurrentPlayer;

        switch (playerActionPlayed)
        {
            case 2: // Foreign Aid


                isk = (int)playerScripts[currentPlayer].GetProgramVariable("playerIsk");
                isk = isk + 2;
                playerScripts[currentPlayer].SetProgramVariable("playerIsk", isk);

                NextPlayer();

                break;
            case 4: // ambassador
                playerScripts[currentPlayer].SendCustomNetworkEvent(Owner, "PlayerTurnAmbassador");
                break;
            case 5: // Assassin
                // this code can only be reached by player skipping an assassination
                if (playerIsAliveList[playerSelected])
                {
                    PlayerChallenged[playerSelected].SetProgramVariable("forcedDiscard", 2);
                    PlayerChallenged[playerSelected].SendCustomNetworkEvent(Owner, "PlayerChallenged");
                    playerChallengedObject[playerSelected].SetActive(false);
                    playerChallengedObject[playerSelected].SetActive(true);
                }
                break;
            case 6: // Captain
                iskCurrentPlayer = (int)playerScripts[currentPlayer].GetProgramVariable("playerIsk");

                iskTargetPlayer = (int)playerScripts[playerSelected].GetProgramVariable("playerIsk");

                // Current player
                if (iskTargetPlayer >= 2)
                    iskCurrentPlayer = iskCurrentPlayer + 2;
                else if (iskTargetPlayer == 1)
                    iskCurrentPlayer = iskCurrentPlayer + 1;

                playerScripts[currentPlayer].SetProgramVariable("playerIsk", iskCurrentPlayer);

                // Selected player

                iskTargetPlayer = iskTargetPlayer - 2;

                if (iskTargetPlayer < 0)
                    iskTargetPlayer = 0;

                playerScripts[playerSelected].SetProgramVariable("playerIsk", iskTargetPlayer);
                NextPlayer();
                break;
            case 7: // Duke
                isk = (int)playerScripts[currentPlayer].GetProgramVariable("playerIsk");
                isk = isk + 3;
                playerScripts[currentPlayer].SetProgramVariable("playerIsk", isk);

                NextPlayer();
                break;
            default:
                NextPlayer();
                break;
        }
    }

    // networked Owner
    public void ChallengeContinue()
    {
        Debug.Log("ActPlayed: " + playerActionPlayed + ", ActBlocked: " + playerActionBlocked + ", challengeSuccess: " + challengeSuccess);

        if (playerActionPlayed == 2 && playerActionBlocked && !challengeSuccess)
            ContinueAction();
        else if (playerActionPlayed == 2 && playerActionBlocked && challengeSuccess)
            NextPlayer();

        else if (playerActionPlayed == 3)
            NextPlayer();

        else if (playerActionPlayed == 4 && !playerActionBlocked && challengeSuccess)
            ContinueAction();
        else if (playerActionPlayed == 4 && !playerActionBlocked && !challengeSuccess)
            NextPlayer();

        else if (playerActionPlayed == 5)
            NextPlayer();

        else if (playerActionPlayed == 6 && playerActionBlocked && !challengeSuccess)
            ContinueAction();
        else if (playerActionPlayed == 6 && playerActionBlocked && challengeSuccess)
            NextPlayer();
        else if (playerActionPlayed == 6 && !playerActionBlocked && !challengeSuccess)
            NextPlayer();

        else if (playerActionPlayed == 7 && !playerActionBlocked && challengeSuccess)
            ContinueAction();
        else if (playerActionPlayed == 7 && !playerActionBlocked && !challengeSuccess)
            NextPlayer();

        else
        {
            Debug.LogError("Table ERROR");
        }
    }

    // networked Owner
    public void ChallengeSuccess()
    {
        //player had card they claimed, force discard challenger
        //playerActionBlocked = false;

        if (playerActionPlayed == 0)
            playerActionBlocked = false;

        challengeSuccess = true;

        if (deck.TryGetValue(0, out DataToken card))
            playerScripts[playerClaimID].SetProgramVariable("newCard", (int)card);

        deck.RemoveAt(0);
        playerScripts[playerClaimID].SendCustomNetworkEvent(Owner, "PlayerDrawNewCard");

        int oldCard = (int)playerScripts[playerClaimID].GetProgramVariable("oldCard");
        deck.Add(oldCard);

        deck = ShuffleDeck();
        deck = ShuffleDeck();

        PlayerChallenged[playerChallengePanelID].SetProgramVariable("forcedDiscard", 1);
        PlayerChallenged[playerChallengePanelID].SendCustomNetworkEvent(Owner, "PlayerChallenged");
    }



    // ----------------- action played, counteractions below -----------------

    // General function! - sends counteraction to everyone except current player
    private void PlayerAction(string counterAction)
    {
        Debug.Log("playerActionPlayed: " + playerActionPlayed);

        for (int i = 0; i <= 5; i++)
        {
            if (i == currentPlayer || // if same action came from same player
                !playerIsAliveList[i]) // if player dead
                continue;

            if (playerIsSelected && playerSelected == i)
                CounterActions[i].SendCustomNetworkEvent(Owner, counterAction + "Targeted");
            else
                CounterActions[i].SendCustomNetworkEvent(Owner, counterAction);
        }
    }

    //networked Owner
    public void PlayerActionSelected()
    {
        string counterAction = "";

        Debug.Log("PlayerActionSelected | playerActionPlayed: " + playerActionPlayed);

        switch (playerActionPlayed)
        {
            case 2:
                counterAction = "counterActionForeignAid";
                break;
            case 4:
                counterAction = "counterActionAmbassador";
                break;
            case 5:
                counterAction = "counterActionAssassin";
                break;
            case 6:
                counterAction = "counterActionCaptain";
                break;
            case 7:
                counterAction = "CounterActionCallBluffDuke";
                break;
        }

        if (playerActionPlayed == 3) //coup
        {
            PlayerChallenged[playerSelected].SetProgramVariable("forcedDiscard", 3);
            PlayerChallenged[playerSelected].SendCustomNetworkEvent(Owner, "PlayerChallenged");
        }
        else
        {
            //assassin, deduct 3 isk
            if (playerActionPlayed == 5)
            {
                int isk = (int)playerScripts[currentPlayer].GetProgramVariable("playerIsk");
                isk = isk - 3;

                playerScripts[currentPlayer].SetProgramVariable("playerIsk", isk);
            }

            for (int i = 0; i <= 5; i++)
            {
                string test = "Loop: " + i;
                if (i == currentPlayer || // if same action came from same player
                    !playerIsAliveList[i]) // if player dead
                    continue;



                if (playerIsSelected && playerSelected == i)
                    CounterActions[i].SendCustomNetworkEvent(Owner, counterAction + "Targeted");
                else
                    CounterActions[i].SendCustomNetworkEvent(Owner, counterAction);

                Debug.Log("found player: " + i);
            }
        }
    }



    // ----------------- blocks  below -----------------

    // networked Owner
    public void PlayerBlock()
    {
        playerActionBlocked = true;

        for (int i = 0; i <= 5; i++)
        {
            if (!playerIsAliveList[i] ||
                (int)playerScripts[i].GetProgramVariable("PanelID") == playerClaimID)
                continue;

            switch (claimedCard)
            {
                case 0:
                    CounterActions[i].SendCustomNetworkEvent(Owner, "counterActionAmbassador");
                    break;
                case 2:
                    CounterActions[i].SendCustomNetworkEvent(Owner, "counterActionCaptain");
                    break;
                case 3:
                    CounterActions[i].SendCustomNetworkEvent(Owner, "CounterActionCallBluffContessa");
                    break;
                case 4:
                    CounterActions[i].SendCustomNetworkEvent(Owner, "CounterActionCallBluffDuke");
                    break;
            }
        }
    }

    // networked Owner
    public void CallBluff()
    {
        playerCalledBluff = true;

        for (int i = 0; i <= 5; i++)
        {
            if (i == playerClaimID)
            {
                PlayerChallenged[i].SetProgramVariable("claimedCard", claimedCard);
                PlayerChallenged[i].SetProgramVariable("forcedDiscard", 0);
                PlayerChallenged[i].SendCustomNetworkEvent(Owner, "PlayerChallenged");
            }
            else if (i == playerChallengePanelID)
            {
                //potential?
            }
            else
            {
                CounterActions[i].SendCustomNetworkEvent(Owner, "disablePanel");
            }
        }
    }



    // misc ----------------------------------

    private DataList populateDeck(DataList deckCard)
    {
        //clear all cards before populating and randomizing
        deckCard.Clear();

        foreach (int id in cardID)
        {
            deckCard.Add(id);
            deckCard.Add(id);
            deckCard.Add(id);
        }
        deckCard = ShuffleDeck();

        return deckCard;
    }

    private DataList ShuffleDeck()
    {
        DataList shuffle = new DataList();

        var length = deck.Count;
        var rng = 0;

        for (int i = 0; i <= length; i++)
        {
            rng = Random.Range(0, deck.Count);

            if (deck.TryGetValue(rng, out DataToken value))
            {
                shuffle.Add(value);
                deck.RemoveAt(rng);
            }
        }
        return shuffle;
    }

    public void AmbassadorGrab2Cards()
    {
        if (deck.TryGetValue(0, out DataToken card1))
            ActionAmbassador[currentPlayer].SetProgramVariable("deckCard1", (int)card1);

        if (deck.TryGetValue(1, out DataToken card2))
            ActionAmbassador[currentPlayer].SetProgramVariable("deckCard2", (int)card2);
    }

    public void AmbassadorDone()
    {
        deck.Add(ambassadorCard1);
        deck.Add(ambassadorCard2);

        deck.RemoveRange(0, 2);

        deck = ShuffleDeck();
        deck = ShuffleDeck();
    }

    // networked Owner
    public void PlayerDead()
    {
        playersLeft--;
        playerIsAliveList[playerDeadId] = false;
    }



    //-------- !onchange callbacks below ! -------------------------------------------------------  !onchange callbacks below !  ----------------

    //refer to ContinueAction() for details
    public int playerSkipCountChange
    {
        set
        {
            playerSkipCount = value;
            RequestSerialization();

            if (playerSkipCount == (playersLeft - 1))
            {
                if (playerActionBlocked == true)
                    NextPlayer();
                else
                    ContinueAction();
            }
        }
        get => playerSkipCount;
    }

    public int playerChallengePanelIDChange
    {
        set
        {
            playerChallengePanelID = value;
            RequestSerialization();
        }
        get => playerChallengePanelID;
    }

    public int playerClaimIDChange
    {
        set
        {
            playerClaimID = value;
            RequestSerialization();
        }
        get => playerClaimID;
    }

    public int playersLeftChange
    {
        set
        {
            playersLeft = value;
            RequestSerialization();
        }
        get => playersLeft;
    }

    public bool[] playerIsAliveListChange
    {
        set
        {
            playerIsAliveList = value;
            RequestSerialization();
        }
        get => playerIsAliveList;
    }

    public string[] playerNameListChange
    {
        set
        {
            playerNameList = value;
            RequestSerialization();
        }
        get => playerNameList;
    }

    public int playerDeadIdChange
    {
        set
        {
            playerDeadId = value;
            RequestSerialization();
        }
        get => playerDeadId;
    }

    public int playerSelectedChange
    {
        set
        {
            playerSelected = value;
            RequestSerialization();
        }
        get => playerSelected;
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

    public bool playerCalledBluffChange
    {
        set
        {
            playerCalledBluff = value;
            RequestSerialization();
        }
        get => playerCalledBluff;
    }

    public bool challengeSuccessChange
    {
        set
        {
            challengeSuccess = value;
            RequestSerialization();
        }
        get => challengeSuccess;
    }

    public int playerActionPlayedChange
    {
        set
        {
            playerActionPlayed = value;
            RequestSerialization();
        }
        get => playerActionPlayed;
    }

}