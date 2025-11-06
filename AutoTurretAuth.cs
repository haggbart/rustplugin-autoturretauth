using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ProtoBuf;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Auto Turret Authorization", "haggbart", "1.2.4")]
    [Description("One-way synchronizing cupboard authorization with auto turrets")]
    class AutoTurretAuth : RustPlugin
    {
        #region Initialization

        private static IEnumerable<AutoTurret> turrets;
        private static HashSet<ulong> authorizedPlayers;
        private const string PERSISTENT_AUTHORIZATION = "Use persistent authorization?";
        
        protected override void LoadDefaultConfig()
        {
            Config[PERSISTENT_AUTHORIZATION] = true;
        }

        private void Init()
        {
            if ((bool)Config[PERSISTENT_AUTHORIZATION])
            {
                Unsubscribe(nameof(OnCupboardAuthorize));
                Unsubscribe(nameof(OnCupboardDeauthorize));
                Unsubscribe(nameof(OnCupboardClearList));
            }
            else
            {
                Unsubscribe(nameof(OnTurretTarget));
            }
        }

        #endregion Initialization

        #region Hooks
        
        private object OnTurretTarget(AutoTurret turret, BaseCombatEntity entity)
        {
            var player = entity as BasePlayer;
            if (player == null) return null;
            if (!IsAuthed(player, turret)) return null;
            Auth(turret, player.userID);
            return false;
        }
        
        private void OnEntityBuilt(Planner plan, GameObject go)
        {
            var turret = go.ToBaseEntity() as AutoTurret;
            if (turret == null) return;
            authorizedPlayers = turret.GetBuildingPrivilege()?.authorizedPlayers;
            if (authorizedPlayers == null) return;
            foreach (ulong playerId in authorizedPlayers)
            {
                Auth(turret, playerId);
            }
        }

        private void OnCupboardAuthorize(BuildingPrivlidge privilege, BasePlayer player)
        {
            FindTurrets(privilege.buildingID);
            ServerMgr.Instance.StartCoroutine(AddPlayer(player.userID));
        }
        
        private void OnCupboardDeauthorize(BuildingPrivlidge privilege, BasePlayer player)
        {
            FindTurrets(privilege.buildingID);
            ServerMgr.Instance.StartCoroutine(RemovePlayer(player.userID));
        }
        
        private void OnCupboardClearList(BuildingPrivlidge privilege, BasePlayer player)
        {
            FindTurrets(privilege.buildingID);
            ServerMgr.Instance.StartCoroutine(RemoveAll());
        }

        #endregion Hooks

        #region Helpers

        private static bool IsAuthed(BasePlayer player, BaseEntity turret)
        {
            authorizedPlayers = turret.GetBuildingPrivilege()?.authorizedPlayers;
            return authorizedPlayers != null && authorizedPlayers.Any(playerId => playerId != null && playerId == player.userID);
        }
        
        private static void Auth(AutoTurret turret, ulong playerId)
        {
            turret.authorizedPlayers.Add(playerId);
            turret.SendNetworkUpdate();
        }
		/*
        private static PlayerNameID GetPlayerNameId(BasePlayer player)
        {
            var playerNameId = new PlayerNameID()
            {
                userid = player.userID,
                username = player.displayName
            };
            return playerNameId;
        }
		*/
        private static void FindTurrets(uint buildingId)
        {
            turrets = UnityEngine.Object.FindObjectsOfType<AutoTurret>()
                .Where(x => x.GetBuildingPrivilege()?.buildingID == buildingId);
        }
        
        private static IEnumerator AddPlayer(ulong playerId)
        {
            foreach (AutoTurret turret in turrets)
            {
                AddPlayer(turret, playerId);
                yield return new WaitForFixedUpdate();
            }
        }

        private static void AddPlayer(AutoTurret turret, ulong playerId)
        {
            RemovePlayer(turret, playerId);
            turret.authorizedPlayers.Add(playerId);
            turret.target = null;
            turret.SendNetworkUpdate();
        }
        
        private static IEnumerator RemovePlayer(ulong userId)
        {
            foreach (AutoTurret turret in turrets)
            {
                RemovePlayer(turret, userId);
                yield return new WaitForFixedUpdate();
            }
        }

        private static void RemovePlayer(AutoTurret turret, ulong userId)
        {
            if (turret.authorizedPlayers.RemoveWhere( x => x == userId) == 0) return;
            turret.SendNetworkUpdate();
        }
        
        private static IEnumerator RemoveAll()
        {
            foreach (AutoTurret turret in turrets)
            {
                turret.authorizedPlayers.Clear();
                turret.SendNetworkUpdate();
                yield return new WaitForFixedUpdate();
            }
        }

        #endregion Helpers
    }
}