
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class CounterActionScript : UdonSharpBehaviour
{
    [Header("UdonBehaviour Script References")]
    public GameObject self;
    public UdonBehaviour Table;
    public UdonBehaviour Player;

    [Header("Counteractions that a player can make")]
    public GameObject Skip;
    public GameObject[] CounterActions;

    // for increased readability
    private VRC.Udon.Common.Interfaces.NetworkEventTarget Owner = VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner;
    //private VRC.Udon.Common.Interfaces.NetworkEventTarget All = VRC.Udon.Common.Interfaces.NetworkEventTarget.All;

    private void setChallengerId()
    {
        var id = Player.GetProgramVariable("PanelID");
        Table.SetProgramVariable("playerChallengePanelID", id);
    }

    private void setClaimId()
    {
        var id = Player.GetProgramVariable("PanelID");
        Table.SetProgramVariable("playerClaimID", id);
    }

    //networked Owner
    public void disablePanel()
    {
        self.SetActive(false);
    }

    public void PlayerSkip()
    {
        int playerSkipCount = (int)Table.GetProgramVariable("playerSkipCount");
        playerSkipCount++;
        Table.SetProgramVariable("playerSkipCount", playerSkipCount);

        self.SetActive(false);
    }

    private void ActivatePanel(int panelId)
    {
        CounterActions[0].SetActive(panelId == 0);
        CounterActions[1].SetActive(panelId == 1);
        CounterActions[2].SetActive(panelId == 2);
        CounterActions[3].SetActive(panelId == 3);
        CounterActions[4].SetActive(panelId == 4);
        CounterActions[5].SetActive(panelId == 5);
        CounterActions[6].SetActive(panelId == 6);
        CounterActions[7].SetActive(panelId == 7);
    }

    //CounterActions

    public void counterActionForeignAid()
    {
        ActivatePanel(0);
        self.SetActive(true);
    }

    public void counterActionAmbassador()
    {
        ActivatePanel(4);
        self.SetActive(true);
    }

    public void counterActionAssassin()
    {
        ActivatePanel(2);
        self.SetActive(true);
    }

    public void counterActionCaptain()
    {
        ActivatePanel(3);
        self.SetActive(true);
    }

    public void counterActionContessa()
    {
        ActivatePanel(5);
        self.SetActive(true);
    }

    //callbluff panels

    public void CounterActionCallBluffDuke()
    {
        ActivatePanel(1);
        self.SetActive(true);
    }

    public void CounterActionCallBluffContessa()
    {
        ActivatePanel(5);
        self.SetActive(true);
    }

    //targeted

    public void counterActionAssassinTargeted()
    {
        ActivatePanel(6);
        self.SetActive(true);

    }

    public void counterActionCaptainTargeted()
    {
        ActivatePanel(7);
        self.SetActive(true);
    }

    // ---- claims below

    public void BlockContessa()
    {
        setClaimId();
        Table.SetProgramVariable("claimedCard", 3);
        Table.SendCustomNetworkEvent(Owner, "PlayerBlock");

        self.SetActive(false);
    }

    public void BlockAmbassador()
    {
        setClaimId();
        Table.SetProgramVariable("claimedCard", 0);
        Table.SendCustomNetworkEvent(Owner, "PlayerBlock");

        self.SetActive(false);
    }

    public void BlockCaptain()
    {
        setClaimId();
        Table.SetProgramVariable("claimedCard", 2);
        Table.SendCustomNetworkEvent(Owner, "PlayerBlock");

        self.SetActive(false);
    }

    public void BlockDuke()
    {
        setClaimId();
        Table.SetProgramVariable("claimedCard", 4);
        Table.SendCustomNetworkEvent(Owner, "PlayerBlock");

        self.SetActive(false);
    }

    // ---- generic call bluffs below

    public void CallBluff()
    {
        setChallengerId();
        Table.SendCustomNetworkEvent(Owner, "CallBluff");
        self.SetActive(false);
    }
}