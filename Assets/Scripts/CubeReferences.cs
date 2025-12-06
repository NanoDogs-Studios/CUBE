using UnityEngine;

public class CubeReferences : MonoBehaviour
{
    public static BasePlayer GetKiller()
    {
        // loop through all players and find the killer
        foreach (var player in FindObjectsByType<BasePlayer>(FindObjectsSortMode.None))
        {
            if (player.GetPlayerType() == BasePlayer.PlayerType.Killer)
            {
                return player;
            }
        }
        return null;
    }

    public static BasePlayer[] GetSurvivors()
    {
        // loop through all players and find the survivors
        var survivors = new System.Collections.Generic.List<BasePlayer>();
        foreach (var player in FindObjectsByType<BasePlayer>(FindObjectsSortMode.None))
        {
            if (player.GetPlayerType() == BasePlayer.PlayerType.Survivor)
            {
                survivors.Add(player);
            }
        }
        return survivors.ToArray();
    }
}
