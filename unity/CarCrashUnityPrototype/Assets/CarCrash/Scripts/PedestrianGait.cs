using System;
using System.Collections.Generic;
using UnityEngine;

namespace CarCrash
{
    internal sealed class PedestrianGait
    {
        private const float JogMetersPerSecond = 3f;
        private const float RunMetersPerSecond = 4.5f;
        private static readonly Dictionary<string, MotionClips> Clips = new(StringComparer.OrdinalIgnoreCase);

        private readonly Animation _animation;
        private readonly AnimationState _idle;
        private readonly AnimationState _walk;
        private readonly AnimationState _jog;
        private readonly AnimationState _run;
        private readonly float _walkMetersPerSecond;
        private AnimationState _playing;

        private readonly struct MotionClips
        {
            public readonly AnimationClip Idle, Jog, Run;

            public MotionClips(string character)
            {
                Idle = Load(character + "-idle_standing");
                Jog = Load(character + "-jog");
                Run = Load(character + "-run");
            }

            private static AnimationClip Load(string name)
            {
                foreach (var clip in Resources.LoadAll<AnimationClip>("PedestrianMotion/" + name))
                    if (clip.legacy && !clip.name.StartsWith("__preview__", StringComparison.Ordinal)) return clip;
                return null;
            }
        }

        public PedestrianGait(Animation animation, AnimationClip walkClip, float walkMetersPerSecond)
        {
            _animation = animation;
            _walkMetersPerSecond = Mathf.Max(.1f, walkMetersPerSecond);
            _walk = AddState(walkClip, "Walk");
            foreach (var bone in animation.GetComponentsInChildren<Transform>(true))
            {
                var separator = bone.name.IndexOf(' ');
                if (separator <= 0 || !bone.name.Contains("anatomical body.rig")) continue;
                var character = bone.name[..separator].ToLowerInvariant();
                if (!Clips.TryGetValue(character, out var clips))
                    Clips.Add(character, clips = new MotionClips(character));
                _idle = AddState(clips.Idle, "Idle");
                _jog = AddState(clips.Jog, "Jog");
                _run = AddState(clips.Run, "Run");
                break;
            }

            SetSpeed(0f);
        }

        private AnimationState AddState(AnimationClip clip, string alias)
        {
            if (clip == null) return null;
            if (_animation[alias] == null) _animation.AddClip(clip, alias);
            var state = _animation[alias];
            state.wrapMode = WrapMode.Loop;
            return state;
        }

        public void SetSpeed(float speed)
        {
            var runThreshold = _playing == _run ? 3.12f : 3.46f;
            var jogThreshold = _playing == _jog ? 1.82f : 2.09f;
            var state = speed < .15f && _idle != null ? _idle :
                speed > runThreshold && _run != null ? _run :
                speed > jogThreshold && _jog != null ? _jog : _walk;
            if (state == null) return;

            var pace = state == _run ? RunMetersPerSecond : state == _jog ? JogMetersPerSecond : _walkMetersPerSecond;
            state.speed = state == _idle ? 1f : Mathf.Clamp(speed / pace, 0f, 1.25f);
            if (_playing == state) return;

            var previous = _playing;
            _playing = state;
            if (!_animation.enabled) return;
            if (previous == null || !_animation.IsPlaying(previous.name))
            {
                _animation.Play(state.name);
                return;
            }
            if (state != _idle && previous != _idle)
                state.normalizedTime = Mathf.Repeat(previous.normalizedTime, 1f);
            _animation.CrossFade(state.name, .18f);
        }

        public void SetVisible(bool visible)
        {
            if (_animation.enabled == visible) return;
            _animation.enabled = visible;
            if (visible && _playing != null) _animation.Play(_playing.name);
        }

        public static float FacingSpeed(ref Quaternion heading, Vector3 destination, float pace, float turnRate, float step)
        {
            if (destination.sqrMagnitude < .04f) return 0f;
            var target = Quaternion.LookRotation(destination);
            heading = Quaternion.RotateTowards(heading, target, turnRate * step);
            var alignment = Mathf.Clamp01((Vector3.Dot(heading * Vector3.forward, destination.normalized) - .2f) / .8f);
            return pace * alignment * Mathf.Lerp(.32f, 1f, Mathf.Clamp01(destination.magnitude / 1.1f));
        }
    }
}
