using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModelecComponents
{
    public class ConnectionNet
    {
        private string _netName;
        private List<PartPinPair> _partPinPairs;

        public ConnectionNet()
        {
            this._netName = "";
            this._partPinPairs = new List<PartPinPair>();
        }
        public ConnectionNet(string n, List<PartPinPair> plist)
        {
            this._netName = n;
            this._partPinPairs = plist;
        }

        public void AppendPartPinPair(string pt_n, string pin_n)
        {
            PartPinPair temp = new PartPinPair(pt_n, pin_n);
            this._partPinPairs.Add(temp);
        }

        public int IndexOfPart(string pt_n)
        {
            int idx = 0;
            while (!this._partPinPairs.ElementAt(idx).PartName.Equals(pt_n))
            {
                idx++;
            }
            return idx;
        }

        public PartPinPair ElementAt(int idx)
        {
            return this._partPinPairs.ElementAt(idx);
        }

        public string NetName { get => _netName; set => _netName = value; }
        public List<PartPinPair> PartPinPairs { get => _partPinPairs; set => _partPinPairs = value; }
    }

    public class PartPinPair
    {
        private string _partName;
        private string _pinName;

        public PartPinPair()
        {
            this._partName = "";
            this._pinName = "";
        }
        public PartPinPair(string pt_n, string pin_n)
        {
            this._partName = pt_n;
            this._pinName = pin_n;
        }
        public string PartName { get => _partName; set => _partName = value; }
        public string PinName { get => _pinName; set => _pinName = value; }
    }
}
