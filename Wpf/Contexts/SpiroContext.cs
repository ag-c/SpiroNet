﻿/*
SpiroNet.Wpf
Copyright (C) 2015 Wiesław Šoltés

This program is free software; you can redistribute it and/or
modify it under the terms of the GNU General Public License
as published by the Free Software Foundation; either version 3
of the License, or (at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program; if not, write to the Free Software
Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA
02110-1301, USA.

*/
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows.Input;

namespace SpiroNet.Wpf
{
    public enum Mode { None, Create, Move, Selected }

    public class SpiroContext : ObservableObject
    {
        private Mode _mode = Mode.Create;
        private PathShape _shape = null;
        private double _hitTresholdSquared = 49;
        private PathShape _hitShape = null;
        private int _hitShapePointIndex = -1;
        private double _width = 600;
        private double _height = 600;
        private bool _isClosed = false;
        private bool _isTagged = false;
        private SpiroPointType _pointType = SpiroPointType.G4;
        private IList<PathShape> _shapes = null;
        private IDictionary<PathShape, string> _data = null;

        public Mode Mode
        {
            get { return _mode; }
            set { Update(ref _mode, value); }
        }

        public PathShape Shape
        {
            get { return _shape; }
            set { Update(ref _shape, value); }
        }

        public double HitTresholdSquared
        {
            get { return _hitTresholdSquared; }
            set { Update(ref _hitTresholdSquared, value); }
        }

        public PathShape HitShape
        {
            get { return _hitShape; }
            set { Update(ref _hitShape, value); }
        }

        public int HitShapePointIndex
        {
            get { return _hitShapePointIndex; }
            set { Update(ref _hitShapePointIndex, value); }
        }

        public double Width
        {
            get { return _width; }
            set { Update(ref _width, value); }
        }

        public double Height
        {
            get { return _height; }
            set { Update(ref _height, value); }
        }

        public bool IsClosed
        {
            get { return _isClosed; }
            set { Update(ref _isClosed, value); }
        }

        public bool IsTagged
        {
            get { return _isTagged; }
            set { Update(ref _isTagged, value); }
        }

        public SpiroPointType PointType
        {
            get { return _pointType; }
            set { Update(ref _pointType, value); }
        }

        public IList<PathShape> Shapes
        {
            get { return _shapes; }
            set { Update(ref _shapes, value); }
        }

        public IDictionary<PathShape, string> Data
        {
            get { return _data; }
            set { Update(ref _data, value); }
        }

        public ICommand NewCommand { get; set; }

        public ICommand OpenCommand { get; set; }

        public ICommand SaveAsCommand { get; set; }

        public ICommand ExportAsSvgCommand { get; set; }

        public ICommand ExitCommand { get; set; }

        public ICommand IsClosedCommand { get; set; }

        public ICommand IsTaggedCommand { get; set; }

        public ICommand PointTypeCommand { get; set; }

        public ICommand ExecuteScriptCommand { get; set; }
        
        public Action Invalidate { get; set; }

        public void ToggleIsClosed()
        {
            IsClosed = !IsClosed;
            if (_shape != null)
            {
                _shape.IsClosed = IsClosed;
                UpdateData(_shape);
                Invalidate();
            }
        }

        public void ToggleIsTagged()
        {
            IsTagged = !IsTagged;
            if (_shape != null)
            {
                _shape.IsTagged = IsTagged;
                UpdateData(_shape);
                Invalidate();
            }
        }

        public void TogglePointType(string value)
        {
            var type = (SpiroPointType)Enum.Parse(typeof(SpiroPointType), value);
            PointType = type;
            SetLastPointType(type);
            UpdateData(_shape);
            Invalidate();
        }

        private void NewShape()
        {
            _shape = new PathShape();
            _shape.IsClosed = IsClosed;
            _shape.IsTagged = IsTagged;
            _shape.Points = new ObservableCollection<SpiroControlPoint>();
            Shapes.Add(_shape);
        }

        private void NewPoint(double x, double y)
        {
            var point = new SpiroControlPoint();
            point.X = x;
            point.Y = y;
            point.Type = PointType;
            _shape.Points.Add(point);
        }

        private void SetLastPointPosition(double x, double y)
        {
            if (_shape == null || _shape.Points.Count < 1)
                return;

            SetPointPosition(_shape, _shape.Points.Count - 1, x, y);
        }

        private void SetPointPosition(PathShape shape, int index, double x, double y)
        {
            var point = new SpiroControlPoint();
            point.X = x;
            point.Y = y;
            point.Type = shape.Points[index].Type;
            shape.Points[index] = point;
        }

        private void SetLastPointType(SpiroPointType type)
        {
            if (_shape == null || _shape.Points.Count < 1)
                return;

            var old = _shape.Points[_shape.Points.Count - 1];
            var point = new SpiroControlPoint();
            point.X = old.X;
            point.Y = old.Y;
            point.Type = type;
            _shape.Points[_shape.Points.Count - 1] = point;
        }

        private void UpdateData(PathShape shape)
        {
            if (shape == null)
                return;

            try
            {
                if (Data.ContainsKey(shape))
                {
                    string data;
                    if (shape.TryGetData(out data))
                    {
                        Data[shape] = data;
                    }
                    else
                    {
                        Data[shape] = null;
                    }
                }
                else
                {
                    string data;
                    if (shape.TryGetData(out data))
                    {
                        Data.Add(shape, data);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.Print(ex.Message);
                Debug.Print(ex.StackTrace);
            }
        }

        public void LeftDown(double x, double y)
        {
            if (_shape == null)
            {
                PathShape hitShape;
                int hitShapePointIndex;
                var result = HitTest(x, y, out hitShape, out hitShapePointIndex);
                if (result)
                {
                    _hitShape = hitShape;
                    _hitShapePointIndex = hitShapePointIndex;
                    _mode = Mode.Move;
                    Invalidate();
                    return;
                }
                else
                {
                    if (_hitShape != null)
                    {
                        _hitShape = null;
                        _hitShapePointIndex = -1;
                        _mode = Mode.Create;
                        Invalidate();
                        return;
                    }
                }
            }

            if (_mode == Mode.Create)
            {
                if (_shape == null)
                {
                    NewShape();
                }

                NewPoint(x, y);
                UpdateData(_shape);
                Invalidate();
            }
        }

        public void LeftUp(double x, double y)
        {
            if (_mode == Mode.Move)
            {
                _mode = Mode.Selected;
            }
        }

        public void RightDown(double x, double y)
        {
            if (_shape != null)
            {
                UpdateData(_shape);
                Invalidate();
                _shape = null;
            }
            else
            {
                if (_hitShape != null)
                {
                    _hitShape = null;
                    _hitShapePointIndex = -1;
                    _mode = Mode.Create;
                    Invalidate();
                }
            }
        }

        private double DistanceSquared(double x0, double y0, double x1, double y1)
        {
            double dx = x0 - x1;
            double dy = y0 - y1;
            return dx * dx + dy * dy;
        }

        private bool HitTest(double x, double y, out PathShape hitShape, out int hitShapePointIndex)
        {
            foreach (var shape in _shapes)
            {
                for (int i = 0; i < shape.Points.Count; i++)
                {
                    var point = shape.Points[i];
                    var distance = DistanceSquared(x, y, point.X, point.Y);
                    if (distance < _hitTresholdSquared)
                    {
                        hitShape = shape;
                        hitShapePointIndex = i;
                        return true;
                    }
                }
            }
            hitShape = null;
            hitShapePointIndex = -1;
            return false;
        }

        public void Move(double x, double y)
        {
            if (_shape != null)
            {
                if (_shape.Points.Count > 1)
                {
                    SetLastPointPosition(x, y);
                    UpdateData(_shape);
                    Invalidate();
                }
            }
            else
            {
                if (_mode == Mode.Move)
                {
                    SetPointPosition(_hitShape, _hitShapePointIndex, x, y);
                    UpdateData(_hitShape);
                    Invalidate();
                }
                else if (_mode == Mode.Create)
                {
                    PathShape hitShape;
                    int hitShapePointIndex;
                    var result = HitTest(x, y, out hitShape, out hitShapePointIndex);
                    if (result)
                    {
                        _hitShape = hitShape;
                        _hitShapePointIndex = hitShapePointIndex;
                        Invalidate();
                    }
                    else
                    {
                        _hitShape = null;
                        _hitShapePointIndex = -1;
                        Invalidate();
                    }
                }
            }
        }

        public void New()
        {
            Shapes = new ObservableCollection<PathShape>();
            Data = new Dictionary<PathShape, string>();
            Invalidate();
        }

        public void Open(string path)
        {
            using (var f = System.IO.File.OpenText(path))
            {
                var json = f.ReadToEnd();
                var drawing = JsonSerializer.Deserialize<PathDrawing>(json);
                Open(drawing);
            }
        }

        public void Open(PathDrawing drawing)
        {
            Width = drawing.Width;
            Height = drawing.Height;
            Shapes = drawing.Shapes;
            Data = new Dictionary<PathShape, string>();

            foreach (var shape in Shapes)
            {
                UpdateData(shape);
            }

            Invalidate();
        }

        public void SaveAs(string path)
        {
            using (var f = System.IO.File.CreateText(path))
            {
                var drawing = new PathDrawing()
                {
                    Width = Width,
                    Height = Height,
                    Shapes = Shapes
                };
                var json = JsonSerializer.Serialize(drawing);
                f.Write(json);
            }
        }

        public void ExportAsSvg(string path)
        {
            using (var f = System.IO.File.CreateText(path))
            {
                var sb = new StringBuilder();
                var suffix = Environment.NewLine + "           ";

                sb.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"no\"?>");
                sb.AppendLine(string.Format("<svg xmlns=\"http://www.w3.org/2000/svg\" version=\"1.1\" width=\"{1}\" height=\"{0}\">", Width, Height));

                foreach (var shape in Shapes)
                {
                    sb.AppendLine(string.Format("  <path {0}",
                        shape.IsClosed ?
                        "style=\"fill-rule:nonzero;stroke:#000000;stroke-opacity:1;stroke-width:2;fill:#808080;fill-opacity:0.5\"" :
                        "style=\"fill-rule:nonzero;stroke:#000000;stroke-opacity:1;stroke-width:2;fill:none\""));
                    sb.AppendLine(string.Format("        d=\"{0}\"/>", Data[shape].Replace(Environment.NewLine, suffix)));
                }

                sb.AppendLine("</svg>");

                f.Write(sb);
            }
        }
  
        private SpiroControlPoint ToPoint(SpiroPointType type, string x, string y)
        {
            var point = new SpiroControlPoint();
            point.X = double.Parse(x, CultureInfo.GetCultureInfo("en-GB").NumberFormat);
            point.Y = double.Parse(y, CultureInfo.GetCultureInfo("en-GB").NumberFormat);
            point.Type = type;
            return point;
        }

        public void ExecuteScript(string script)
        {
            if (string.IsNullOrEmpty(script))
                return;

            var newLine = Environment.NewLine.ToCharArray();
            var separator = new char[] { ' ', '\t' };
            var options = StringSplitOptions.RemoveEmptyEntries;
            var lines = script.Split(newLine, options).Select(x => x.Trim().Split(separator, options));

            var shape = new PathShape() { IsClosed = false, IsTagged = true, Points = new ObservableCollection<SpiroControlPoint>() };

            foreach (var line in lines)
            {
                switch (line[0][0])
                {
                    case 'v':
                        {
                            if (line.Length == 3)
                                shape.Points.Add(ToPoint(SpiroPointType.Corner, line[1], line[2]));
                            else
                                throw new FormatException();
                        }
                        break;
                    case 'o':
                        {
                            if (line.Length == 3)
                                shape.Points.Add(ToPoint(SpiroPointType.G4, line[1], line[2]));
                            else
                                throw new FormatException();
                        }
                        break;
                    case 'c':
                        {
                            if (line.Length == 3)
                                shape.Points.Add(ToPoint(SpiroPointType.G2, line[1], line[2]));
                            else
                                throw new FormatException();
                        }
                        break;
                    case '[':
                        {
                            if (line.Length == 3)
                                shape.Points.Add(ToPoint(SpiroPointType.Left, line[1], line[2]));
                            else
                                throw new FormatException();
                        }
                        break;
                    case ']':
                        {
                            if (line.Length == 3)
                                shape.Points.Add(ToPoint(SpiroPointType.Right, line[1], line[2]));
                            else
                                throw new FormatException();
                        }
                        break;
                    case 'z':
                        {
                            if (line.Length == 1)
                                 shape.Points.Add(ToPoint(SpiroPointType.End, "0", "0"));
                            else if (line.Length == 3)
                                shape.Points.Add(ToPoint(SpiroPointType.End, line[1], line[2]));
                            else
                                throw new FormatException();

                            Shapes.Add(shape);
                            UpdateData(shape);
                            shape = new PathShape() { IsClosed = false, IsTagged = true, Points = new ObservableCollection<SpiroControlPoint>() };
                        }
                        break;
                    case '{':
                        {
                            if (line.Length == 3)
                                shape.Points.Add(ToPoint(SpiroPointType.OpenContour, line[1], line[2]));
                            else
                                throw new FormatException();
                        }
                        break;
                    case '}':
                        {
                            if (line.Length == 3)
                                shape.Points.Add(ToPoint(SpiroPointType.EndOpenContour, line[1], line[2]));
                            else
                                throw new FormatException();
                            
                            Shapes.Add(shape);
                            UpdateData(shape);
                            shape = new PathShape() { IsClosed = false, IsTagged = true, Points = new ObservableCollection<SpiroControlPoint>() };
                        }
                        break;
                    default: 
                        throw new FormatException();
                }
            }

            if (shape != null)
            {
                shape.IsTagged = false;
                Shapes.Add(shape);
                UpdateData(shape);
                shape = null;
            }

            Invalidate(); 
        }
    }
}