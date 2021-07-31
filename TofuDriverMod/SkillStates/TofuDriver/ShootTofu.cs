using EntityStates;
using RoR2;
using RoR2.Projectile;
using UnityEngine;

namespace TofuDriverMod.SkillStates
{
    class ShootTofu : BaseSkillState
    {
        public float baseDuration = 0.5f;
        private float duration;
        private float damageCoefficient = 16f;
        private float throwForce = 80f;
        public override void OnEnter()
        {
            base.OnEnter();
            this.duration = this.baseDuration / base.attackSpeedStat;
            Ray aimRay = base.GetAimRay();
            base.StartAimMode(aimRay, 2f, false);
            //base.PlayAnimation("Gesture, Override", "FireShotgun", "FireShotgun.playbackRate", this.duration * 1.1f);
            if (base.isAuthority)
            {
                ProjectileManager.instance.FireProjectile(Modules.Projectiles.tofuPrefab,
                    aimRay.origin,
                    Util.QuaternionSafeLookRotation(aimRay.direction),
                    base.gameObject,
                    this.damageCoefficient * this.damageStat,
                    4000f,
                    base.RollCrit(),
                    DamageColorIndex.Default,
                    null,
                    this.throwForce);
            }
        }
        public override void OnExit()
        {
            base.OnExit();
        }
        public override void FixedUpdate()
        {
            base.FixedUpdate();
            if (base.fixedAge >= this.duration && base.isAuthority)
            {
                this.outer.SetNextStateToMain();
                return;
            }
        }

        public override InterruptPriority GetMinimumInterruptPriority()
        {
            return InterruptPriority.Skill;
        }
    }
}
