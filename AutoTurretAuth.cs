using System.Collections.Generic;
using System.Linq;
using ProtoBuf;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Auto Turret Authorization", "haggbart", "1.1.0")]
    [Description("One-way synchronizing cupboard authorization with auto-turrets.")]
    class AutoTurretAuth : RustPlugin
    {

        private const string PERSISTENT_AUTHORIZATION = "Use persistent authorization?";
        
        protected override void LoadDefaultConfig()
        {
            Config[PERSISTENT_AUTHORIZATION] = true;
        }

        private void Init()
        {
            if (!(bool)Config[PERSISTENT_AUTHORIZATION]) return;
            Unsubscribe(nameof(OnCupboardDeauthorize));
            Unsubscribe(nameof(OnCupboardClearList));
        }

        #region hooks
        
        private object OnCupboardAuthorize(BuildingPrivlidge privilege, BasePlayer player)
        {
            var turrets = GetAutoTurrets(privilege.buildingID);

            foreach (AutoTurret turret in turrets)
            {
                AddPlayer(turret, GetPlayerNameId(player));
            }
            return null;
        }
        
        private object OnCupboardDeauthorize(BuildingPrivlidge privilege, BasePlayer player)
        {
            var turrets = GetAutoTurrets(privilege.buildingID);

            foreach (AutoTurret turret in turrets)
            {
                RemovePlayer(turret, player.userID);
            }
            return null;
        }
        
        private object OnCupboardClearList(BuildingPrivlidge privilege, BasePlayer player)
        {
            var turrets = GetAutoTurrets(privilege.buildingID);

            foreach (AutoTurret turret in turrets)
            {
                turret.authorizedPlayers.Clear();
                turret.SendNetworkUpdate();
            }
            
            return null;
        }
        
        private void OnEntityBuilt(Planner plan, GameObject go)
        {
            var turret = go.ToBaseEntity() as AutoTurret;
            if (turret == null) return;
            var authorizedPlayers = turret.GetBuildingPrivilege()?.authorizedPlayers;
            if (authorizedPlayers == null) return;
            foreach (PlayerNameID playerNameId in authorizedPlayers)
            {
                AddPlayer(turret, playerNameId);
            }
        }
        
        #endregion hooks

        private static IEnumerable<AutoTurret> GetAutoTurrets(uint buildingId)
        {
            var turrets = UnityEngine.Object.FindObjectsOfType<AutoTurret>()
                .Where(x => x.GetBuildingPrivilege()?.buildingID == buildingId);
            return turrets;
        }

        private static void AddPlayer(AutoTurret turret, PlayerNameID playerNameId)
        {
            //turret.authorizedPlayers.RemoveAll(x => x.userid == playerNameId.userid); // this it what facepunch does to ensure players recorded twice
            for (var i = 0; i < turret.authorizedPlayers.Count; i++)
            {
                if (turret.authorizedPlayers[i].userid != playerNameId.userid) continue;
                turret.authorizedPlayers.RemoveAt(i);
                break;
            }
            turret.authorizedPlayers.Add(playerNameId);
            turret.SendNetworkUpdate();
        }

        private static void RemovePlayer(AutoTurret turret, ulong userId)
        {
            for (int i = turret.authorizedPlayers.Count - 1; i >= 0; i--)
            {
                if (turret.authorizedPlayers[i].userid != userId) continue;
                turret.authorizedPlayers.RemoveAt(i);
                turret.SendNetworkUpdate();
                return;
            }
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