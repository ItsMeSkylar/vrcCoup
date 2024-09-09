
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

using TMPro;

public class GameAnnouncementScript : UdonSharpBehaviour
{
    private Vector3 playerLoc;
    private Vector3 myLoc;

    public TextMeshProUGUI text;

    void Start()
    {
        myLoc = this.transform.localPosition;
    }

    public void StartGame()
    {
    text.text = "playerId: " + Networking.LocalPlayer.playerId;
    }

    
    void Update()
    {


        playerLoc = Networking.LocalPlayer.GetBonePosition(HumanBodyBones.Head);
        this.transform.LookAt(playerLoc);
        Debug.DrawLine(myLoc, playerLoc);


        //update pos of localplayer
    }
}
