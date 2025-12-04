using System;
using Newtonsoft.Json.Linq;
using UnityEngine;

public class CatteJsonParse
{
    public static void HandleParseDataGame(JObject jData)
    {
        var gameView = (CatteGameView)UIManager.instance.gameView;
        if (gameView == null) return;
        string evt = (string)jData["evt"];

        switch (evt)
        {
            // case "ctable":
            //     gameView.handleCTable(jData.ToString());
            //     break;
            // case "stable":
            //     gameView.handleSTable(jData.ToString());
            //     break;
            // case "vtable":
            //     gameView.handleVTable(jData.ToString());
            //     break;
            // case "jtable":
            //     gameView.handleJTable(jData.ToString());
            //     break;
            // case "rjtable":
            //     gameView.handleRJTable(jData.ToString());
            //     break;
            // case "ltable":
            //     gameView.handleLTable(jData);
            //     break;
            case "startGame":
                gameView.HandleStartGame(jData.ToString());
                break;
            case "lc":
                gameView.HandleLc(jData);
                break;
            case "showCard":
                gameView.HandleShowCard(jData);
                break;
            case "dropCard":
                gameView.HandleDropCard(jData);
                break;
            case "lost4round":
                gameView.HandleLost4Round(jData);
                break;
            case "addPot":
                //hgds
                gameView.HandleAddPot(jData);
                break;
            case "punish":
                gameView.HandlePunish(jData);
                break;
            case "finish":
                //csygbjwe
                gameView.HandleFinishGame(jData);
                break;
            case "history":
                gameView.HandleHistory(jData);
                break;
        }
    }
}
