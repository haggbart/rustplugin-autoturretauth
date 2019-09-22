using ProtoBuf;

namespace Oxide.Plugins
{
    [Info("Auto Turret Authorization", "haggbart", "1.0.3")]
    [Description("Players authorized on the cupboard are added to the auto turret.")]
    class AutoTurretAuth : RustPlugin
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
            BuildingPrivlidge buildingPrivilege = entity.GetBuildingPrivilege();
            if (buildingPrivilege == null) return false;
            foreach (PlayerNameID playerNameId in buildingPrivilege.authorizedPlayers)
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