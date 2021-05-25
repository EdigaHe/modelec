using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModelecComponents.Creator
{
    public class CreatorData
    {
        private static CreatorData instance = null;

        public List<PadInfo> pads;
        public string partName;
        

        private CreatorData()
        {
            pads = new List<PadInfo>();
            partName = "";        
        }

        public static CreatorData Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new CreatorData();
                }
                return instance;
            }
        }
    }
}
