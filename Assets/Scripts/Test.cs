using RuntimeScripting;
using UnityEngine;

public class Test : MonoBehaviour
{
    private RuntimeTextScriptController _controller;
    private GameLogic _gameLogic;

    private void Start()
    {
        _gameLogic = new GameLogic();
        
        // register functions that return int
        _gameLogic.RegisterFunction(nameof(HpMin), (logic, parameter) => HpMin());
        _gameLogic.RegisterFunction(nameof(ComboCount), (logic, parameter) => ComboCount());
        _gameLogic.RegisterFunction(nameof(Shield), (logic, parameter) => Shield());
        _gameLogic.RegisterFunction(nameof(NanikaCount), (logic, parameter) => NanikaCount(parameter.Args[0]));
        _gameLogic.RegisterFunction(nameof(ResourceCount), (logic, parameter) => ResourceCount(parameter.Args[0]));

        // register a function that returns bool
        _gameLogic.RegisterFunction(nameof(UseResource), (logic, parameter) => UseResource(
            parameter.Args[0],
            logic.ParseIntArg(parameter, 1)
        ));

        // register functions that return float
        _gameLogic.RegisterFunction(nameof(Interval), (logic, parameter) => Interval(logic.ParseFloatArg(parameter, 0)));
        
        // register actions
        _gameLogic.RegisterAction(nameof(Attack), (logic, parameter) => { Attack(logic.ParseIntArg(parameter, 0)); });
        _gameLogic.RegisterAction(nameof(AddPlayerEffect),
            (logic, parameter) =>
            {
                AddPlayerEffect(parameter.Args[0], parameter.Args[1], logic.ParseIntArg(parameter, 2));
            });
        _gameLogic.RegisterAction(nameof(AddPlayerEffectFor), (logic, parameter) =>
        {
            AddPlayerEffectFor(
                parameter.Args[0],
                parameter.Args[1],
                logic.ParseIntArg(parameter, 2),
                logic.ParseIntArg(parameter, 3)
            );
        });
        _gameLogic.RegisterAction(nameof(RemoveRandomDebuffPlayerEffect), (logic, parameter) =>
        {
            RemoveRandomDebuffPlayerEffect(
                parameter.Args[0],
                logic.ParseIntArg(parameter, 1)
            );
        });
        _gameLogic.RegisterAction(nameof(AddMaxHp), (logic, parameter) =>
        {
            AddMaxHp(
                parameter.Args[0],
                logic.ParseIntArg(parameter, 1)
            );
        });
        _gameLogic.RegisterAction(nameof(SetNanikaEffectFor), (logic, parameter) =>
        {
            SetNanikaEffectFor(
                parameter.Args[0],
                parameter.Args[1],
                logic.ParseIntArg(parameter, 2)
            );
        });
        _gameLogic.RegisterAction(nameof(SpawnNanika), (logic, parameter) =>
        {
            SpawnNanika(
                parameter.Args[0],
                parameter.Args[1],
                logic.ParseIntArg(parameter, 2)
            );
        });

        _controller = gameObject.GetComponent<RuntimeTextScriptController>();
        _controller.Initialize(_gameLogic);
        _controller.LoadFile("ScriptFiles/test2.txt");
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.S))
        {
            _controller.Trigger("OnSpawned");
        }
        
        if (Input.GetKeyDown(KeyCode.D))
        {
            _controller.Trigger("OnDropped");
        }
        
        if (Input.GetKeyDown(KeyCode.F))
        {
            _controller.ExecuteString("act { AddPlayerEffect(@l, strength, 1) } mod { interval = 1 };");
        }
        
        if (Input.GetKeyDown(KeyCode.G))
        {
            _controller.ExecuteEasyScript("AddPlayerEffect(@l,strength,1):1");
        }


        if (Input.GetKeyDown(KeyCode.Space))
        {
            _resourceCount += 1;
            Debug.Log($"Resource: {ResourceCount("sun")}");
        }
    }

    public void Attack(int value) => Debug.Log($"Attack {value}");

    public void AddPlayerEffect(string targets, string effectId, int value)
        => Debug.Log($"Add effect {effectId} {value} to {targets}");

    public void AddPlayerEffectFor(string targets, string effectId, int value, float duration)
        => Debug.Log($"Add effect {effectId} {value} for {duration} to {targets}");

    public void RemoveRandomDebuffPlayerEffect(string targets, int count)
        => Debug.Log($"Remove {count} debuffs from {targets}");

    public void AddMaxHp(string targets, int value)
        => Debug.Log($"Add max hp {value} to {targets}");

    public void SetNanikaEffectFor(string targets, string effectId, int value)
        => Debug.Log($"Set nanika effect {effectId} {value} for {targets}");

    public void SpawnNanika(string targets, string nanikaId, int spawnPosId)
        => Debug.Log($"Spawn nanika {nanikaId} at {spawnPosId} for {targets}");


    public int HpMin() => 100;

    private int _comboCount;
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

    private int _resourceCount;
    public int ResourceCount(string spec) => _resourceCount;

    public int NanikaCount(string spec)
    {
        Debug.Log($"NanikaCount {spec}");
        return 2;
    }

    public float Interval(float interval) => interval;
}
