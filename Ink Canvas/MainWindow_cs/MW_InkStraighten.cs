using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using Point = System.Windows.Point;

namespace Ink_Canvas
{
    public partial class MainWindow : Window
    {
        private const int MousePointerId = -1;

        private class InkStraightenSession
        {
            public Point StartPoint;
            public Point LastPoint;
            public DateTime LastTimestamp;
            public bool IsLowSpeedCandidate;
            public DateTime LowSpeedStartTimestamp;
            public Point LowSpeedAnchorPoint;
            public double LowSpeedDisplacement;
            public bool IsTriggered;
            public bool IsCommitted;
            public bool IsInputSuppressed;
            public Stroke PreviewStroke;
            public Point LatestPoint;
        }

        private class PendingInkStraighten
        {
            public Point StartPoint;
            public Point EndPoint;
            public DateTime CreatedAt;
        }

        private readonly Dictionary<int, InkStraightenSession> _inkStraightenSessions = new Dictionary<int, InkStraightenSession>();
        private readonly Queue<PendingInkStraighten> _pendingInkStraightenQueue = new Queue<PendingInkStraighten>();

        private bool IsInkStraightenAvailable()
        {
            return Settings?.InkStraighten != null
                   && Settings.InkStraighten.IsInkStraightenEnabled
                   && inkCanvas.EditingMode == InkCanvasEditingMode.Ink
                   && drawingShapeMode == 0
                   && !forceEraser;
        }

        private static double Distance(Point a, Point b)
        {
            double dx = a.X - b.X;
            double dy = a.Y - b.Y;
            return Math.Sqrt(dx * dx + dy * dy);
        }

        private double GetInputScaleFactor()
        {
            var dpi = VisualTreeHelper.GetDpi(this);
            return Math.Max(1.0, dpi.DpiScaleX);
        }

        private int ClampInkStraightenHoldDurationMs()
        {
            return Math.Max(200, Math.Min(2000, Settings.InkStraighten.HoldDurationMs));
        }

        private void StartInkStraightenSession(int pointerId, Point startPoint)
        {
            if (!IsInkStraightenAvailable())
            {
                return;
            }

            _inkStraightenSessions[pointerId] = new InkStraightenSession
            {
                StartPoint = startPoint,
                LastPoint = startPoint,
                LatestPoint = startPoint,
                LastTimestamp = DateTime.UtcNow
            };
        }

        private void UpdateInkStraightenSession(int pointerId, Point currentPoint)
        {
            if (!_inkStraightenSessions.TryGetValue(pointerId, out var session) || !IsInkStraightenAvailable())
            {
                return;
            }

            DateTime now = DateTime.UtcNow;
            double deltaMs = Math.Max(1, (now - session.LastTimestamp).TotalMilliseconds);
            double deltaDistance = Distance(currentPoint, session.LastPoint) * GetInputScaleFactor();
            double speed = deltaDistance / deltaMs;

            session.LatestPoint = currentPoint;

            if (!session.IsTriggered)
            {
                if (speed < Settings.InkStraighten.SpeedThresholdPxPerMs)
                {
                    if (!session.IsLowSpeedCandidate)
                    {
                        session.IsLowSpeedCandidate = true;
                        session.LowSpeedStartTimestamp = now;
                        session.LowSpeedAnchorPoint = currentPoint;
                        session.LowSpeedDisplacement = 0;
                    }
                    else
                    {
                        session.LowSpeedDisplacement += deltaDistance;
                    }

                    if (session.LowSpeedDisplacement <= Settings.InkStraighten.DisplacementThresholdPx
                        && (now - session.LowSpeedStartTimestamp).TotalMilliseconds >= ClampInkStraightenHoldDurationMs())
                    {
                        session.IsTriggered = true;
                        if (!session.IsInputSuppressed && inkCanvas.EditingMode == InkCanvasEditingMode.Ink)
                        {
                            inkCanvas.EditingMode = InkCanvasEditingMode.None;
                            session.IsInputSuppressed = true;
                        }
                        UpdateInkStraightenPreview(session, currentPoint);
                    }
                    else if (session.LowSpeedDisplacement > Settings.InkStraighten.DisplacementThresholdPx)
                    {
                        session.IsLowSpeedCandidate = false;
                    }
                }
                else
                {
                    session.IsLowSpeedCandidate = false;
                }
            }
            else
            {
                UpdateInkStraightenPreview(session, currentPoint);
            }

            session.LastPoint = currentPoint;
            session.LastTimestamp = now;
        }

        private void UpdateInkStraightenPreview(InkStraightenSession session, Point currentPoint)
        {
            var lineStroke = new Stroke(new StylusPointCollection
            {
                new StylusPoint(session.StartPoint.X, session.StartPoint.Y, 0.5f),
                new StylusPoint(currentPoint.X, currentPoint.Y, 0.5f)
            })
            {
                DrawingAttributes = inkCanvas.DefaultDrawingAttributes.Clone()
            };

            _currentCommitType = CommitReason.CodeInput;
            if (session.PreviewStroke != null && inkCanvas.Strokes.Contains(session.PreviewStroke))
            {
                inkCanvas.Strokes.Remove(session.PreviewStroke);
            }
            inkCanvas.Strokes.Add(lineStroke);
            _currentCommitType = CommitReason.UserInput;

            session.PreviewStroke = lineStroke;
        }

        private void EndInkStraightenSession(int pointerId)
        {
            if (!_inkStraightenSessions.TryGetValue(pointerId, out var session))
            {
                return;
            }

            if (session.IsCommitted)
            {
                if (session.IsInputSuppressed && inkCanvas.EditingMode == InkCanvasEditingMode.None)
                {
                    inkCanvas.EditingMode = InkCanvasEditingMode.Ink;
                }
                _inkStraightenSessions.Remove(pointerId);
                return;
            }

            if (session.PreviewStroke != null && inkCanvas.Strokes.Contains(session.PreviewStroke))
            {
                _currentCommitType = CommitReason.CodeInput;
                inkCanvas.Strokes.Remove(session.PreviewStroke);
                _currentCommitType = CommitReason.UserInput;
            }

            if (session.IsTriggered)
            {
                var straightStroke = new Stroke(new StylusPointCollection
                {
                    new StylusPoint(session.StartPoint.X, session.StartPoint.Y, 0.5f),
                    new StylusPoint(session.LatestPoint.X, session.LatestPoint.Y, 0.5f)
                })
                {
                    DrawingAttributes = inkCanvas.DefaultDrawingAttributes.Clone()
                };

                _currentCommitType = CommitReason.UserInput;
                inkCanvas.Strokes.Add(straightStroke);
                session.IsCommitted = true;
            }

            if (session.IsInputSuppressed && inkCanvas.EditingMode == InkCanvasEditingMode.None)
            {
                inkCanvas.EditingMode = InkCanvasEditingMode.Ink;
            }

            _inkStraightenSessions.Remove(pointerId);
        }

        private bool TryApplyActiveInkStraighten(Stroke rawStroke)
        {
            if (rawStroke == null || rawStroke.StylusPoints.Count == 0)
            {
                return false;
            }

            Point rawStartPoint = rawStroke.StylusPoints.First().ToPoint();
            InkStraightenSession matchedSession = null;
            double minDistance = double.MaxValue;
            foreach (var session in _inkStraightenSessions.Values)
            {
                if (!session.IsTriggered || session.IsCommitted)
                {
                    continue;
                }

                double distance = Distance(rawStartPoint, session.StartPoint);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    matchedSession = session;
                }
            }

            if (matchedSession == null || minDistance > 48)
            {
                return false;
            }

            if (matchedSession.PreviewStroke != null && inkCanvas.Strokes.Contains(matchedSession.PreviewStroke))
            {
                _currentCommitType = CommitReason.CodeInput;
                inkCanvas.Strokes.Remove(matchedSession.PreviewStroke);
                _currentCommitType = CommitReason.UserInput;
            }

            var straightStroke = new Stroke(new StylusPointCollection
            {
                new StylusPoint(matchedSession.StartPoint.X, matchedSession.StartPoint.Y, rawStroke.StylusPoints.First().PressureFactor),
                new StylusPoint(matchedSession.LatestPoint.X, matchedSession.LatestPoint.Y, rawStroke.StylusPoints.Last().PressureFactor)
            })
            {
                DrawingAttributes = rawStroke.DrawingAttributes.Clone()
            };

            SetNewBackupOfStroke();
            _currentCommitType = CommitReason.ShapeRecognition;
            inkCanvas.Strokes.Remove(rawStroke);
            inkCanvas.Strokes.Add(straightStroke);
            _currentCommitType = CommitReason.UserInput;
            matchedSession.IsCommitted = true;
            return true;
        }

        private void inkCanvas_StylusDownForStraighten(object sender, StylusDownEventArgs e)
        {
            StartInkStraightenSession(e.StylusDevice.Id, e.GetPosition(inkCanvas));
        }

        private void inkCanvas_StylusMoveForStraighten(object sender, StylusEventArgs e)
        {
            UpdateInkStraightenSession(e.StylusDevice.Id, e.GetPosition(inkCanvas));
        }

        private void inkCanvas_StylusUpForStraighten(object sender, StylusEventArgs e)
        {
            EndInkStraightenSession(e.StylusDevice.Id);
        }

        private void HandleMouseStraightenDown(MouseButtonEventArgs e)
        {
            if (e?.ChangedButton == MouseButton.Left)
            {
                StartInkStraightenSession(MousePointerId, e.GetPosition(inkCanvas));
            }
        }

        private void HandleMouseStraightenMove(MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                UpdateInkStraightenSession(MousePointerId, e.GetPosition(inkCanvas));
            }
        }

        private void HandleMouseStraightenUp(MouseButtonEventArgs e)
        {
            if (e == null || e.ChangedButton == MouseButton.Left)
            {
                EndInkStraightenSession(MousePointerId);
            }
        }

        private bool TryApplyPendingInkStraighten(Stroke rawStroke)
        {
            if (TryApplyActiveInkStraighten(rawStroke))
            {
                return true;
            }

            if (_pendingInkStraightenQueue.Count == 0 || rawStroke == null || rawStroke.StylusPoints.Count == 0)
            {
                return false;
            }

            var now = DateTime.UtcNow;
            while (_pendingInkStraightenQueue.Count > 0 && (now - _pendingInkStraightenQueue.Peek().CreatedAt).TotalSeconds > 2)
            {
                _pendingInkStraightenQueue.Dequeue();
            }

            if (_pendingInkStraightenQueue.Count == 0)
            {
                return false;
            }

            Point rawStartPoint = rawStroke.StylusPoints.First().ToPoint();
            var candidate = _pendingInkStraightenQueue.Peek();
            if (Distance(rawStartPoint, candidate.StartPoint) > 24)
            {
                return false;
            }

            _pendingInkStraightenQueue.Dequeue();

            var straightStroke = new Stroke(new StylusPointCollection
            {
                new StylusPoint(candidate.StartPoint.X, candidate.StartPoint.Y, rawStroke.StylusPoints.First().PressureFactor),
                new StylusPoint(candidate.EndPoint.X, candidate.EndPoint.Y, rawStroke.StylusPoints.Last().PressureFactor)
            })
            {
                DrawingAttributes = rawStroke.DrawingAttributes.Clone()
            };

            SetNewBackupOfStroke();
            _currentCommitType = CommitReason.ShapeRecognition;
            inkCanvas.Strokes.Remove(rawStroke);
            inkCanvas.Strokes.Add(straightStroke);
            _currentCommitType = CommitReason.UserInput;
            return true;
        }
    }
}
