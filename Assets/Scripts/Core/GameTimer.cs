using System;
using UnityEngine;

namespace SantaPlay.Core
{
    public class GameTimer
    {
        public event Action<float> OnTick;
        public event Action<float, float> OnTimeUpdated;
        public event Action OnCompleted;

        public float Duration { get; private set; }
        public float RemainingTime { get; private set; }
        public float ElapsedTime => Duration - RemainingTime;
        public float Progress => Duration > 0f ? Mathf.Clamp01(ElapsedTime / Duration) : 0f;
        public bool IsRunning { get; private set; }
        public bool IsCompleted => RemainingTime <= 0f && Duration > 0f;

        public void Start(float duration)
        {
            Duration = Mathf.Max(0f, duration);
            RemainingTime = Duration;
            IsRunning = Duration > 0f;
            OnTick?.Invoke(RemainingTime);
            OnTimeUpdated?.Invoke(RemainingTime, ElapsedTime);

            if (Duration <= 0f)
            {
                OnCompleted?.Invoke();
            }
        }

        public void Tick(float deltaTime)
        {
            if (!IsRunning) return;

            RemainingTime = Mathf.Max(0f, RemainingTime - deltaTime);
            OnTick?.Invoke(RemainingTime);
            OnTimeUpdated?.Invoke(RemainingTime, ElapsedTime);

            if (RemainingTime <= 0f)
            {
                IsRunning = false;
                OnCompleted?.Invoke();
            }
        }

        public void Pause() => IsRunning = false;
        public void Resume() => IsRunning = RemainingTime > 0f;
        public void Stop() => IsRunning = false;
    }
}
