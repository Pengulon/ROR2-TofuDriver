using TofuDriverMod.SkillStates;
using TofuDriverMod.SkillStates.BaseStates;
using System.Collections.Generic;
using System;

namespace TofuDriverMod.Modules
{
    public static class States
    {
        internal static List<Type> entityStates = new List<Type>();

        internal static void RegisterStates()
        {
            entityStates.Add(typeof(BaseMeleeAttack));
            entityStates.Add(typeof(SlashCombo));

            entityStates.Add(typeof(Shoot));

            entityStates.Add(typeof(Roll));

            entityStates.Add(typeof(ThrowBomb));
        }

        internal static class ActivationStateName
        {
            public static readonly string AllowMovement = "Weapon";
            public static readonly string DisallowMovement = "Body";
        }
    }
}