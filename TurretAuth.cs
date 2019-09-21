using System.Collections.Generic;
using ProtoBuf;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Turret Authorization", "haggbart", "1.0.0")]
    [Description("Makes turrets act in a similar fashion to shotgun traps and flame turrets.")]
    class TurretAuth : RustPlugin
    {
        private void OnEntityBuilt(Planner plan, GameObject go)
        {
            var turret = go.ToBaseEntity() as AutoTurret;
            if (turret == null) return;
            var authorizedPlayers = turret.GetBuildingPrivilege().authorizedPlayers;
            AuthCupboard(turret, authorizedPlayers);
        }

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
        
        private static void AuthCupboard(AutoTurret turret, IEnumerable<PlayerNameID> playerNameIds)
        {
            foreach (PlayerNameID playerNameId in playerNameIds)
            {
                AuthPlayer(turret, playerNameId);
            }
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

        [ChatCommand("unauth")] // testing
        void CmdUnAuth(BasePlayer player, string command, string[] args)
        {
            if (!player.IsAdmin) return;
            RaycastHit hit;
            if (!Physics.Raycast(player.eyes.HeadRay(), out hit, 30)) return;
            SendReply(player, hit.GetEntity().ToString());
            var turret = (AutoTurret)hit.GetEntity();

            turret.authorizedPlayers.Clear();
            turret.SendNetworkUpdate();
        }
    }
}