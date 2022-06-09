using UnityEngine;
using UnityEngine.UI;
using CaravanSerialization;

public class CaravanDemoScript : MonoBehaviour
{
    [SerializeField] private Button _save;
    [SerializeField] private Button _reload;

    //Need something to reference the scriptable for it to be loaded in memory
    [SerializeField] private ScriptableObject _saved;

    void Start()
    {
        CaravanHelper.Preload();
        _save.onClick.AddListener(CaravanHelper.Instance.SaveAll);
        _reload.onClick.AddListener(CaravanHelper.Instance.LoadAll);
    }
}
