using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModelecComponents
{
    public class Part3D
    {
        private Guid _prtID;
        private string _partName;
        private Point3d _partPos;
        private string _modelPath;
        private List<Pin> _pins;
        private bool _isDeployed;
        private double _rotationAngle; // degree 0 -360
        private bool _isFlipped;
        private double _exposureLevel; // percentage of the exposure: 0, 10%, 20%, ... , 100%
        private double _height;
        private bool _iscustom;

        private List<Transform> _transformSets; // translate first followed by rotation
        private List<Transform> _transformReverseSets; // rotation first followed by translation

        private Vector3d _normal;
        private bool _isPrintable;

        public Part3D()
        {
            this._prtID = Guid.Empty;
            this._partName = "";
            this._partPos = new Point3d(0, 0, 0);
            this._modelPath = "";
            this._pins = new List<Pin>();
            this._isDeployed = true;
            this._rotationAngle = 0;
            this._transformSets = new List<Transform>();
            this._transformReverseSets = new List<Transform>();
            this._normal = new Vector3d();
            this._isFlipped = false;
            this._exposureLevel = 0;
            this._height = 0;
            this._iscustom = false;
            this._isPrintable = false;
        }
        public Part3D(string n, double x, double y, double z)
        {
            this._prtID = Guid.Empty;
            this._partName = n;
            this._partPos = new Point3d(x, y, z);
            this._modelPath = "";
            this._pins = new List<Pin>();
            this._isDeployed = false;
            this._rotationAngle = 0;
            this._transformSets = new List<Transform>();
            this._transformReverseSets = new List<Transform>();
            this._normal = new Vector3d();
            this._isFlipped = false;
            this._exposureLevel = 0;
            this._height = 0;
            this._isPrintable = false;
            this._iscustom = false;
        }
        public string PartName { get => _partName; set => _partName = value; }
        public string ModelPath { get => _modelPath; set => _modelPath = value; }
        public List<Pin> Pins { get => _pins; set => _pins = value; }
        public bool IsDeployed { get => _isDeployed; set => _isDeployed = value; }
        public double RotationAngle { get => _rotationAngle; set => _rotationAngle = value; }
        public Guid PrtID { get => _prtID; set => _prtID = value; }
        public Point3d PartPos { get => _partPos; set => _partPos = value; }
        public List<Transform> TransformSets { get => _transformSets; set => _transformSets = value; }
        public List<Transform> TransformReverseSets { get => _transformReverseSets; set => _transformReverseSets = value; }
        public Vector3d Normal { get => _normal; set => _normal = value; }
        public bool IsFlipped { get => _isFlipped; set => _isFlipped = value; }
        public double ExposureLevel { get => _exposureLevel; set => _exposureLevel = value; }
        public double Height { get => _height; set => _height = value; }
        public bool Iscustom { get => _iscustom; set => _iscustom = value; }
        public bool IsPrintable { get => _isPrintable; set => _isPrintable = value; }
    }

    public class Pin
    {
        private string _pinName;
        private int _pinNum;
        private Point3d _pos;

        public Pin() { }
        public Pin(string n, int num, double x, double y, double z)
        {
            this._pinName = n;
            this._pinNum = num;
            this._pos = new Point3d(x, y, z);
        }

        public string PinName { get => _pinName; set => _pinName = value; }
        public int PinNum { get => _pinNum; set => _pinNum = value; }
        public Point3d Pos { get => _pos; set => _pos = value; }
    }
}
