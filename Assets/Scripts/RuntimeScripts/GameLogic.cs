using System;

namespace RuntimeScripting
{
    /// <summary>
    /// Placeholder for game-specific logic. In a real project these methods
    /// would interact with the rest of the game systems.
    /// </summary>
    public static class GameLogic
    {
        public static int HpMin() => 100;
        public static int ComboCount() => 0;
        public static int Shield() => 0;
        public static bool UseResource(string id, int value) => true;
        public static int NanikaCount(string spec) => 0;
        public static bool NotDebuffed(string target) => true;

        public static int EvaluateFunctionInt(string func, string[] args)
        {
            switch (func)
            {
                case "HpMin": return HpMin();
                case "ComboCount": return ComboCount();
                case "Shield": return Shield();
                case "NanikaCount": return NanikaCount(args.Length > 0 ? args[0] : string.Empty);
                case "UseResource": return UseResource(args[0], int.Parse(args[1])) ? 1 : 0;
                case "NotDebuffed": return NotDebuffed(args[0]) ? 1 : 0;
                default: return 0;
            }
        }

        // Action methods used by RuntimeTextScriptController
        public static void Attack(int value)
        {
            Console.WriteLine($"Attack {value}");
        }

        public static void AddPlayerEffect(string targets, string effectId, int value)
        {
            Console.WriteLine($"Add effect {effectId} {value} to {targets}");
        }

        public static void AddPlayerEffectFor(string targets, string effectId, int value, int duration)
        {
            Console.WriteLine($"Add effect {effectId} {value} for {duration} to {targets}");
        }

        public static void RemoveRandomDebuffPlayerEffect(string targets, int count)
        {
            Console.WriteLine($"Remove {count} debuffs from {targets}");
        }

        public static void AddMaxHp(string targets, int value)
        {
            Console.WriteLine($"Add max hp {value} to {targets}");
        }

        public static void SetNanikaEffectFor(string targets, string effectId, int value)
        {
            Console.WriteLine($"Set nanika effect {effectId} {value} for {targets}");
        }

        public static void SpawnNanika(string targets, string nanikaId, int spawnPosId)
        {
            Console.WriteLine($"Spawn nanika {nanikaId} at {spawnPosId} for {targets}");
        }
    }
}