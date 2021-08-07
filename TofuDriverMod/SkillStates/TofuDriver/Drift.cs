using EntityStates;
using RoR2;
using RoR2.Audio;
using UnityEngine;
using UnityEngine.Networking;
using System;

namespace TofuDriverMod.SkillStates
{
    class Drift : BaseSkillState
    {
        // public GameObject effectPrefab = Resources.Load<GameObject>("prefabs/effects/impacteffects/Hitspark");
        public GameObject hitEffectPrefab = Resources.Load<GameObject>("prefabs/effects/impacteffects/critspark");

        private readonly float straightAngleThreshold = 10.0f;
        private readonly float initialLaunchForce = 10f;

        private float driftAccel = 0.0f;
        private readonly float driftTurnSpeed = 4.20f;

        private HitBoxGroup hitBoxGroup = null;
        private readonly string hitboxName = "Drift";

        protected DamageType damageType = DamageType.Generic;
        protected float damageCoefficient = 3.5f;
        protected float procCoefficient = 1f;
        protected float pushForce = 1000f;
        protected Vector3 bonusForce = Vector3.zero;

        //total skill duration
        protected float baseDuration = 0.4f;

        //out of 1.0, time percent of duration
        protected float attackStartTime = 0.1f;
        protected float attackEndTime = 0.9f;

        protected float hitStopDuration = 0.2f;
        protected bool cancelled = false;

        //protected NetworkSoundEventIndex impactSound;

        public float duration;
        private float hitPauseTimer;
        private OverlapAttack attack;
        protected bool inHitPause;
        protected float stopwatch;
        protected Animator animator;
        private BaseState.HitStopCachedState hitStopCachedState;
        private Vector3 storedVelocity;

        float lastLaunchForce;
        //private Vector3 driftVelocity;

        public override void OnEnter()
        {
            base.OnEnter();
            this.duration = this.baseDuration / base.attackSpeedStat;
            this.inHitPause = false;
            Ray aimRay = base.GetAimRay();
            // base.StartAimMode(aimRay, 2f, false);
            //base.PlayAnimation("Gesture, Override", "FireShotgun", "FireShotgun.playbackRate", this.duration * 1.1f);
            if (base.isAuthority)
            {
                Vector3 forwardDirection = ((base.inputBank.moveVector == Vector3.zero) ? base.characterDirection.forward : base.inputBank.moveVector).normalized;
                Vector3 charUp = getCharUp();

                Vector3 flatLookDirection = Vector3.ProjectOnPlane(aimRay.direction, charUp).normalized;
                float angleBetween = Vector3.SignedAngle(flatLookDirection, forwardDirection, charUp);
                Vector3 launchDirection = forwardDirection;
                float calculatedLaunchForce = this.initialLaunchForce * this.moveSpeedStat;
                //check if straight with threshold
                if (Mathf.Abs(angleBetween) < straightAngleThreshold)
                {
                    Debug.Log("Second Skill going straight, angle found: " + angleBetween.ToString());
                    //lessen launch force since only going forward
                    this.driftAccel = 0.0f;
                    calculatedLaunchForce = this.initialLaunchForce * this.moveSpeedStat * 0.5f;
                }
                else
                {
                    bool left = angleBetween < 0;
                    //Debug.Log("Second Skill going to curve " + (left ? "LEFT" : "RIGHT"));
                    this.driftAccel = this.driftTurnSpeed * (left ? 1 : -1) * this.moveSpeedStat * 0.1f;
                }

                //set initial drift velocity
                //this.driftVelocity = launchDirection.normalized * calculatedLaunchForce;
                // Debug.Log("Initial Drift Velocity: " + this.driftVelocity.ToString());

                if (base.characterMotor && base.characterDirection)
                {
                    base.characterMotor.velocity.y = 0f;
                    //base.characterMotor.velocity = this.driftVelocity;
                    base.characterMotor.velocity = launchDirection.normalized * calculatedLaunchForce;
                    this.lastLaunchForce = calculatedLaunchForce;
                }

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
                //this.attack.impactSound = this.impactSound;

                if (NetworkServer.active)
                {
                    base.characterBody.AddTimedBuff(Modules.Buffs.armorBuff, 3f * this.duration);
                    base.characterBody.AddTimedBuff(RoR2Content.Buffs.HiddenInvincibility, 0.5f * this.duration);
                }

            }
        }
        protected virtual void OnHitEnemyAuthority()
        {
            //Util.PlaySound(this.hitSoundString, base.gameObject);

            if (!this.inHitPause && this.hitStopDuration > 0f)
            {
                this.storedVelocity = base.characterMotor.velocity;
                this.hitStopCachedState = base.CreateHitStopCachedState(base.characterMotor, this.animator, "Slash.playbackRate");
                this.hitPauseTimer = this.hitStopDuration / this.attackSpeedStat;
                this.inHitPause = true;
                this.stopwatch = 0f;
                Debug.Log("Hit enemy with velocity: " + this.storedVelocity.ToString());
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
                Debug.Log("Ended hit pause");
            }

            if (!this.inHitPause)
            {
                this.stopwatch += Time.fixedDeltaTime;

                Vector3 flattenedVelocity = Vector3.ProjectOnPlane(base.characterMotor.velocity, getCharUp()).normalized;
                float durationPercentLeft = (1 - this.stopwatch / this.duration);

                // add drift accel for curve
                Vector3 driftVector = Quaternion.Euler(0, 90, 0) * flattenedVelocity.normalized * this.driftAccel * durationPercentLeft;
                base.characterMotor.velocity += driftVector;
                // subtract ending velocity with brake speed reverse of 
                Vector3 brakeVector = flattenedVelocity.normalized * Math.Abs(this.driftAccel) * durationPercentLeft;
                base.characterMotor.velocity -= brakeVector;
            }
            else
            {
                if (base.characterMotor) base.characterMotor.velocity = Vector3.zero;
                if (this.animator) this.animator.SetFloat("Swing.playbackRate", 0f);
                Debug.Log("In hit pause");
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

        private Vector3 getCharUp()
        {
            return base.characterDirection ? base.characterDirection.transform.up : Vector3.up;
        }
    }
}
