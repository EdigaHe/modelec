using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModelecComponents
{
    public class Trace
    {
        private List<Guid> _trID;
        private string _prtName;    // always using part name and pin name to locate the trace
        private string _prtPinName;
        //private string _prtBName;
        //private string _prtBPinName;
        private string _desPrtName; // it could be part, 3D-printed part, or a trace, not include the connection net center
        private string _desPinName; // it is valid when a part is connected
        private Point3d _prtAPos;   // the position of pin
        private Point3d _prtBPos;   // the position of point that connects to the pin

        private List<Point3d> _pts; // all the points that are used to compose the trace

        private int _type;  // 1: from the circuit schematic; 2: manual-3DP part to 3DP part; 3: manual-3DP part to trace; 4: manual-3DP part to pad; 5: manual-trace to trace;
                            // 6: manual-trace to pad; 7: manual-pad to pad

        public Trace()
        {
            this._trID = new List<Guid>();
            this._prtName = "";
            this._prtPinName = "";
            //this._prtBName = "";
            //this._prtBPinName = "";
            this._prtAPos = new Point3d();
            this._prtBPos = new Point3d();
            this._type = -1;
            this._desPrtName = "";
            this._desPinName = "";
            this._pts = new List<Point3d>();
        }

        public Trace(string aN, string aP, /*string bN, string bP,*/ List<Guid> ids, Point3d aPos, Point3d bPos)
        {
            this._trID = ids;
            this._prtName = aN;
            this._prtPinName = aP;
            //this._prtBName = bN;
            //this._prtBPinName = bP;
            this._prtAPos = aPos;
            this._prtBPos = bPos;
        }

   
        public string PrtName { get => _prtName; set => _prtName = value; }
        public string PrtPinName { get => _prtPinName; set => _prtPinName = value; }
        //public string PrtBName { get => _prtBName; set => _prtBName = value; }
        //public string PrtBPinName { get => _prtBPinName; set => _prtBPinName = value; }
        public Point3d PrtAPos { get => _prtAPos; set => _prtAPos = value; }
        public Point3d PrtBPos { get => _prtBPos; set => _prtBPos = value; }
        public int Type { get => _type; set => _type = value; }
        public string DesPrtName { get => _desPrtName; set => _desPrtName = value; }
        public string DesPinName { get => _desPinName; set => _desPinName = value; }
        public List<Point3d> Pts { get => _pts; set => _pts = value; }
        public List<Guid> TrID { get => _trID; set => _trID = value; }
    }
}
