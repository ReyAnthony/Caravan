using System;
using UnityEditor;

namespace CaravanSerialization.Editor
{
	public static class CaravanEditorHelper
	{
        [MenuItem("Game/Caravan/Save all")]
        public static void Save()
        {
            CaravanHelper.Instance.SaveAll();
        }

        [MenuItem("Game/Caravan/Load all")]
        public static void Load()
        {
            CaravanHelper.Instance.LoadAll();
        }

        [MenuItem("Game/Caravan/Generate All missing IDs")]
        public static void GenerateAllMissingIDs()
        {
            CaravanHelper.Instance.GenerateAllMissingInstanceID();
        }

        [MenuItem("Game/Caravan/Clear all savefiles")]
        public static void ClearAllSaveFiles()
        {
            //Delete all json file in save dir 
            CaravanHelper.Instance.CleanAllSaves();
        }
    }
}


