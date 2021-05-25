using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModelecComponents
{
    public class TargetBody
    {
        private static TargetBody instance = null;

        public Guid objID;
        public Guid convertedObjID;
        public Brep bodyBrep;
        public List<Point3d> surfPts;
        public List<Point3d> bodyPts;
        public List<Point3d> allPts;
        public List<Point3d> forRtPts;
        public int rtShape; // 1: round; 2: square; 3: triangle
        public double rtThickness; // the thickness is decided by the printer profile 
        public List<partSocket> partSockets;

        private TargetBody()
        {
            this.surfPts = new List<Point3d>();
            this.objID = Guid.Empty;
            this.bodyPts = new List<Point3d>();
            this.allPts = new List<Point3d>();
            this.forRtPts = new List<Point3d>();
            this.rtShape = 1;
            this.rtThickness = 0.4;
            this.bodyBrep = new Brep();
            this.partSockets = new List<partSocket>();
            this.convertedObjID = Guid.Empty;
        }

        // Singleton design pattern
        public static TargetBody Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new TargetBody();
                }
                return instance;
            }
        }
    }

    public class partSocket
    {
        private string _prtName;
        private Brep _socketBrep;

        public partSocket() {
            this._prtName = "";
            this._socketBrep = new Brep();
        }

        public string PrtName { get => _prtName; set => _prtName = value; }
        public Brep SocketBrep { get => _socketBrep; set => _socketBrep = value; }
    }
}
