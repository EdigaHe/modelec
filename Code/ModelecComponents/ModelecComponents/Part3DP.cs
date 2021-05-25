using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModelecComponents
{
    public class Part3DP
    {
        private Guid _prtID;
        private string _partName;
        private Point3d _partPos;
        private List<Pin> _pins;
        private double _height;
        private double _length;
        private double _depth;
        private int _type; //1: dot; 2: area; 3: resistor
        private bool _isDeployed;

        private int _level;

        public Part3DP()
        {
            this._prtID = Guid.Empty;
            this._partName = "";
            this._partPos = new Point3d();
            this._pins = new List<Pin>();
            this._height = 0;
            this._depth = 0;
            this._length = 0;
            this._type = -1;
            this._isDeployed = false;
            this._level = 1;
        }

        public Guid PrtID { get => _prtID; set => _prtID = value; }
        public string PartName { get => _partName; set => _partName = value; }
        public Point3d PartPos { get => _partPos; set => _partPos = value; }
        public List<Pin> Pins { get => _pins; set => _pins = value; }
        public double Height { get => _height; set => _height = value; }
        public double Length { get => _length; set => _length = value; }
        public double Depth { get => _depth; set => _depth = value; }
        public int Type { get => _type; set => _type = value; }
        public bool IsDeployed { get => _isDeployed; set => _isDeployed = value; }
        public int Level { get => _level; set => _level = value; }
    }
}
