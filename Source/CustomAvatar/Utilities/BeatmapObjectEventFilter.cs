using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Zenject;

namespace CustomAvatar.Utilities
{
    internal class BeatmapObjectEventFilter : IInitializable, IDisposable
    {
        private readonly BeatmapObjectManager _beatmapObjectManager;
        private readonly IReadonlyBeatmapData _beatmapData;
        private readonly AudioTimeSyncController _audioTimeSyncController;

        private readonly List<NoteData> _burstSliderHeadNoteDatas = new List<NoteData>();
        private readonly Dictionary<NoteData, NoteCutResult> _noteCutResults = new Dictionary<NoteData, NoteCutResult>();

        public event BeatmapObjectManager.NoteWasCutDelegate noteGoodCut;

        public event BeatmapObjectManager.NoteWasCutDelegate noteBadCut;

        public event Action<NoteController> noteMissed;

        [SuppressMessage("CodeQuality", "IDE0051", Justification = "Used by Zenject")]
        private BeatmapObjectEventFilter(BeatmapObjectManager beatmapObjectManager, IReadonlyBeatmapData beatmapData, AudioTimeSyncController audioTimeSyncController)
        {
            _beatmapObjectManager = beatmapObjectManager;
            _beatmapData = beatmapData;
            _audioTimeSyncController = audioTimeSyncController;
        }

        public void Initialize()
        {
            PopulateSliderHeadNoteDatas();

            _beatmapObjectManager.noteWasCutEvent += OnNoteWasCut;
            _beatmapObjectManager.noteWasMissedEvent += OnNoteWasMissed;
        }

        public void Dispose()
        {
            _beatmapObjectManager.noteWasCutEvent -= OnNoteWasCut;
            _beatmapObjectManager.noteWasMissedEvent -= OnNoteWasMissed;
        }

        private void PopulateSliderHeadNoteDatas()
        {
            LinkedListNode<BeatmapDataItem> node = _beatmapData.allBeatmapDataItems.First;

            while (node != null)
            {
                if (node.Value is NoteData noteData && noteData.gameplayType == NoteData.GameplayType.BurstSliderHead)
                {
                    _burstSliderHeadNoteDatas.Add(noteData);
                }

                node = node.Next;
            }

            _burstSliderHeadNoteDatas.TrimExcess();
            _burstSliderHeadNoteDatas.Sort((a, b) => b.CompareTo(a)); // sort in reverse
        }

        private void OnNoteWasCut(NoteController noteController, in NoteCutInfo cutInfo)
        {
            // this is the same logic as MissedNoteEffectSpawner BadNoteCutEffectSpawner
            if (cutInfo.allIsOK && noteController.noteData.colorType != ColorType.None)
            {
                if (ParentWasAlreadyTriggered(noteController, NoteCutResult.Good))
                {
                    return;
                }

                noteGoodCut?.Invoke(noteController, in cutInfo);
            }
            else if (noteController.noteData.time + 0.5f >= _audioTimeSyncController.songTime)
            {
                if (ParentWasAlreadyTriggered(noteController, NoteCutResult.Bad))
                {
                    return;
                }

                noteBadCut?.Invoke(noteController, in cutInfo);
            }
        }

        private void OnNoteWasMissed(NoteController noteController)
        {
            // this is the same logic as MissedNoteEffectSpawner
            if (!noteController.hidden && noteController.noteData.time + 0.5f >= _audioTimeSyncController.songTime && noteController.noteData.colorType != ColorType.None)
            {
                if (ParentWasAlreadyTriggered(noteController, NoteCutResult.Missed))
                {
                    return;
                }

                noteMissed?.Invoke(noteController);
            }
        }

        private bool ParentWasAlreadyTriggered(NoteController noteController, NoteCutResult noteCutResult)
        {
            NoteData noteData = noteController.noteData;

            if (noteData.gameplayType != NoteData.GameplayType.BurstSliderHead && noteData.gameplayType != NoteData.GameplayType.BurstSliderElement && noteData.gameplayType != NoteData.GameplayType.BurstSliderElementFill)
            {
                return false;
            }

            NoteData headNote = GetHeadNote(noteData);

            if (_noteCutResults.TryGetValue(headNote, out NoteCutResult existingResult))
            {
                if (existingResult.HasFlag(noteCutResult))
                {
                    return true;
                }
                else
                {
                    _noteCutResults[headNote] = existingResult | noteCutResult;
                    return false;
                }
            }

            _noteCutResults.Add(headNote, noteCutResult);

            return false;
        }

        private NoteData GetHeadNote(NoteData noteData)
        {
            return _burstSliderHeadNoteDatas.First(nd => nd.time <= noteData.time && nd.colorType == noteData.colorType);
        }

        [Flags]
        private enum NoteCutResult
        {
            None = 0,
            Good = 1,
            Bad = 2,
            Missed = 4,
        }
    }
}
