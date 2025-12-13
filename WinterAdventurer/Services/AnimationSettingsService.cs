using System;

namespace WinterAdventurer.Services
{
    public class AnimationSettingsService
    {
        private double _animationIntensity = 1.0;
        public double AnimationIntensity
        {
            get => _animationIntensity;
            set
            {
                _animationIntensity = Math.Clamp(value, 0.5, 2.0);
                OnAnimationIntensityChanged?.Invoke();
            }
        }

        public event Action? OnAnimationIntensityChanged;

        /// <summary>
        /// Gets the adjusted delay duration based on current animation intensity.
        /// </summary>
        public int GetAdjustedDelay(int baseDurationMs)
        {
            return (int)(baseDurationMs / _animationIntensity);
        }

        /// <summary>
        /// Gets the CSS custom property value for animation intensity.
        /// Value less than 1 speeds up, value greater than 1 slows down.
        /// </summary>
        public string GetCssIntensityVariable()
        {
            return _animationIntensity.ToString("F2");
        }
    }
}
