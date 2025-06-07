using System;
using UnityEngine;
using Random = UnityEngine.Random;

namespace RuntimeScripting
{
    /// <summary>
    /// Placeholder for game-specific logic. In a real project these methods
    /// would interact with the rest of the game systems.
    /// </summary>
    public class GameLogic
    {
        public int HpMin() => 100;
        public int ComboCount()
        {
            _comboCount++;
            return _comboCount;
        }

        private int _comboCount = 0;
        public int Shield() => 0;

        public bool UseResource(string id, int value)
        {
            if (_resourceCount < value)
            {
                Debug.Log($"UseResource {_resourceCount} < {value}");
                return false;
            }
            Debug.Log($"UseResource {_resourceCount} -> {_resourceCount - value}");
            _resourceCount -= value;
            return true;
        }

        public int ResourceCount(string spec) => _resourceCount;
        private int _resourceCount = 1;
        public void AddResource(int value) => _resourceCount += value;
        public int NanikaCount(string spec) => 2;
        public bool NotDebuffed(string target) => true;
        
        public float Interval(float value) => Random.Range(0.1f, value);
        
        public int RandomInt(int min, int max) => Random.Range(min, max);
        
        public float Double(float value) => value * 2f;

        public int EvaluateFunctionInt(FunctionInt func, string[] args)
        {
            switch (func)
            {
                case FunctionInt.HpMin: return HpMin();
                case FunctionInt.ComboCount: return ComboCount();
                case FunctionInt.Shield: return Shield();
                case FunctionInt.NanikaCount: return NanikaCount(args.Length > 0 ? args[0] : string.Empty);
                case FunctionInt.ResourceCount: return ResourceCount(args.Length > 0 ? args[0] : string.Empty);
                case FunctionInt.UseResource: return UseResource(args[0], int.Parse(args[1])) ? 1 : 0;
                default: return 0;
            }
        }

        public int EvaluateFunctionInt(string func, string[] args)
        {
            if (Enum.TryParse(func, out FunctionInt f))
                return EvaluateFunctionInt(f, args);
            return 0;
        }

        /// <summary>
        /// Evaluates a function that returns a floating point value.
        /// </summary>
        /// <param name="func">Function name.</param>
        /// <param name="args">Arguments passed from the script.</param>
        /// <returns>Function result as float.</returns>
        public float EvaluateFunctionFloat(FunctionFloat func, string[] args)
        {
            switch (func)
            {
                case FunctionFloat.Interval:
                    if (args.Length > 0 && float.TryParse(args[0], out float v))
                        return Interval(v);
                    return 0f;
                case FunctionFloat.Double:
                    if (args.Length > 0 && float.TryParse(args[0], out float v1))
                        return Double(v1);
                    return 0f;
                default:
                    return 0f;
            }
        }

        public float EvaluateFunctionFloat(string func, string[] args)
        {
            if (Enum.TryParse(func, out FunctionFloat f))
                return EvaluateFunctionFloat(f, args);
            if (Enum.TryParse(func, out FunctionInt fi))
                return EvaluateFunctionInt(fi, args);
            return 0f;
        }

        // Action methods used by RuntimeTextScriptController
        public void Attack(int value)
        {
            // Debug.Log($"{ResourceCount}");
            Debug.Log($"Attack {value}");
        }

        public void AddPlayerEffect(string targets, string effectId, int value)
        {
            Debug.Log($"Add effect {effectId} {value} to {targets}");
        }

        public void AddPlayerEffectFor(string targets, string effectId, int value, int duration)
        {
            Debug.Log($"Add effect {effectId} {value} for {duration} to {targets}");
        }

        public void RemoveRandomDebuffPlayerEffect(string targets, int count)
        {
            Debug.Log($"Remove {count} debuffs from {targets}");
        }

        public void AddMaxHp(string targets, int value)
        {
            Debug.Log($"Add max hp {value} to {targets}");
        }

        public void SetNanikaEffectFor(string targets, string effectId, int value)
        {
            Debug.Log($"Set nanika effect {effectId} {value} for {targets}");
        }

        public void SpawnNanika(string targets, string nanikaId, int spawnPosId)
        {
            Debug.Log($"Spawn nanika {nanikaId} at {spawnPosId} for {targets}");
        }

        /// <summary>
        /// Creates an executable parameter object from a parsed action.
        /// </summary>
        /// <param name="pa">Parsed action.</param>
        /// <returns>Parameter instance.</returns>
        internal ActionParameter CreateParameter(ParsedAction pa)
        {
            var param = new ActionParameter
            {
                FunctionName = pa.FunctionName
            };
            param.Args.AddRange(pa.Args);
            return param;
        }

        /// <summary>
        /// Executes a pre-parsed action.
        /// </summary>
        /// <param name="param">Action parameter.</param>
        internal void ExecuteAction(ActionParameter param)
        {
            if (Enum.TryParse(param.FunctionName, out FunctionVoid fv))
            {
                switch (fv)
                {
                    case FunctionVoid.Attack:
                        if (param.Args.Count > 0)
                            Attack(ParseIntArg(param.Args[0]));
                        break;
                    case FunctionVoid.AddPlayerEffect:
                        if (param.Args.Count > 2)
                            AddPlayerEffect(param.Args[0], param.Args[1], ParseIntArg(param.Args[2]));
                        break;
                    case FunctionVoid.AddPlayerEffectFor:
                        if (param.Args.Count > 3)
                            AddPlayerEffectFor(param.Args[0], param.Args[1], ParseIntArg(param.Args[2]), ParseIntArg(param.Args[3]));
                        break;
                    case FunctionVoid.RemoveRandomDebuffPlayerEffect:
                        if (param.Args.Count > 1)
                            RemoveRandomDebuffPlayerEffect(param.Args[0], ParseIntArg(param.Args[1]));
                        break;
                    case FunctionVoid.AddMaxHp:
                        if (param.Args.Count > 1)
                            AddMaxHp(param.Args[0], ParseIntArg(param.Args[1]));
                        break;
                    case FunctionVoid.SetNanikaEffectFor:
                        if (param.Args.Count > 2)
                            SetNanikaEffectFor(param.Args[0], param.Args[1], ParseIntArg(param.Args[2]));
                        break;
                    case FunctionVoid.SpawnNanika:
                        if (param.Args.Count > 2)
                            SpawnNanika(param.Args[0], param.Args[1], ParseIntArg(param.Args[2]));
                        break;
                }
            }
            else if (Enum.TryParse(param.FunctionName, out FunctionInt fi))
            {
                param.IntResult = EvaluateFunctionInt(fi, param.Args.ToArray());
            }
            else if (Enum.TryParse(param.FunctionName, out FunctionFloat ff))
            {
                param.FloatResult = EvaluateFunctionFloat(ff, param.Args.ToArray());
            }
            else
            {
                param.FloatResult = EvaluateFunctionFloat(param.FunctionName, param.Args.ToArray());
            }
        }

        private int ParseIntArg(string arg)
        {
            if (int.TryParse(arg, out var value))
                return value;

            return IntExpressionEvaluator.Evaluate(arg, this);
        }
    }
    

    /// <summary>
    /// Functions that perform actions and return no value.
    /// </summary>
    public enum FunctionVoid
    {
        Attack,
        AddPlayerEffect,
        AddPlayerEffectFor,
        RemoveRandomDebuffPlayerEffect,
        AddMaxHp,
        SetNanikaEffectFor,
        SpawnNanika
    }
    
    /// <summary>
    /// Functions returning integer values.
    /// </summary>
    public enum FunctionInt
    {
        HpMin,
        ComboCount,
        Shield,
        NanikaCount,
        ResourceCount,
        UseResource,
    }

    /// <summary>
    /// Functions returning floating point values.
    /// </summary>
    public enum FunctionFloat
    {
        Interval,
        Double
    }
}