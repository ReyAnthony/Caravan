using System;
using UnityEditor;

namespace CaravanSerialization
{
	public static class CaravanEditorHelper
	{
        [MenuItem("Game/Save all")]
        public static void Save()
        {
            CaravanHelper.Instance.SaveAll();
        }

        [MenuItem("Game/Load all")]
        public static void Load()
        {
            CaravanHelper.Instance.LoadAll();
        }
    }
}


