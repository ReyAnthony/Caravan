
```cs
namespace NinjaToolset.GameFramework.Options
{
    [Saved(nameof(GameSettings), requiresExplicitAction: true)]
    public partial class GameSettings : CaravanScriptableObject
    {
        [Inject] private TranslationManager _translationManager;
        
        //Filled by the game
        [SaveThat] private Object _gameSpecificSettings;

        public int QualityLevel
        {
            get => QualitySettings.GetQualityLevel();
            set => QualitySettings.SetQualityLevel(value);
        }

        public string CurrentLocale
        {
            get => _translationManager.SelectedLocale;
            set => _translationManager.SetLocaleFromName(value);
        }
        
        public object GameSpecificSettings
        {
            get => _gameSpecificSettings;
            set => _gameSpecificSettings = value;
        }

        protected override void SaveCallback(ISaver saver)
        {
            saver.Save(CurrentLocale, "_locale");
            saver.Save(QualityLevel, "_qualityLevel");
        }

        protected override void LoadCallback(ILoader loader)
        {
            CurrentLocale = loader.Load<string>("_locale");
            QualityLevel = loader.Load<int>("_qualityLevel");
        }

        public void Save() => CaravanHelper.Instance.SaveExplicit(nameof(GameSettings));
        public void Load() => CaravanHelper.Instance.LoadExplicit(nameof(GameSettings));
        
        [CallOnEditorDomainReload]
        public void ResetToDefault()
        {
            _translationManager = DItanHelpers.FindInjector().LateBind<TranslationManager>();
            
            _gameSpecificSettings = null;
            CurrentLocale = _translationManager.DefaultLocale;
            QualitySettings.SetQualityLevel(5);
        }
    }
}
```

```cs
namespace NinjaToolset.GameFramework.SaveLoad
{
    /// <summary>
    /// Contains scene data, like where was the player when saving, will be saved by Caravan.
    /// </summary>
    [Saved("SceneData")]
    internal class SceneData : CaravanScriptableObject, IDitanResettable
    {
        [ShowInInspector] private string _savedSceneName;
        [ShowInInspector] private Vector3 _playerPosition;
        [ShowInInspector] private Quaternion _playerRotation;
        
        //if this is true then the game should do something with the position or other data !
        [ShowInInspector] private bool _needsLoading;
        
        //If not null, this will be used when loading a level to set the position of the player according to a marker
        [ShowInInspector] private string _sceneChangeStartPointId = null;
        

        public string Scene => _savedSceneName;
        public Vector3 Position => _playerPosition;
        public Quaternion Rotation => _playerRotation;
        public bool NeedsLoading => _needsLoading;
        public string SceneChangeStartPointId => _sceneChangeStartPointId;

        //This only gets called when the Scriptable is created
        public void Awake()
        {
            _savedSceneName = null;
            _playerPosition = Vector3.zero;
            _playerRotation = Quaternion.identity;
            _needsLoading = false;
        }

        protected override void SaveCallback(ISaver saver)
        {
            var playerEntity = 
                DItanHelpers.FindInjector().LateBind<AbstractGameStatus>().PlayerEntity;
            
            saver.Save<string>(SceneManager.GetActiveScene().path, "sceneId");
            
            var transform = playerEntity.transform;
            saver.Save(transform.position, "playerPos");
            saver.Save(transform.localRotation, "playerRot");
            _sceneChangeStartPointId = null;
        }

        protected override void LoadCallback(ILoader loader)
        {
            _savedSceneName = loader.Load<string>("sceneId");
            _playerPosition = loader.Load<Vector3>("playerPos");
            _playerRotation = loader.Load<Quaternion>("playerRot");
            _needsLoading = true;
            _sceneChangeStartPointId = null;
        }

        [CallOnEditorDomainReload]
        public void ResetData()
        {
            _savedSceneName = null;
            _playerPosition = Vector3.zero;
            _playerRotation = Quaternion.identity;
            _needsLoading = false;
            _sceneChangeStartPointId = null;
        }

        public void SetFutureSpawnId(string idToSpawnAt)
        {
            _sceneChangeStartPointId = idToSpawnAt;
            _needsLoading = true;
        }
    }
}
```
