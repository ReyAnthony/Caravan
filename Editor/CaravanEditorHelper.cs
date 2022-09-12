using System;
using UnityEditor;

namespace CaravanSerialization.Editor
{
	public static class CaravanEditorHelper
	{
        [MenuItem("Tools/Caravan/Save all")]
        public static void Save()
        {
            CaravanHelper.Instance.SaveAll();
        }

        [MenuItem("Tools/Caravan/Load all")]
        public static void Load()
        {
            CaravanHelper.Instance.LoadAll();
        }

        [MenuItem("Tools/Caravan/Generate All missing IDs")]
        public static void GenerateAllMissingIDs()
        {
            CaravanHelper.Instance.GenerateAllMissingInstanceID();
        }

        [MenuItem("Tools/Caravan/Clear all savefiles")]
        public static void ClearAllSaveFiles()
        {
            //Delete all json file in save dir 
            CaravanHelper.Instance.CleanAllSaves();
        }
    }
}


