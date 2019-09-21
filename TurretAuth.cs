using System.Collections.Generic;
using ProtoBuf;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Turret Authorization", "haggbart", "1.0.1")]
    [Description("Makes turrets act in a similar fashion to shotgun traps and flame turrets.")]
    class TurretAuth : RustPlugin
    {
        
        private object OnTurretTarget(AutoTurret turret, BaseCombatEntity entity)
        {
            var player = entity as BasePlayer;
            if (!IsAuthed(player, turret)) return null;
            AuthPlayer(turret, GetPlayerNameId(player));
            return false;
        }
        
        private static bool IsAuthed(BasePlayer player, BaseEntity entity)
        {
            foreach (PlayerNameID playerNameId in entity.GetBuildingPrivilege().authorizedPlayers)
            {
                if (playerNameId.userid == player.userID) return true;
            }
            return false;
        }

        private static void AuthPlayer(AutoTurret turret, PlayerNameID playerNameId)
        {
            turret.authorizedPlayers.Add(playerNameId);
            turret.SendNetworkUpdate();
        }

        private static PlayerNameID GetPlayerNameId(BasePlayer player)
        {
            var playerNameId = new PlayerNameID()
            {
                userid = player.userID,
                username = player.displayName
            };
            return playerNameId;
        }
    }
}