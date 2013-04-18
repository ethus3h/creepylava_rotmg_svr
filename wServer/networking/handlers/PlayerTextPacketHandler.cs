using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using wServer.networking.cliPackets;
using wServer.realm;
using wServer.networking.svrPackets;
using db;
using wServer.realm.entities;
using wServer.realm.entities.player.commands;

namespace wServer.networking.handlers
{
    class PlayerTextPacketHandler : PacketHandlerBase<PlayerTextPacket>
    {
        public override PacketID ID { get { return PacketID.PlayerText; } }

        protected override void HandlePacket(Client client, PlayerTextPacket packet)
        {
            client.Manager.Logic.AddPendingAction(t => Handle(client.Player, t, packet.Text));
        }

        void Handle(Player player, RealmTime time, string text)
        {
            if (player.Owner == null) return;

            if (text[0] == '/')
            {
                CommandManager.Execute(player, time, text);
            }
            else
                player.Owner.BroadcastPacket(new TextPacket()
                {
                    Name = (player.Client.Account.Admin ? "@" : "") + player.Name,
                    ObjectId = player.Id,
                    Stars = player.Stars,
                    BubbleTime = 5,
                    Recipient = "",
                    Text = text,
                    CleanText = text
                }, null);
        }
    }
}
