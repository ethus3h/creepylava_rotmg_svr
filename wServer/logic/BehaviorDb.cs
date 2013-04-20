using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using wServer.realm;
using wServer.realm.entities;
using wServer.logic.loot;
using System.Threading;
using System.Reflection;

namespace wServer.logic
{
    public partial class BehaviorDb
    {
        public RealmManager Manager { get; private set; }

        static int initializing;
        internal static BehaviorDb InitDb;
        internal static XmlData InitGameData { get { return InitDb.Manager.GameData; } }

        public BehaviorDb(RealmManager manager)
        {
            this.Manager = manager;

            Definitions = new Dictionary<short, Tuple<State, Loot>>();

            if (Interlocked.Exchange(ref initializing, 1) == 1)
                throw new InvalidOperationException("Attempted to initialize multiple BehaviorDb at the same time.");
            InitDb = this;
            foreach (var field in GetType().GetFields(BindingFlags.NonPublic | BindingFlags.Instance))
                if (field.FieldType == typeof(_))
                {
                    ((_)field.GetValue(this))();
                    field.SetValue(this, null);
                }
            InitDb = null;
            initializing = 0;
        }

        public void ResolveBehavior(Entity entity)
        {
            Tuple<State, Loot> def;
            if (Definitions.TryGetValue(entity.ObjectType, out def))
                entity.SwitchTo(def.Item1);
        }

        delegate ctor _();
        struct ctor
        {
            public ctor Init(string objType, State rootState, params ILootDef[] defs)
            {
                var d = new Dictionary<string, State>();
                rootState.Resolve(d);
                rootState.ResolveChildren(d);
                var dat = InitDb.Manager.GameData;
                if (defs.Length > 0)
                {
                    var loot = new Loot(defs);
                    rootState.Death += (sender, e) => loot.Handle((Enemy)e.Host, e.Time);
                    InitDb.Definitions.Add(dat.IdToType[objType], new Tuple<State, Loot>(rootState, loot));
                }
                else
                    InitDb.Definitions.Add(dat.IdToType[objType], new Tuple<State, Loot>(rootState, null));
                return this;
            }
        }
        static ctor Behav()
        {
            return new ctor();
        }

        public Dictionary<short, Tuple<State, Loot>> Definitions { get; private set; }

    }
}
