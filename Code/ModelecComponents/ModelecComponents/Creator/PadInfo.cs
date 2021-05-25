using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModelecComponents.Creator
{
    public class PadInfo
    {
        private string _padName;
        private double _x;
        private double _y;
        private double _z;

        public PadInfo()
        {
            this._padName = "";
            this._x = 0;
            this._y = 0;
            this._z = 0;
        }

        public string PadName { get => _padName; set => _padName = value; }
        public double X { get => _x; set => _x = value; }
        public double Y { get => _y; set => _y = value; }
        public double Z { get => _z; set => _z = value; }
    }
}
