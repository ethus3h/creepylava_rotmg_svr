using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using db;
using System.Threading;
using System.Diagnostics;
using System.IO;
using wServer.realm.worlds;
using System.Collections.Concurrent;
using wServer.networking;
using System.Globalization;
using wServer.logic;

namespace wServer.realm
{
    public struct RealmTime
    {
        public long tickCount;
        public long tickTimes;
        public int thisTickCounts;
        public int thisTickTimes;
    }
    public class TimeEventArgs : EventArgs
    {
        public TimeEventArgs(RealmTime time)
        {
            this.Time = time;
        }
        public RealmTime Time { get; private set; }
    }

    public enum PendingPriority
    {
        Emergent,
        Destruction,
        Normal,
        Creation,
    }

    public class RealmManager
    {
        public int MAX_CLIENT = Program.Settings.GetValue<int>("maxClient", "100");

        int nextWorldId = 0;
        public readonly ConcurrentDictionary<int, World> Worlds = new ConcurrentDictionary<int, World>();
        public readonly ConcurrentDictionary<int, Client> Clients = new ConcurrentDictionary<int, Client>();
        public ConcurrentDictionary<int, World> PlayerWorldMapping = new ConcurrentDictionary<int, World>();

        public RealmPortalMonitor Monitor { get; private set; }

        public bool TryConnect(Client client)
        {
            if (Clients.Count >= MAX_CLIENT)
                return false;
            else
                return Clients.TryAdd(client.Account.AccountId, client);
        }
        public void Disconnect(Client client)
        {
            Clients.TryRemove(client.Account.AccountId, out client);
        }

        public World AddWorld(int id, World world)
        {
            if (world.Manager != null)
                throw new InvalidOperationException("World already added.");
            world.Id = id;
            Worlds[id] = world;
            OnWorldAdded(world);
            return world;
        }
        public World AddWorld(World world)
        {
            if (world.Manager != null)
                throw new InvalidOperationException("World already added.");
            world.Id = Interlocked.Increment(ref nextWorldId);
            Worlds[world.Id] = world;
            OnWorldAdded(world);
            return world;
        }

        public bool RemoveWorld(World world)
        {
            if (world.Manager == null)
                throw new InvalidOperationException("World is not added.");
            if (Worlds.TryRemove(world.Id, out world))
            {
                OnWorldRemoved(world);
                return true;
            }
            else
                return false;
        }

        public World GetWorld(int id)
        {
            World ret;
            if (!Worlds.TryGetValue(id, out ret)) return null;
            if (ret.Id == 0) return null;
            return ret;
        }


        void OnWorldAdded(World world)
        {
            world.Manager = this;
            if (world is GameWorld)
                Monitor.WorldAdded(world);
        }

        void OnWorldRemoved(World world)
        {
            world.Manager = null;
            if (world is GameWorld)
                Monitor.WorldRemoved(world);
        }

        Thread network;
        public NetworkTicker Network { get; private set; }

        Thread logic;
        public LogicTicker Logic { get; private set; }

        public XmlData GameData { get; private set; }
        public BehaviorDb Behaviors { get; private set; }
        public Database Database { get; private set; }

        public void Initialize()
        {
            this.GameData = new XmlData();
            this.Behaviors = new BehaviorDb(this);

            AddWorld(World.NEXUS_ID, Worlds[0] = new Nexus());
            Monitor = new RealmPortalMonitor(this);

            AddWorld(World.TUT_ID, new Tutorial(true));
            AddWorld(World.NEXUS_LIMBO, new NexusLimbo());
            AddWorld(World.VAULT_ID, new Vault(true));
            AddWorld(World.TEST_ID, new Test());
            AddWorld(World.RAND_REALM, new RandomRealm());
            AddWorld(World.GAUNTLET, new GauntletMap());

            AddWorld(new GameWorld(1, "Medusa", true));
        }

        public void Run()
        {
            Database = new Database(Program.Settings.GetValue("conn"));
            Network = new NetworkTicker(this);
            Logic = new LogicTicker(this);
            network = new Thread(Network.TickLoop)
            {
                Name = "Network Process Thread",
                CurrentCulture = CultureInfo.InvariantCulture
            };
            logic = new Thread(Logic.TickLoop)
            {
                Name = "Logic Ticking Thread",
                CurrentCulture = CultureInfo.InvariantCulture
            };
            //Start logic loop first
            logic.Start();
            network.Start();
        }

        public bool Terminating { get; private set; }
        public void Stop()
        {
            Terminating = true;
            Database.Dispose();
        }
    }
}
