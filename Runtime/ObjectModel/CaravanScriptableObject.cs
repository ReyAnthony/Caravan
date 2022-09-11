using System;
using CaravanSerialization.Attributes;
using CaravanSerialization.ObjectModel;
using UnityEngine;

#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#else
using NaughtyAttributes;
#endif

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace CaravanSerialization.ObjectModel
{
	[RequireSavedInInheritors]
	public abstract class CaravanScriptableObject : ScriptableObject
	{
        [CaravanId, SerializeField , ReadOnly] private string _id;

        [SaveCallback]
		protected abstract void SaveCallback(ISaver saver);

		[LoadCallback]
		protected abstract void LoadCallback(ILoader loader);

#if UNITY_EDITOR
		[Button]
		private void GenerateUuid()
		{
			if(!string.IsNullOrEmpty(_id))
            {
				if(!EditorUtility.DisplayDialog("Warning", "You have already an ID !\nThis will most likely break the saves !", "k", "Cancel"))
                {
					return;
                }
            }
			_id = Guid.NewGuid().ToString();
		}
#endif
	}
}
