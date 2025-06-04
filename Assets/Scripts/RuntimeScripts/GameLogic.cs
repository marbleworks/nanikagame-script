using System;

namespace RuntimeScripting
{
    /// <summary>
    /// Placeholder for game-specific logic. In a real project these methods
    /// would interact with the rest of the game systems.
    /// </summary>
    public class GameLogic
    {
        public int HpMin() => 100;
        public int ComboCount() => 0;
        public int Shield() => 0;
        public bool UseResource(string id, int value) => true;
        public int NanikaCount(string spec) => 0;
        public bool NotDebuffed(string target) => true;

        public int EvaluateFunctionInt(string func, string[] args)
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

        /// <summary>
        /// Evaluates a function that returns a floating point value.
        /// </summary>
        /// <param name="func">Function name.</param>
        /// <param name="args">Arguments passed from the script.</param>
        /// <returns>Function result as float.</returns>
        public float EvaluateFunctionFloat(string func, string[] args)
        {
            switch (func)
            {
                case "Interval":
                    if (args.Length > 0 && float.TryParse(args[0], out float v))
                        return v;
                    return 0f;
                default:
                    return EvaluateFunctionInt(func, args);
            }
        }

        // Action methods used by RuntimeTextScriptController
        public void Attack(int value)
        {
            Console.WriteLine($"Attack {value}");
        }

        public void AddPlayerEffect(string targets, string effectId, int value)
        {
            Console.WriteLine($"Add effect {effectId} {value} to {targets}");
        }

        public void AddPlayerEffectFor(string targets, string effectId, int value, int duration)
        {
            Console.WriteLine($"Add effect {effectId} {value} for {duration} to {targets}");
        }

        public void RemoveRandomDebuffPlayerEffect(string targets, int count)
        {
            Console.WriteLine($"Remove {count} debuffs from {targets}");
        }

        public void AddMaxHp(string targets, int value)
        {
            Console.WriteLine($"Add max hp {value} to {targets}");
        }

        public void SetNanikaEffectFor(string targets, string effectId, int value)
        {
            Console.WriteLine($"Set nanika effect {effectId} {value} for {targets}");
        }

        public void SpawnNanika(string targets, string nanikaId, int spawnPosId)
        {
            Console.WriteLine($"Spawn nanika {nanikaId} at {spawnPosId} for {targets}");
        }
    }
}