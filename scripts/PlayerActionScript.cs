
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

using UnityEngine.UI; // Button[]

public class PlayerActionScript : UdonSharpBehaviour
{
    [Header("UdonBehaviour Script References")]
    public UdonBehaviour Player;
    public UdonBehaviour Table;
    public UdonBehaviour playerSelect;
    public GameObject PlayerTurn;
    public GameObject PlayerAction;
    public GameObject extraPanels;

    private int selectPlayerAction; // 0 = coup, 1 = assassin, 2 = captain

    [Header("Button References")]
    public Button[] buttonActions;

    // for increased readability
    private VRC.Udon.Common.Interfaces.NetworkEventTarget Owner = VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner;
    private VRC.Udon.Common.Interfaces.NetworkEventTarget All = VRC.Udon.Common.Interfaces.NetworkEventTarget.All;

    //slow solution, find something else
    void OnEnable()
    {
        var playerIsk = (int)Player.GetProgramVariable("playerIsk");
        if (playerIsk >= 10)
        {
            buttonActions[0].interactable = false;
            buttonActions[1].interactable = false;
            buttonActions[2].interactable = true;
            buttonActions[3].interactable = false;
            buttonActions[4].interactable = false;
            buttonActions[5].interactable = false;
            buttonActions[6].interactable = false;
        }
        else if (playerIsk >= 7)
        {
            buttonActions[0].interactable = true;
            buttonActions[1].interactable = true;
            buttonActions[2].interactable = true;
            buttonActions[3].interactable = true;
            buttonActions[4].interactable = true;
            buttonActions[5].interactable = true;
            buttonActions[6].interactable = true;
        }
        else if (playerIsk >= 3)
        {
            buttonActions[0].interactable = true;
            buttonActions[1].interactable = true;
            buttonActions[2].interactable = false;
            buttonActions[3].interactable = true;
            buttonActions[4].interactable = true;
            buttonActions[5].interactable = true;
            buttonActions[6].interactable = true;
        }
        else
        {
            buttonActions[0].interactable = true;
            buttonActions[1].interactable = true;
            buttonActions[2].interactable = false;
            buttonActions[3].interactable = true;
            buttonActions[4].interactable = false;
            buttonActions[5].interactable = true;
            buttonActions[6].interactable = true;
        }

    }

    private void DELETEME()
    {
        if (selectPlayerAction != 1)
            Debug.Log("DELETEME");
    }

    //networked Owner
    public void actionIncome()  // action 1, cannot be countered
    {

        SendCustomNetworkEvent(All, "actionIncomeNetworked");
        Table.SendCustomNetworkEvent(Owner, "NextPlayer");
        PlayerTurn.SetActive(false);
    }

    //networked all
    public void actionIncomeNetworked()
    {
        var Isk = (int)Player.GetProgramVariable("playerIsk");
        Isk++;

        Player.SetProgramVariable("playerIsk", Isk);
    }


    public void actionForeignAid() // action 2
    {
        Table.SetProgramVariable("playerActionPlayed", 2);
        Table.SendCustomNetworkEvent(Owner, "PlayerActionSelected");
        PlayerTurn.SetActive(false);
    }

    public void actionCoup() // action 3, cannot be countered
    {
        // must select player, refer to PlayerSelectScript

        PlayerAction.SetActive(false);
        extraPanels.SetActive(true);
        selectPlayerAction = 0;

        playerSelect.SendCustomNetworkEvent(Owner, "SelectPlayer");
    }

    public void actionAmbassador() // action 4
    {
        Table.SetProgramVariable("playerActionPlayed", 4);
        Table.SendCustomNetworkEvent(Owner, "PlayerActionSelected");

        var id = Player.GetProgramVariable("PanelID");

        Table.SetProgramVariable("playerClaimID", id);
        Table.SetProgramVariable("claimedCard", 0);

        PlayerTurn.SetActive(false);
    }

    public void actionAssassin() // action 5
    {
        // must select player, refer to PlayerSelectScript

        PlayerAction.SetActive(false);
        extraPanels.SetActive(true);
        selectPlayerAction = 1;

        playerSelect.SendCustomNetworkEvent(Owner, "SelectPlayer");
    }

    public void actionCaptain()  // action 6
    {
        // must select player, refer to PlayerSelectScript

        PlayerAction.SetActive(false);
        extraPanels.SetActive(true);
        selectPlayerAction = 2;

        playerSelect.SendCustomNetworkEvent(Owner, "SelectPlayer");
    }

    public void actionDuke()  // action 7
    {
        Table.SetProgramVariable("playerActionPlayed", 7);
        Table.SendCustomNetworkEvent(Owner, "PlayerActionSelected");

        var id = Player.GetProgramVariable("PanelID");

        Table.SetProgramVariable("playerClaimID", id);
        Table.SetProgramVariable("claimedCard", 4);

        PlayerTurn.SetActive(false);
    }
}