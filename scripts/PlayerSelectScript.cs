
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

using UnityEngine.UI; // Button[]
using TMPro;

public class PlayerSelectScript : UdonSharpBehaviour
{

    public UdonBehaviour Player;
    public UdonBehaviour Table;
    public UdonBehaviour PlayerActionScript;
    public GameObject PlayerTurn;
    public GameObject self;

    public Button[] selectPlayerButton;
    public TextMeshProUGUI[] selectPlayerButtonName;
    private int[] sortedPlayers = new int[6];

    private VRC.Udon.Common.Interfaces.NetworkEventTarget Owner = VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner;

    private void sortArray()
    {
        //sorts the array with the current player first e.g [4, 5, 0, 1, 2, 3]
        int thisPlayer = (int)Player.GetProgramVariable("PanelID");

        int current;

        for (int i = 0; i < 6; i++)
        {
            current = (thisPlayer + i);
            if (current >= 6)
                current = current - 6;

            sortedPlayers[i] = current;
        }
    }

    public void Return() { }

    public void SelectPlayer()
    {

        self.SetActive(true);

        bool[] playerIsAlive = (bool[])Table.GetProgramVariable("playerIsAliveList");
        string[] playerNames = (string[])Table.GetProgramVariable("playerNameList");

        sortArray();

        for (int i = 0; i < 5; i++)
        {
            selectPlayerButton[i].interactable = playerIsAlive[sortedPlayers[i + 1]];
            selectPlayerButtonName[i].text = playerNames[sortedPlayers[i + 1]];
        }
    }

    private void continueActionPlayerSelected(int selectedPlayerPanelId)
    {

        Table.SetProgramVariable("playerSelected", selectedPlayerPanelId);
        int playerAction = (int)PlayerActionScript.GetProgramVariable("selectPlayerAction");
        var id = Player.GetProgramVariable("PanelID");

        Table.SetProgramVariable("playerIsSelected", true);

        /*
        action 1: Income
        action 2: ForeignAid
        action 3: Coup
        action 4: Ambassador
        action 5: Assassin
        action 6: Captain
        action 7: Duke
        */

        switch (playerAction)
        {
            case 0:
                Table.SetProgramVariable("playerActionPlayed", 3); // Coup
                Table.SendCustomNetworkEvent(Owner, "PlayerActionSelected");
                break;
            case 1:
                Table.SetProgramVariable("playerActionPlayed", 5); // Assassin
                Table.SendCustomNetworkEvent(Owner, "PlayerActionSelected");

                Table.SetProgramVariable("playerClaimID", id);
                Table.SetProgramVariable("claimedCard", 1);

                break;
            case 2:
                Table.SetProgramVariable("playerActionPlayed", 6); // Captain
                Table.SendCustomNetworkEvent(Owner, "PlayerActionSelected");

                Table.SetProgramVariable("playerClaimID", id);
                Table.SetProgramVariable("claimedCard", 2);
                break;
        }

        PlayerTurn.SetActive(false);
        self.SetActive(false);

    }

    public void SelectPlayer1()
    {
        continueActionPlayerSelected(sortedPlayers[1]);
    }

    public void SelectPlayer2()
    {
        continueActionPlayerSelected(sortedPlayers[2]);
    }

    public void SelectPlayer3()
    {
        continueActionPlayerSelected(sortedPlayers[3]);
    }

    public void SelectPlayer4()
    {
        continueActionPlayerSelected(sortedPlayers[4]);
    }

    public void SelectPlayer5()
    {

        continueActionPlayerSelected(sortedPlayers[5]);
    }
}
