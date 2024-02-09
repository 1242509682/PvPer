using TShockAPI;
using Terraria;
using Terraria.ID;
using Microsoft.Xna.Framework;
using Org.BouncyCastle.Bcpg;

namespace PvPer
{
    public class Pair
    {
        public int Player1, Player2;

        public Pair(int player1, int player2)
        {
            Player1 = player1;
            Player2 = player2;
        }

        public override bool Equals(object? obj)
        {
            if (obj == null)
            {
                return false;
            }

            Pair other = (Pair)obj;

            return Player1 == other.Player1 && Player2 == other.Player2 || Player1 == other.Player2 && Player2 == other.Player1;
        }

        public override int GetHashCode()
        {
            return Player1 << 16 | Player2;
        }

        public void EndDuel(int winner)
        {
            int loser = winner == Player1 ? Player2 : Player1;
            TSPlayer.All.SendMessage($"{TShock.Players[winner].Name} has won against {TShock.Players[loser].Name}!", 255, 204, 255);
            PvPer.ActiveDuels.Remove(this);
            TShock.Players[winner].SetPvP(false);
            TShock.Players[loser].SetPvP(false);
            
            Task.Run(async () =>
            {
                int p = Projectile.NewProjectile(Projectile.GetNoneSource(), TShock.Players[winner].TPlayer.position.X + 16, 
                    TShock.Players[winner].TPlayer.position.Y - 64f, 0f, -8f, ProjectileID.RocketFireworkGreen, 0, 0);
				Main.projectile[p].Kill();
                await Task.Delay(5000);
                TShock.Players[winner].Teleport(Main.spawnTileX * 16, Main.spawnTileY * 16);
            });
        }

        public void StartDuel()
        {
            TSPlayer plr1 = TShock.Players[Player1];
            TSPlayer plr2 = TShock.Players[Player2];

            if (plr1 != null && plr2 != null)
            {
                if (!plr1.Active || !plr2.Active)
                {
                    plr1.SendErrorMessage("Duel has been cancelled because one of the participants is not online.");
                    plr2.SendErrorMessage("Duel has been cancelled because one of the participants is not online.");
                    PvPer.Invitations.Remove(this);
                    return;
                }

                if (plr1.Dead || plr2.Dead)
                {
                    plr1.SendErrorMessage("Duel has been cancelled because one of the participants is dead.");
                    plr2.SendErrorMessage("Duel has been cancelled because one of the participants is dead.");
                    PvPer.Invitations.Remove(this);
                    return;
                }
            }
            else
            {
                PvPer.Invitations.Remove(this);
                return;
            }

            // Move the pair to active duels
            PvPer.Invitations.Remove(this);
            PvPer.ActiveDuels.Add(this);

            // Teleport and buffs
            plr1.Teleport(PvPer.Config.Player1PositionX * 16, PvPer.Config.Player1PositionY * 16);
            plr2.Teleport(PvPer.Config.Player2PositionX * 16, PvPer.Config.Player2PositionY * 16);


            plr1.SetBuff(BuffID.Webbed, 60 * 5);
            plr2.SetBuff(BuffID.Webbed, 60 * 5);

            float gameModeFactor = 1;
            if (Main.GameMode == GameModeID.Master)
            {
                gameModeFactor = 2.5f;
            }
            else if (Main.GameMode == GameModeID.Expert)
            {
                gameModeFactor = 2f;
            }

            plr1.SetBuff(BuffID.Cursed, (int)(60 * 5 / gameModeFactor));
            plr2.SetBuff(BuffID.Cursed, (int)(60 * 5 / gameModeFactor));
            plr1.SetPvP(false);
            plr2.SetPvP(false);
            plr1.SetTeam(0);
            plr2.SetTeam(0);

            // Countdown and set pvp mode of each player
            Task.Run(async () =>
            {
                NetMessage.SendData((int)PacketTypes.CreateCombatTextExtended, Player1, -1,
                    Terraria.Localization.NetworkText.FromLiteral("Duel starting in..."), (int)new Color(255, 204, 255).PackedValue,
                    plr1.X + 16, plr1.Y - 16);

                NetMessage.SendData((int)PacketTypes.CreateCombatTextExtended, Player2, -1,
                    Terraria.Localization.NetworkText.FromLiteral("Duel starting in..."), (int)new Color(255, 204, 255).PackedValue,
                    plr2.X + 16, plr2.Y - 16);

                for (int i = 1; i <= 5; i++)
                {
                    NetMessage.SendData((int)PacketTypes.CreateCombatTextExtended, Player1, -1,
                        Terraria.Localization.NetworkText.FromLiteral(i.ToString()), (int)new Color(255, 204, 255).PackedValue,
                        plr1.X + 16, plr1.Y - 16);

                    NetMessage.SendData((int)PacketTypes.CreateCombatTextExtended, Player2, -1,
                        Terraria.Localization.NetworkText.FromLiteral(i.ToString()), (int)new Color(255, 204, 255).PackedValue,
                        plr2.X + 16, plr2.Y - 16);

                    await Task.Delay(1000);
                }

                NetMessage.SendData((int)PacketTypes.CreateCombatTextExtended, Player1, -1,
                    Terraria.Localization.NetworkText.FromLiteral("GO!!"), (int)new Color(255, 204, 255).PackedValue,
                    plr1.X + 16, plr1.Y - 16);

                NetMessage.SendData((int)PacketTypes.CreateCombatTextExtended, Player2, -1,
                    Terraria.Localization.NetworkText.FromLiteral("Go!!"), (int)new Color(255, 204, 255).PackedValue,
                    plr2.X + 16, plr2.Y - 16);

                plr1.TPlayer.hostile = true;
                plr2.TPlayer.hostile = true;
                NetMessage.SendData((int)PacketTypes.TogglePvp, Player1, -1, Terraria.Localization.NetworkText.Empty, Player1);
                NetMessage.SendData((int)PacketTypes.TogglePvp, Player1, -1, Terraria.Localization.NetworkText.Empty, Player2);
                NetMessage.SendData((int)PacketTypes.TogglePvp, Player2, -1, Terraria.Localization.NetworkText.Empty, Player1);
                NetMessage.SendData((int)PacketTypes.TogglePvp, Player2, -1, Terraria.Localization.NetworkText.Empty, Player2);
            });
        }
    }
}