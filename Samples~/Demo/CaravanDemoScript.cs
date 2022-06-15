using UnityEngine;
using UnityEngine.UI;
using CaravanSerialization;
using System.Collections.Generic;
using System;
using System.Linq;
using System.Reflection;

public class CaravanDemoScript : MonoBehaviour
{
    [SerializeField] private Button _save;
    [SerializeField] private Button _reload;

    //Need something to reference the scriptable for it to be loaded in memory
    [SerializeField] private ScriptableObject _saved;

    void Start()
    {
        CaravanHelper.Preload();

#if CARAVAN_DEMO_SAVE_V2
        CaravanHelper.Instance.RegisterMigrationHandler(new V2MigrationHandler());
#endif

        _save.onClick.AddListener(CaravanHelper.Instance.SaveAll);
        _reload.onClick.AddListener(CaravanHelper.Instance.LoadAll);
     
    }

}

#if CARAVAN_DEMO_SAVE_V2

//Impl
public class V2MigrationHandler : IMigrationHandler
{
    public int Version => 2;
    public List<IMigrationDefinition> MigrationDefinitions => _migrationDefinitions;
    public List<IMigrationDefinition> _migrationDefinitions;

    public V2MigrationHandler()
    {
        _migrationDefinitions = new List<IMigrationDefinition>
        {
           new GameManagerMigrationDefinition(),
        };

    }

    public IMigrationDefinition FindDefinitionForType(Type t)
    {
        return _migrationDefinitions.Where(md => md.Type == t).FirstOrDefault();
    }
}

public class GameManagerMigrationDefinition : AbstractMigrationDefinition
{
    public override Type Type => typeof(GameManager);

    protected override void InternalMigrate(ILoader loader, object obj)
    {
        var testStruct = loader.Load<TestStruct>("testStruct");
        obj.GetType().GetField("testStructa", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(obj, testStruct.a);
        obj.GetType().GetField("testStructb", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(obj, testStruct.b);

        var f = loader.Load<float>("f");
        var d = loader.Load<double>("d");
        var i = loader.Load<int>("_i");

        obj.GetType().GetField("f", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(obj, (float) (f + d + i));
    }
}
#endif