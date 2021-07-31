using EntityStates;
using RoR2;
using RoR2.Audio;
using UnityEngine;
using System;

namespace TofuDriverMod.SkillStates
{
    class Drift : BaseSkillState
    {
        // public GameObject effectPrefab = Resources.Load<GameObject>("prefabs/effects/impacteffects/Hitspark");
        public GameObject hitEffectPrefab = Resources.Load<GameObject>("prefabs/effects/impacteffects/critspark");
        private float straightThreshold = 10.0f;
        private float initialLaunchForce = 10f;
        private Vector3 driftAccel = Vector3.zero;
        private float driftSpeed = 0.3f;
        private float brakeSpeed = 0.1f;

        private HitBoxGroup hitBoxGroup = null;
        private string hitboxName = "Drift";

        protected DamageType damageType = DamageType.Generic;
        protected float damageCoefficient = 3.5f;
        protected float procCoefficient = 1f;
        protected float pushForce = 300f;
        protected Vector3 bonusForce = Vector3.zero;
        protected float baseDuration = 0.5f;
        protected float attackStartTime = 0.1f;
        protected float attackEndTime = 0.4f;
        protected float baseEarlyExitTime = 0.2f;
        protected float hitStopDuration = 0.012f;
        protected float attackRecoil = 0.75f;
        // protected float hitHopVelocity = 4f;
        protected bool cancelled = false;

        protected NetworkSoundEventIndex impactSound;

        private float earlyExitTime;
        public float duration;
        private bool hasFired;
        private float hitPauseTimer;
        private OverlapAttack attack;
        protected bool inHitPause;
        private bool hasHopped;
        protected float stopwatch;
        protected Animator animator;
        private BaseState.HitStopCachedState hitStopCachedState;
        private Vector3 storedVelocity;

        public override void OnEnter()
        {
            base.OnEnter();
            this.duration = this.baseDuration / base.attackSpeedStat;
            Ray aimRay = base.GetAimRay();
            // base.StartAimMode(aimRay, 2f, false);
            //base.PlayAnimation("Gesture, Override", "FireShotgun", "FireShotgun.playbackRate", this.duration * 1.1f);
            if (base.isAuthority)
            {
                Vector3 flatLookDirection = Vector3.ProjectOnPlane(aimRay.direction, base.characterBody.transform.up).normalized;
                float angleBetween = Vector3.Angle(flatLookDirection, base.characterMotor.moveDirection);
                Vector3 LaunchDirection = flatLookDirection;
                //check if straight with threshold
                if (Mathf.Abs(angleBetween) > straightThreshold)
                {
                    Debug.Log("Second Skill going straight, angle found: " + angleBetween.ToString());
                    driftAccel = LaunchDirection.normalized * -driftSpeed;
                }
                else
                {
                    bool left = angleBetween < 0;
                    Debug.Log("Second Skill going to curve " + (left ? "LEFT" : "RIGHT"));
                    LaunchDirection = Quaternion.Euler(0, 0, (left ? -45 : 45)) * LaunchDirection;
                    driftAccel = Quaternion.Euler(0, 0, (left ? 90 : -90)) * LaunchDirection * driftSpeed;
                }
                base.characterMotor.ApplyForce(LaunchDirection * initialLaunchForce);

                this.earlyExitTime = this.baseEarlyExitTime / this.attackSpeedStat;
                this.hasFired = false;
                this.animator = base.GetModelAnimator();
                //base.characterBody.outOfCombatStopwatch = 0f;
                this.animator.SetBool("attacking", true);

                Transform modelTransform = base.GetModelTransform();
                if (modelTransform)
                {
                    hitBoxGroup = Array.Find<HitBoxGroup>(modelTransform.GetComponents<HitBoxGroup>(), (HitBoxGroup element) => element.groupName == this.hitboxName);
                }
                this.attack = new OverlapAttack();
                this.attack.damageType = this.damageType;
                this.attack.attacker = base.gameObject;
                this.attack.inflictor = base.gameObject;
                this.attack.teamIndex = base.GetTeam();
                this.attack.damage = this.damageCoefficient * this.damageStat;
                this.attack.procCoefficient = this.procCoefficient;
                this.attack.hitEffectPrefab = this.hitEffectPrefab;
                this.attack.forceVector = this.bonusForce;
                this.attack.pushAwayForce = this.pushForce;
                this.attack.hitBoxGroup = hitBoxGroup;
                this.attack.isCrit = base.RollCrit();
                this.attack.impactSound = this.impactSound;

            }
        }
        protected virtual void OnHitEnemyAuthority()
        {
            //Util.PlaySound(this.hitSoundString, base.gameObject);

            if (!this.inHitPause && this.hitStopDuration > 0f)
            {
                this.storedVelocity = base.characterMotor.velocity;
                //this.hitStopCachedState = base.CreateHitStopCachedState(base.characterMotor, this.animator, "Slash.playbackRate");
                this.hitPauseTimer = this.hitStopDuration / this.attackSpeedStat;
                this.inHitPause = true;
            }
        }
        public override void OnExit()
        {
            base.OnExit();
        }
        private void FireAttack()
        {
            if (base.isAuthority)
            {
                if (this.attack.Fire())
                {
                    this.OnHitEnemyAuthority();
                }
            }
        }
        public override void FixedUpdate()
        {
            base.FixedUpdate();

            this.hitPauseTimer -= Time.fixedDeltaTime;

            if (this.hitPauseTimer <= 0f && this.inHitPause)
            {
                base.ConsumeHitStopCachedState(this.hitStopCachedState, base.characterMotor, this.animator);
                this.inHitPause = false;
                base.characterMotor.velocity = this.storedVelocity;
            }

            if (!this.inHitPause)
            {
                this.stopwatch += Time.fixedDeltaTime;
                //update curve velocity
                base.characterMotor.velocity += driftAccel;
                //slow down velocity
                base.characterMotor.velocity -= base.characterMotor.velocity.normalized * brakeSpeed;

            }
            else
            {
                if (base.characterMotor) base.characterMotor.velocity = Vector3.zero;
                //if (this.animator) this.animator.SetFloat("Swing.playbackRate", 0f);
            }

            if (this.stopwatch >= (this.duration * this.attackStartTime) && this.stopwatch <= (this.duration * this.attackEndTime))
            {
                this.FireAttack();
            }


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
