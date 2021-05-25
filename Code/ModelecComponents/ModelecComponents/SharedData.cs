using Rhino;
using Rhino.Geometry;
using Rhino.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ModelecComponents.Helpers;

namespace ModelecComponents
{

    public class MyMouseCallback : MouseCallback
    {
        PartValidationHelper valEngine = new PartValidationHelper();

        protected override void OnEndMouseUp(MouseCallbackEventArgs e)
        {
            base.OnEndMouseUp(e);
            e.Cancel = false;
            valEngine.validateParts();
        }
    }

    public class SharedData
    {
        private static SharedData instance = null;

        public string circuitPath;  // the directory that stores all the circuit schematic files
        public string circuitFileName; // the circuit schematic file name
        public int schematicType; // the type of schematics: 1: Eagle file; 2: Fritzing file; 3: Cadence
        public List<Part2D> part2DList;
        public List<Part3D> part3DList;
        public List<ConnectionNet> connectionNetList;

        public List<TraceCluster> traceOracle;
        public List<Trace> deployedTraces; // track the traces that have beed generated in the model
        public List<Guid> pads; // store all the generated pads

        public bool isTraceGenerated;

        private MouseCallback m_mc;

        public Part3DP currPart3DP;
        public List<Part3DP> part3DPList;
        public List<Trace> currTrace;
        public Part3D currPart3D;


        private SharedData()
        {
            part2DList = new List<Part2D>();
            part3DList = new List<Part3D>();
            connectionNetList = new List<ConnectionNet>();
            deployedTraces = new List<Trace>();
            traceOracle = new List<TraceCluster>();
            circuitFileName = "";
            schematicType = -1;
            circuitPath = "";
            isTraceGenerated = false;

            currPart3DP = new Part3DP();
            part3DPList = new List<Part3DP>();
            currTrace = new List<Trace>();
            currPart3D = new Part3D();
            pads = new List<Guid>();

            if (m_mc == null)
            {
                m_mc = new MyMouseCallback();
                m_mc.Enabled = true;
            }
        }

        // Singleton design pattern
        public static SharedData Instance
        {
            get
            {
                if(instance == null)
                {
                    instance = new SharedData();
                }
                return instance;
            } 
        }
    }

    public class TraceCluster
    {
        private Point3d _clusterCenter;
        private List<TraceClusterMember> _clusterMembers;
        private List<JumpWire> _jumpwires;

        public TraceCluster() {
            this._clusterCenter = new Point3d();
            this._clusterMembers = new List<TraceClusterMember>();
            this._jumpwires = new List<JumpWire>();
        }

        public Point3d ClusterCenter { get => _clusterCenter; set => _clusterCenter = value; }
        public List<TraceClusterMember> ClusterMembers { get => _clusterMembers; set => _clusterMembers = value; }
        public List<JumpWire> Jumpwires { get => _jumpwires; set => _jumpwires = value; }
    }

    public class JumpWire
    {
        private Guid _crvID;
        private Curve _crv;
        private string _startPrtName;
        private string _startPinName;
        private string _endPrtName;
        private string _endPinName;
        private bool _isDeployed;

        public JumpWire()
        {
            this._crvID = Guid.Empty;
            this._crv = null;
            this._startPrtName = "";
            this._startPinName = "";
            this._endPrtName = "";
            this._endPinName = "";
            this._isDeployed = false;
        }

        public Guid CrvID { get => _crvID; set => _crvID = value; }
        public Curve Crv { get => _crv; set => _crv = value; }
        public string StartPrtName { get => _startPrtName; set => _startPrtName = value; }
        public string StartPinName { get => _startPinName; set => _startPinName = value; }
        public string EndPrtName { get => _endPrtName; set => _endPrtName = value; }
        public string EndPinName { get => _endPinName; set => _endPinName = value; }
        public bool IsDeployed { get => _isDeployed; set => _isDeployed = value; }
    }

    public class TraceClusterMember
    {
        private string _partName;
        private string _pinName;
        private Point3d _pos;

        public TraceClusterMember()
        {
            this._partName = "";
            this._pinName = "";
            this._pos = new Point3d();
        }

        public string PartName { get => _partName; set => _partName = value; }
        public string PinName { get => _pinName; set => _pinName = value; }
        public Point3d Pos { get => _pos; set => _pos = value; }
    }
}
