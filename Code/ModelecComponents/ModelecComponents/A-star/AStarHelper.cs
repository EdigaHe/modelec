using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModelecComponents.A_star
{
    public class AStarHelper
    {
        public VoxClass Start { get; set; }
        public VoxClass End { get; set; }

        public List<VoxClass> ActiveVoxes { get; set; }
        public List<VoxClass> VisitedVoxes { get; set; }

        public AStarHelper()
        {
            Start = new VoxClass();
            End = new VoxClass();
            ActiveVoxes = new List<VoxClass>();
            VisitedVoxes = new List<VoxClass>();
        }

        public void InitializePathFinding()
        {
            Start.SetDistance(End.X, End.Y, End.Z);
            ActiveVoxes.Add(Start);
        }

        private List<VoxClass> GetWalkableVoxels(List<Point3d> voxels, VoxClass currentVox, VoxClass targetVox, double grid)
        {
            var possibilities = new List<VoxClass>()
            {
                new VoxClass{X = currentVox.X, Y = currentVox.Y + grid, Z = currentVox.Z, Parent = currentVox, Cost = currentVox.Cost + 1},
                new VoxClass{X = currentVox.X, Y = currentVox.Y - grid, Z = currentVox.Z, Parent = currentVox, Cost = currentVox.Cost + 1},
                new VoxClass{X = currentVox.X + grid, Y = currentVox.Y, Z = currentVox.Z, Parent = currentVox, Cost = currentVox.Cost + 1},
                new VoxClass{X = currentVox.X - grid, Y = currentVox.Y, Z = currentVox.Z, Parent = currentVox, Cost = currentVox.Cost + 1},
                new VoxClass{X = currentVox.X, Y = currentVox.Y, Z = currentVox.Z + grid, Parent = currentVox, Cost = currentVox.Cost + 1},
                new VoxClass{X = currentVox.X, Y = currentVox.Y, Z = currentVox.Z - grid, Parent = currentVox, Cost = currentVox.Cost + 1},
            };

            possibilities.ForEach(vox => vox.SetDistance(targetVox.X, targetVox.Y, targetVox.Z));

            return possibilities.Where(vox => voxels.IndexOf(new Point3d(vox.X, vox.Y, vox.Z)) != -1).ToList();
        }

        public List<Point3d> ExecuteAStarPathFinding(List<Point3d> voxels, double grid)
        {
            List<Point3d> pathPts = new List<Point3d>();
           
            while (ActiveVoxes.Any())
            {
                var checkVox = ActiveVoxes.OrderBy(x => x.CostDistance).First();

                if(checkVox.X==End.X && checkVox.Y==End.Y && checkVox.Z == End.Z)
                {
                    // we find the shortest path
                    var v = checkVox;

                    while (true)
                    {
                        pathPts.Add(new Point3d(v.X, v.Y, v.Z));
                        v = v.Parent;
                        if(v == null)
                        {
                            break;
                        }
                    }

                    // Note: the path points stored is from END to START
                    return pathPts;
                }

                VisitedVoxes.Add(checkVox);
                ActiveVoxes.Remove(checkVox);

                var walkableVoxes = GetWalkableVoxels(voxels, checkVox, End, grid);

                foreach(var walkableVox in walkableVoxes)
                {
                    //We have already visited this voxel so we don't need to do so again!
                    if (VisitedVoxes.Any(x => x.X == walkableVox.X && x.Y == walkableVox.Y && x.Z == walkableVox.Z))
                        continue;

                    //It's already in the active list, but that's OK, 
                    //maybe this new voxel has a better value (e.g. We might zigzag earlier but this is now straighter).

                    if(ActiveVoxes.Any(x => x.X == walkableVox.X && x.Y == walkableVox.Y && x.Z == walkableVox.Z))
                    {
                        var existingVox = ActiveVoxes.First(x => x.X == walkableVox.X && x.Y == walkableVox.Y && x.Z == walkableVox.Z);

                        if(existingVox.CostDistance > checkVox.CostDistance)
                        {
                            ActiveVoxes.Remove(existingVox);
                            ActiveVoxes.Add(walkableVox);
                        }
                    }
                    else
                    {
                        //We've never seen this voxel before so add it to the list.
                        ActiveVoxes.Add(walkableVox);

                    }

                }
            }

            // No path found
            return null;

        }
    }
}
