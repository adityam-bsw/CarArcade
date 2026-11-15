using UnityEditor;
using UnityEngine;

namespace RoadBuilderGame.Editor
{
    /// <summary>
    /// Keeps audio cheap on phones without anyone touching import settings: everything mono, Vorbis;
    /// music streams (so 25 tracks cost no memory), voice lines stay compressed in memory, short effects decompress on load.
    /// Applies to every clip under Resources/RoadBuilder/Audio.
    /// </summary>
    public sealed class AudioImportRules : AssetPostprocessor
    {
        private const string Root = "Assets/Resources/RoadBuilder/Audio/";

        private void OnPreprocessAudio()
        {
            if (!assetPath.StartsWith(Root)) return;
            var imp = (AudioImporter)assetImporter;
            imp.forceToMono = true;
            imp.loadInBackground = assetPath.Contains("/Music/");
            var s = imp.defaultSampleSettings;
            s.compressionFormat = AudioCompressionFormat.Vorbis;
            s.sampleRateSetting = AudioSampleRateSetting.PreserveSampleRate;
            if (assetPath.Contains("/Music/")) { s.loadType = AudioClipLoadType.Streaming; s.quality = 0.45f; }
            else if (assetPath.Contains("/VO/")) { s.loadType = AudioClipLoadType.CompressedInMemory; s.quality = 0.6f; }
            else { s.loadType = AudioClipLoadType.DecompressOnLoad; s.quality = 0.6f; }
            imp.defaultSampleSettings = s;
        }
    }
}
