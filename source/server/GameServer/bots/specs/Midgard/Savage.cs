using System;
using System.Collections.Generic;

namespace DOL.GS
{
    public class SavageBotSpec : BotSpec
    {
        public SavageBotSpec(eSpecType spec, eObjectType preferredWeapon = 0)
        {
            SpecName = "SavageBotSpec";

            if (preferredWeapon is eObjectType.Sword or eObjectType.Axe or eObjectType.Hammer or eObjectType.HandToHand)
                WeaponOneType = preferredWeapon;
            else
            {
                var randBaseWeap = spec switch
                {
                    eSpecType.TwoHanded => Util.Random(0, 2),
                    eSpecType.Mid => Util.Random(0, 2),
                    eSpecType.DualWield => Util.Random(3, 4),
                    _ => Util.Random(4),
                };

                WeaponOneType = randBaseWeap switch
                {
                    0 => eObjectType.Sword,
                    1 => eObjectType.Axe,
                    2 => eObjectType.Hammer,
                    _ => eObjectType.HandToHand,
                };
            }

            if (WeaponOneType != eObjectType.HandToHand)
            {
                Is2H = true;
                SpecType = eSpecType.Mid;
            }
            else
                SpecType = eSpecType.DualWield;

            int randVariance = Util.Random(3);

            switch (randVariance)
            {
                case 0:
                Add(ObjToSpec(WeaponOneType), 44, 1.0f);
                Add(Specs.Savagery, 49, 0.75f);
                Add(Specs.Parry, 4, 0.0f);
                break;

                case 1:
                Add(ObjToSpec(WeaponOneType), 39, 1.0f);
                Add(Specs.Savagery, 49, 0.75f);
                Add(Specs.Parry, 20, 0.1f);
                break;

                case 2:
                Add(ObjToSpec(WeaponOneType), 44, 1.0f);
                Add(Specs.Savagery, 48, 0.75f);
                Add(Specs.Parry, 10, 0.1f);
                break;

                case 3:
                Add(ObjToSpec(WeaponOneType), 50, 1.0f);
                Add(Specs.Savagery, 42, 0.75f);
                Add(Specs.Parry, 9, 0.1f);
                break;
            }
        }

        /// <summary>
        /// Keeps a persistent Savage on the weapon line it already trained.
        /// The generic TwoHanded profile covers three independent Midgard lines;
        /// rerolling that choice on every restart could equip Axe while all saved
        /// points and styles remained Sword (or vice versa).
        /// </summary>
        public static eObjectType WeaponFromPersistedSpecs(string serialized)
        {
            if (string.IsNullOrWhiteSpace(serialized))
                return 0;

            Dictionary<string, eObjectType> weapons = new(StringComparer.OrdinalIgnoreCase)
            {
                [Specs.Sword] = eObjectType.Sword,
                [Specs.Axe] = eObjectType.Axe,
                [Specs.Hammer] = eObjectType.Hammer,
                [Specs.HandToHand] = eObjectType.HandToHand,
            };
            int bestLevel = 0;
            eObjectType best = 0;
            foreach (string entry in serialized.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                string[] parts = entry.Split('|', 2);
                if (parts.Length != 2 || !weapons.TryGetValue(parts[0], out eObjectType weapon) ||
                    !int.TryParse(parts[1], out int level) || level <= bestLevel)
                    continue;
                bestLevel = level;
                best = weapon;
            }
            return best;
        }
    }
}
