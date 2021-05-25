using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModelecComponents
{
    public class Part2D
    {
        private string _partName;
        private double _posX;
        private double _posY;
        private double _width;
        private double _height;
        private double _rotateAngle;

        public Part2D()
        {
            this._partName = "";
            this._posX = 0;
            this._posY = 0;
            this._width = 0;
            this._height = 0;
            this._rotateAngle = 0;
        }
        public Part2D(string n, double x, double y)
        {
            this._partName = n;
            this._posX = x;
            this._posY = y;
            this._width = 0;
            this._height = 0;
            this._rotateAngle = 0;
        }

        public string PartName { get => _partName; set => _partName = value; }
        public double PosX { get => _posX; set => _posX = value; }
        public double PosY { get => _posY; set => _posY = value; }
        public double Width { get => _width; set => _width = value; }
        public double Height { get => _height; set => _height = value; }
        public double RotateAngle { get => _rotateAngle; set => _rotateAngle = value; }
    }
}
