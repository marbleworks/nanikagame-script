using System;
using System.Linq;
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
        #region Fields
        private int _comboCount;
        private int _resourceCount = 1;
        #endregion

        #region Public Functions

        public int HpMin() => 100;

        public int ComboCount() => ++_comboCount;

        public int Shield() => 0;

        public bool UseResource(string id, int value)
        {
            if (_resourceCount < value)
            {
                Debug.Log($"UseResource {_resourceCount} < {value}");
                return false;
            }
            _resourceCount -= value;
            Debug.Log($"UseResource {id}: {_resourceCount}");
            return true;
        }

        public int ResourceCount(string spec) => _resourceCount;

        public void AddResource(int value) => _resourceCount += value;

        public int NanikaCount(string spec) => 2;
        
        public float Interval(float max) => Random.Range(0.1f, max);
        
        public int RandomInt(int min, int max) => Random.Range(min, max);
        
        public float Double(float value) => value * 2f;
        #endregion

        #region Evaluate Functions
        public int EvaluateFunctionInt(FunctionInt func, string[] args)
        {
            switch (func)
            {
                case FunctionInt.HpMin:
                    return HpMin();
                case FunctionInt.ComboCount:
                    return ComboCount();
                case FunctionInt.Shield:
                    return Shield();
                case FunctionInt.NanikaCount:
                    return NanikaCount(args.FirstOrDefault() ?? string.Empty);
                case FunctionInt.ResourceCount:
                    return ResourceCount(args.FirstOrDefault() ?? string.Empty);
                case FunctionInt.UseResource:
                    return UseResource(args[0], int.Parse(args[1])) ? 1 : 0;
                default:
                    return 0;
            }
        }

        public int EvaluateFunctionInt(string func, string[] args)
            => Enum.TryParse(func, out FunctionInt f)
                ? EvaluateFunctionInt(f, args)
                : 0;

        public float EvaluateFunctionFloat(FunctionFloat func, string[] args)
        {
            switch (func)
            {
                case FunctionFloat.Interval when args.Length > 0 && float.TryParse(args[0], out var v):
                    return Interval(v);
                case FunctionFloat.Double when args.Length > 0 && float.TryParse(args[0], out var d):
                    return Double(d);
                default:
                    return 0f;
            }
        }

        public float EvaluateFunctionFloat(string func, string[] args)
        {
            if (Enum.TryParse(func, out FunctionFloat ff))
                return EvaluateFunctionFloat(ff, args);
            if (Enum.TryParse(func, out FunctionInt fi))
                return EvaluateFunctionInt(fi, args);
            return 0f;
        }
        #endregion

        #region Action Methods
        public void Attack(int value) => Debug.Log($"Attack {value}");

        public void AddPlayerEffect(string targets, string effectId, int value)
            => Debug.Log($"Add effect {effectId} {value} to {targets}");

        public void AddPlayerEffectFor(string targets, string effectId, int value, int duration)
            => Debug.Log($"Add effect {effectId} {value} for {duration} to {targets}");

        public void RemoveRandomDebuffPlayerEffect(string targets, int count)
            => Debug.Log($"Remove {count} debuffs from {targets}");

        public void AddMaxHp(string targets, int value)
            => Debug.Log($"Add max hp {value} to {targets}");

        public void SetNanikaEffectFor(string targets, string effectId, int value)
            => Debug.Log($"Set nanika effect {effectId} {value} for {targets}");

        public void SpawnNanika(string targets, string nanikaId, int spawnPosId)
            => Debug.Log($"Spawn nanika {nanikaId} at {spawnPosId} for {targets}");
        #endregion

        #region Execution
        internal ActionParameter CreateParameter(ParsedAction pa)
        {
            var param = new ActionParameter
            {
                FunctionName = pa.FunctionName
            };
            param.Args.AddRange(pa.Args);
            return param;
        }

        internal void ExecuteAction(ActionParameter param)
        {
            if (Enum.TryParse(param.FunctionName, out FunctionVoid fv))
            {
                ExecuteVoidFunction(fv, param);
            }
        }

        private void ExecuteVoidFunction(FunctionVoid fv, ActionParameter param)
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
        #endregion

        #region Helpers
        private int ParseIntArg(string arg)
            => int.TryParse(arg, out var val)
                ? val
                : IntExpressionEvaluator.Evaluate(arg, this);
        #endregion
    }

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
    
    public enum FunctionInt
    {
        HpMin,
        ComboCount,
        Shield,
        NanikaCount,
        ResourceCount,
        UseResource
    }

    public enum FunctionFloat
    {
        Interval,
        Double
    }
}
