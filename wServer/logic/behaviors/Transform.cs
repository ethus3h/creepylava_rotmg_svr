using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using wServer.realm;

namespace wServer.logic.behaviors
{
    class Transform : Behavior
    {
        //State storage: none

        short target;
        public Transform(string target)
        {
            this.target = BehaviorDb.InitGameData.IdToType[target];
        }

        protected override void TickCore(Entity host, RealmTime time, ref object state)
        {
            Entity entity = Entity.Resolve(host.Manager, target);

            entity.Move(host.X, host.Y);
            host.Owner.EnterWorld(entity);
            host.Owner.LeaveWorld(host);
        }
    }
}
