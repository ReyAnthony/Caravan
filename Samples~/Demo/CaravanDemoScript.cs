using UnityEngine;
using UnityEngine.UI;
using CaravanSerialization;
using JetBrains.Annotations;

public class CaravanDemoScript : MonoBehaviour
{
    [SerializeField] private Button _save;
    [SerializeField] private Button _reload;

    //Need something to reference the scriptable for it to be loaded in memory..
    [SerializeField, UsedImplicitly] private ScriptableObject _saved;

    void Start()
    {
        CaravanHelper.Preload();

        //CaravanHelper.Instance.RegisterMigrationHandler(new V2MigrationHandler());

        _save.onClick.AddListener(CaravanHelper.Instance.SaveAll);
        _reload.onClick.AddListener(CaravanHelper.Instance.LoadAll);
    }

}
