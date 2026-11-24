using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

namespace CarCrash
{
    public sealed partial class RacingHudView
    {
        private RectTransform _premiumOverlay;
        private VideoPlayer _premiumPlayer;
        private RawImage _premiumPreview;
        private Text _premiumStatus;
        private string _premiumVehicle;
        private float _premiumLoadDeadline;
        private bool _premiumPrepared;
        private bool _premiumComplete;

        public bool PremiumVideoVisible => _premiumOverlay != null;

        public void ShowPremiumVideo(string vehicleId)
        {
            if (_premiumOverlay != null || !CarCrashHudInput.RideLocked(vehicleId)) return;
            var clip = Resources.Load<VideoClip>("GarageShowcase");
            _premiumVehicle = vehicleId;
            _premiumComplete = false;
            BuildPremiumOverlay();
            if (clip == null)
            {
                SetText(_premiumStatus, "Showcase unavailable");
                return;
            }

            var playerRoot = new GameObject("Garage showcase player");
            playerRoot.transform.SetParent(transform, false);
            _premiumPlayer = playerRoot.AddComponent<VideoPlayer>();
            _premiumPlayer.playOnAwake = false;
            _premiumPlayer.isLooping = false;
            _premiumPlayer.waitForFirstFrame = true;
            _premiumPlayer.timeUpdateMode = VideoTimeUpdateMode.UnscaledGameTime;
            _premiumPlayer.audioOutputMode = VideoAudioOutputMode.None;
            _premiumPlayer.renderMode = VideoRenderMode.APIOnly;
            _premiumPlayer.clip = clip;
            _premiumPlayer.prepareCompleted += PreparePremiumPlayback;
            _premiumPlayer.errorReceived += PremiumPlaybackError;
            _premiumPlayer.loopPointReached += CompletePremiumPlayback;
            _premiumLoadDeadline = Time.realtimeSinceStartup + 15f;
            _premiumPlayer.Prepare();
        }

        private void BuildPremiumOverlay()
        {
            if (_premiumOverlay != null)
            {
                _premiumOverlay.gameObject.SetActive(false);
                Destroy(_premiumOverlay.gameObject);
            }
            _premiumOverlay = Group(_safe, "Premium video").GetComponent<RectTransform>();
            Dim(_premiumOverlay);
            var w = Mathf.Min(_width - 28f, _portrait ? 410f : 670f);
            var h = Mathf.Min(_height - 30f, _portrait ? 774f : 475f);
            var panel = Shape(_premiumOverlay, "Showcase card", (_width - w) / 2f, (_height - h) / 2f, w, h,
                RacingHudGraphic.Shape.Panel, PanelColor).transform;
            Label(panel, "Garage showcase", 18, 13, w - 36, 31, 22, White, TextAnchor.MiddleCenter);
            var clipHeight = Mathf.Min(h - 138f, (w - 36f) * 16f / 9f);
            var clipWidth = clipHeight * 9f / 16f;
            _premiumPreview = Rect(panel, "Showcase video", (w - clipWidth) / 2f, 52f, clipWidth, clipHeight).gameObject.AddComponent<RawImage>();
            _premiumPreview.raycastTarget = false;
            _premiumPreview.texture = _premiumPlayer != null ? _premiumPlayer.texture : null;
            _premiumPreview.color = _premiumPreview.texture != null ? Color.white : Ink;
            _premiumStatus = Label(panel, _premiumComplete ? "Unlocked for 24 hours" :
                _premiumPrepared ? "Watch to unlock for 24 hours" : "Loading showcase", 18f, h - 82f, w - 36f, 22f, 14, White,
                TextAnchor.MiddleCenter);
            ActionButton(panel, "Close showcase", _premiumComplete ? "Back to garage" : "Cancel", 18f, h - 52f, w - 36f,
                40f, ClosePremiumVideo, TileColor, White, 15);
        }

        private void PreparePremiumPlayback(VideoPlayer player)
        {
            if (player != _premiumPlayer) return;
            _premiumPrepared = true;
            _premiumPreview.texture = player.texture;
            _premiumPreview.color = Color.white;
            SetText(_premiumStatus, "Watch to unlock for 24 hours");
            if (CanWatchPremiumVideo) player.Play();
        }

        private void CompletePremiumPlayback(VideoPlayer player)
        {
            if (player != _premiumPlayer || !_premiumPrepared || _premiumComplete) return;
            _premiumComplete = true;
            _session.RewardPremiumVideo(_premiumVehicle);
            BuildPremiumOverlay();
        }

        private void PremiumPlaybackError(VideoPlayer player, string error)
        {
            if (player != _premiumPlayer) return;
            Destroy(player.gameObject);
            _premiumPlayer = null;
            SetText(_premiumStatus, "Showcase unavailable. Close to try again.");
        }

        private void StepPremiumVideo()
        {
            if (_premiumPlayer == null || _premiumComplete) return;
            if (!_premiumPrepared)
            {
                if (Time.realtimeSinceStartup > _premiumLoadDeadline) PremiumPlaybackError(_premiumPlayer, "Preparation timed out");
                return;
            }
            if (!CanWatchPremiumVideo && _premiumPlayer.isPlaying) _premiumPlayer.Pause();
            else if (CanWatchPremiumVideo && !_premiumPlayer.isPlaying) _premiumPlayer.Play();
            if (_premiumPlayer.texture != null && _premiumPreview.texture != _premiumPlayer.texture)
            {
                _premiumPreview.texture = _premiumPlayer.texture;
                _premiumPreview.color = Color.white;
            }
        }

        private static bool CanWatchPremiumVideo => Application.isFocused || Application.isBatchMode;

        public void ClosePremiumVideo()
        {
            if (_premiumOverlay != null)
            {
                _premiumOverlay.gameObject.SetActive(false);
                Destroy(_premiumOverlay.gameObject);
            }
            if (_premiumPlayer != null) Destroy(_premiumPlayer.gameObject);
            _premiumOverlay = null;
            _premiumPlayer = null;
            _premiumPrepared = false;
            _premiumComplete = false;
        }
    }
}
