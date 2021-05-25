using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModelecComponents.A_star
{
    public class VoxClass
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double Z { get; set; }
        public double Cost { get; set; }
        public double Distance { get; set; }
        public double CostDistance => Cost + Distance;
        public VoxClass Parent { get; set; }
        public void SetDistance(double targetX, double targetY, double targetZ)
        {
            this.Distance = Math.Abs(targetX - X) + Math.Abs(targetY - Y) + Math.Abs(targetZ - Z);
        }
    }
}
