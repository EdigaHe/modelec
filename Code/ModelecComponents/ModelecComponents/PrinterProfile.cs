using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModelecComponents
{
    public class PrinterProfile
    {

        private static PrinterProfile instance = null;

        // printer bed dimensions
        private double _bedX;
        private double _bedY;
        private double _bedZ;

        // Resolusion
        private double _resolution;

        // Minimum trace size
        private List<printingConditionPair> _trace_min_x;
        private List<printingConditionPair> _trace_min_y;

        // Trace spacing
        private List<printingConditionPair> _trace_spacing;

        // Trace resistivity
        private double _trace_resistivity;

        // Minimum pad spacing
        private double _pad_spacing;

        // Conductor(pad) area conditions
        private List<printingConditionPair> _conductor_conditions;

        // Thickness for internal traces
        private double _thickness_inter_above;
        private double _thickness_inter_below;
        private double _thickness_exter_pad;
        private double _thickness_inter_pad;

        // printed trivial electronic parts
        // * this could be set in the tool as well
        private bool _resistor_printed;
        private bool _capacitor_printed;
        private bool _inductor_printed;

        // shrinkage
        private double _shrinkage;

        // the cross-sectional shape of the trace
        private int _trShape;  // 1: round; 2: square; 3: triangle

        private PrinterProfile() {
            this._bedX = 0;
            this._bedY = 0;
            this._bedZ = 0;
            this._capacitor_printed = false;
            this._conductor_conditions = new List<printingConditionPair>();
            this._inductor_printed = false;
            this._pad_spacing = 0;
            this._resistor_printed = false;
            this._resolution = 0;
            this._shrinkage = 0;
            this._thickness_exter_pad = 0;
            this._thickness_inter_above = 0;
            this._thickness_inter_below = 0;
            this._thickness_inter_pad = 0;
            this._trace_min_x = new List<printingConditionPair>();
            this._trace_min_y = new List<printingConditionPair>();
            this._trace_resistivity = 0;
            this._trace_spacing = new List<printingConditionPair>();
            this._trShape = 1;
        }

        // Singleton design pattern
        public static PrinterProfile Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new PrinterProfile();
                }
                return instance;
            }
        }

        public double BedX { get => _bedX; set => _bedX = value; }
        public double BedY { get => _bedY; set => _bedY = value; }
        public double Resolution { get => _resolution; set => _resolution = value; }
        public double Pad_spacing { get => _pad_spacing; set => _pad_spacing = value; }
        public double Shrinkage { get => _shrinkage; set => _shrinkage = value; }
        public bool Resistor_printed { get => _resistor_printed; set => _resistor_printed = value; }
        public bool Capacitor_printed { get => _capacitor_printed; set => _capacitor_printed = value; }
        public bool Inductor_printed { get => _inductor_printed; set => _inductor_printed = value; }
        public double Thickness_inter_above { get => _thickness_inter_above; set => _thickness_inter_above = value; }
        public double Thickness_inter_below { get => _thickness_inter_below; set => _thickness_inter_below = value; }
        public double Thickness_exter_pad { get => _thickness_exter_pad; set => _thickness_exter_pad = value; }
        public double Thickness_inter_pad { get => _thickness_inter_pad; set => _thickness_inter_pad = value; }
        public double BedZ { get => _bedZ; set => _bedZ = value; }
        public List<printingConditionPair> Trace_min_x { get => _trace_min_x; set => _trace_min_x = value; }
        public List<printingConditionPair> Trace_min_y { get => _trace_min_y; set => _trace_min_y = value; }
        public List<printingConditionPair> Trace_spacing { get => _trace_spacing; set => _trace_spacing = value; }
        public double Trace_resistivity { get => _trace_resistivity; set => _trace_resistivity = value; }
        public List<printingConditionPair> Conductor_conditions { get => _conductor_conditions; set => _conductor_conditions = value; }
        public int TrShape { get => _trShape; set => _trShape = value; }
    }

    public class printingConditionPair
    {
        private string _cond;
        private string _value;

        public printingConditionPair() { }
        public printingConditionPair(string c, string v)
        {
            this._cond = c;
            this._value = v;
        }

        public string Cond { get => _cond; set => _cond = value; }
        public string Value { get => _value; set => _value = value; }
    }
}
