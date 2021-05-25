using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModelecComponents.Routing
{
    /// <summary>
    /// This is used to create traces with A* routing algorithm
    /// </summary>
    public class Route
    {
        private List<Guid> _r_IDs;
        private List<Point3d> _routePts;

        public Route()
        {
            this._r_IDs = new List<Guid>();
            this._routePts = new List<Point3d>();
        }

        public List<Guid> R_IDs { get => _r_IDs; set => _r_IDs = value; }
        public List<Point3d> RoutePts { get => _routePts; set => _routePts = value; }
    }
}
