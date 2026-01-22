using System.Diagnostics;

namespace KoReadingABook
{
    public class MouseService
    {
        private double _currentAngle = 0;
        private const double Radius = 250;
        private const int Steps = 72;
        private readonly double _angleIncrement;
        private readonly int _centerX;
        private readonly int _centerY;

        private NativeMethods.POINT _lastSetPosition;
        private DateTime _pauseUntil = DateTime.MinValue;
        private bool _firstRun = true;
        private bool _isPaused = false;

        public bool IsPaused => DateTime.Now < _pauseUntil;

        public MouseService()
        {
            _angleIncrement = 2 * Math.PI / Steps;
            
            // Calculate center of primary screen
            int screenWidth = NativeMethods.GetSystemMetrics(NativeMethods.SM_CXSCREEN);
            int screenHeight = NativeMethods.GetSystemMetrics(NativeMethods.SM_CYSCREEN);
            
            _centerX = screenWidth / 2;
            _centerY = screenHeight / 2;
        }

        public string PerformCircularMove()
        {
            // 1. Check if we are in a pause period
            if (DateTime.Now < _pauseUntil)
            {
               _isPaused = true;
               return "Paused";
            }

            // 2. Check for resume
            string statusMessage = null;
            if (_isPaused)
            {
                _isPaused = false;
                _firstRun = true; // Reset position tracking to avoid immediate re-pause
                statusMessage = "Auto-movement resumed.";
            }

            // 3. Check current cursor position
            if (NativeMethods.GetCursorPos(out NativeMethods.POINT currentPos))
            {
                // If it's not the first run, check if the mouse has moved significantly from where we set it
                if (!_firstRun)
                {
                    double distance = Math.Sqrt(Math.Pow(currentPos.X - _lastSetPosition.X, 2) + Math.Pow(currentPos.Y - _lastSetPosition.Y, 2));
                    
                    // Tolerance distance (e.g. 50 pixels). If user moved mouse, distance will be large.
                    if (distance > 50)
                    {
                        // User moved the mouse! Pause for 10 seconds.
                        _pauseUntil = DateTime.Now.AddSeconds(10);
                        return "User interaction detected. Pausing mouse for 10s.";
                    }
                }
            }

            // 4. Calculate new position
            int x = (int)(_centerX + Radius * Math.Cos(_currentAngle));
            int y = (int)(_centerY + Radius * Math.Sin(_currentAngle));

            NativeMethods.SetCursorPos(x, y);
            
            _lastSetPosition = new NativeMethods.POINT { X = x, Y = y };
            _firstRun = false;

            // Increment angle
            _currentAngle += _angleIncrement;
            if (_currentAngle >= 2 * Math.PI)
            {
                _currentAngle -= 2 * Math.PI;
            }

            return statusMessage;
        }
    }
}
