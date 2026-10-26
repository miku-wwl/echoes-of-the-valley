using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Echoes.EditorTools
{
    /// <summary>Puts an AudioDirector, its AudioSources and the generated clips into a scene.</summary>
    internal static class AudioWiring
    {
        /// <summary>
        /// The valley director follows the ResonanceStage and has every cue. The title screen's plays one welcoming mix
        /// (stage 0's volumes) and only knows the interface sounds.
        /// </summary>
        public static AudioDirector Add(AudioSet set, bool valley)
        {
            GameObject root = new GameObject("Audio");
            AudioDirector director = root.AddComponent<AudioDirector>();
            AudioLibrary.LayerSpec[] specs = valley ? AudioLibrary.ValleyLayers : AudioLibrary.MenuLayers;

            AudioSource effects = NewSource(root.transform, "Effects", 128);
            AudioSource interfaceSounds = NewSource(root.transform, "Interface", 120);
            AudioSource steps = NewSource(root.transform, "Footsteps", 140);
            AudioSource ending = NewSource(root.transform, "Ending Music", 60);

            SerializedObject serialized = new SerializedObject(director);
            SerializedProperty layers = serialized.FindProperty("layers");
            layers.arraySize = specs.Length;
            for (int i = 0; i < specs.Length; i++)
            {
                AudioLibrary.LayerSpec spec = specs[i];
                SerializedProperty element = layers.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("name").stringValue = spec.Name;
                element.FindPropertyRelative("clip").objectReferenceValue = set.Layer(spec.Name);
                element.FindPropertyRelative("source").objectReferenceValue = NewSource(root.transform, "Layer " + spec.Name, 64);
                element.FindPropertyRelative("music").boolValue = spec.Music;
                SerializedProperty volumes = element.FindPropertyRelative("stageVolume");
                volumes.arraySize = spec.Volumes.Length;
                for (int s = 0; s < spec.Volumes.Length; s++) volumes.GetArrayElementAtIndex(s).floatValue = spec.Volumes[s];
            }

            var cues = set.Cues
                .Where(pair => valley || pair.Key == SoundCue.UiClick || pair.Key == SoundCue.UiHover)
                .OrderBy(pair => (int)pair.Key)
                .ToArray();
            SerializedProperty sounds = serialized.FindProperty("cueSounds");
            sounds.arraySize = cues.Length;
            for (int i = 0; i < cues.Length; i++)
            {
                SerializedProperty element = sounds.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("cue").enumValueIndex = (int)cues[i].Key;
                element.FindPropertyRelative("volume").floatValue = AudioLibrary.CueVolume(cues[i].Key);
                SerializedProperty clips = element.FindPropertyRelative("clips");
                clips.arraySize = cues[i].Value.Length;
                for (int c = 0; c < cues[i].Value.Length; c++) clips.GetArrayElementAtIndex(c).objectReferenceValue = cues[i].Value[c];
            }

            serialized.FindProperty("effectSource").objectReferenceValue = effects;
            serialized.FindProperty("uiSource").objectReferenceValue = interfaceSounds;
            serialized.FindProperty("stepSource").objectReferenceValue = steps;
            serialized.FindProperty("endingSource").objectReferenceValue = valley ? ending : null;
            serialized.FindProperty("endingClip").objectReferenceValue = valley ? set.Ending : null;
            serialized.FindProperty("followStage").boolValue = valley;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            if (!valley) Object.DestroyImmediate(ending.gameObject);
            return director;
        }

        static AudioSource NewSource(Transform parent, string name, int priority)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            AudioSource source = go.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = false;
            source.spatialBlend = 0f;
            source.priority = priority;
            return source;
        }
    }
}
