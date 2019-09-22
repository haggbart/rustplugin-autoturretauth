using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ProtoBuf;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Auto Turret Authorization", "haggbart", "1.1.2")]
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
        
        private void OnCupboardAuthorize(BuildingPrivlidge privilege, BasePlayer player)
        {
            var turrets = GetAutoTurrets(privilege.buildingID);
            ServerMgr.Instance.StartCoroutine(AddPlayer(turrets, GetPlayerNameId(player)));
        }
        
        private void OnCupboardDeauthorize(BuildingPrivlidge privilege, BasePlayer player)
        {
            var turrets = GetAutoTurrets(privilege.buildingID);
            ServerMgr.Instance.StartCoroutine(RemovePlayer(turrets, player.userID));
        }
        
        private void OnCupboardClearList(BuildingPrivlidge privilege, BasePlayer player)
        {
            var turrets = GetAutoTurrets(privilege.buildingID);
            ServerMgr.Instance.StartCoroutine(RemovePlayer(turrets, player.userID));
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
        
        private static IEnumerator AddPlayer(IEnumerable<AutoTurret> turrets, PlayerNameID playerNameId)
        {
            
            foreach (AutoTurret turret in turrets)
            {
                RemovePlayer(turret, playerNameId.userid);
                turret.authorizedPlayers.Add(playerNameId);
                turret.SendNetworkUpdate();
                yield return new WaitForFixedUpdate();
            }
        }

        private static void AddPlayer(AutoTurret turret, PlayerNameID playerNameId)
        {
            RemovePlayer(turret, playerNameId.userid);
            turret.authorizedPlayers.Add(playerNameId);
            turret.SendNetworkUpdate();
        }
        
        private static IEnumerator RemovePlayer(IEnumerable<AutoTurret> turrets, ulong userId)
        {
            foreach (AutoTurret turret in turrets)
            {
                RemovePlayer(turret, userId);
                turret.SendNetworkUpdate();
                yield return new WaitForFixedUpdate();
            }
        }
        
        private static void RemovePlayer(AutoTurret turret, ulong userId)
        {
            //turret.authorizedPlayers.RemoveAll(x => x.userid == playerNameId.userid); // this it what facepunch does to ensure players recorded twice
            for (int i = turret.authorizedPlayers.Count - 1; i >= 0; i--)
            {
                if (turret.authorizedPlayers[i].userid != userId) continue;
                turret.authorizedPlayers.RemoveAt(i);
                break;
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