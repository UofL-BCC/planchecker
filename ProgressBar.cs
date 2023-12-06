using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Media.Media3D;
using SimpleProgressWindow;
using VMS.TPS.Common.Model.API;
using VMS.TPS.Common.Model.Types;

namespace PlanChecks
{
    public class ProgressBar : SimpleMTbase
    {
        public ProgressBar()
        {

        }

        public volatile bool Done = false;

        public static double TotalTreePoints;

        public static double currentpoint;

        public override bool Run()
        {

            
            if (Count())
            {


                return true;

            };


            

            //MessageBox.Show("e");
            return false;
        }
        
        
        private bool Count()
        {

            SetCloseOnFinish(true);

            UpdateUILabel("Calculating Distances");
            //ProvideUIUpdate((int)Math.Round((UserControl1.currentpoint / UserControl1.TotalTreePoints) * 100), $"{UserControl1.currentpoint}");
            ProvideUIUpdate(1, $"{1}");
            Thread.Sleep(100);

           

            try
            {


                //make the viewport using helix3Dtoolkit for the collision model
                //call the collision model method and calculate the shortes distance between the gantry cylinder and the body/couches/baseplate
                //return the shortest distance
                MessageBox.Show("start collision check");
                CollisionCheck(UserControl1.context1.PlanSetup);
                MessageBox.Show("finish collision check");

                //take out the empty collision check tuple and replace it with the collision result
                var removeTuple = UserControl1.OutputList.Where(c => c.Item1.ToLower().Contains("collision")).FirstOrDefault();

                UserControl1.OutputList.Remove(removeTuple);

                UserControl1.OutputList.Add(new Tuple<string, string, string, bool?>("Collision", "No Collision, closest approach >=2cm", (UserControl1.shortestDistanceGlobal != null) ? Math.Round((double)UserControl1.shortestDistanceGlobal, 2).ToString() +
                " cm" : null, (UserControl1.shortestDistanceGlobal >= 2) ? ((UserControl1.shortestDistanceGlobal > 4) ? true : (bool?)null) : false));




            }
            catch (Exception o)
            {
                MessageBox.Show("Collision check encountered an error.\n" + o.Message);

            }
            Done = true;

            UserControl1.DataGridGlobal.Items.Refresh();

            ProvideUIUpdate(100, $"{100}");


            

            return false;




            
        }

        public static void ShortestDistance(List<Point3D> bodyMeshPositions, List<Tuple<Point3D, string>> cylinderMeshPositions, VVector isocenter, PlanSetup plan)
        {

            double shortestDistance = 2000000;

           
           
            //only use body points which are in the neighborhood of the iso in the z direction
            var zList = cylinderMeshPositions.Select(c => c.Item1).ToList().Select(c => c.Z).ToList();
            zList.Sort();
            var zMin = zList.First();
            var zMax = zList.Last();

            List<Point3D> nearbyBodyPositions = new List<Point3D>();


            //find the neaby body points that you want to measure distance from the mesh
            //use some extra body points if you are comparing e cone (easier to see body this way)
            if (Math.Abs(zMax - zMin) <= 300)
            {
                nearbyBodyPositions = bodyMeshPositions.Where(c => c.Z <= zMax + 100 && c.Z >= zMin - 100).ToList();

            }
            else
            {
                nearbyBodyPositions = bodyMeshPositions.Where(c => c.Z <= zMax && c.Z >= zMin).ToList();

                if (nearbyBodyPositions.Any() == false)
                {
                    nearbyBodyPositions = bodyMeshPositions;
                }


            }

            //arrange mesh points into array of arrays of doubles
            var OnlyMeshPoints = cylinderMeshPositions.Select(c => c.Item1).ToList();


            //stopwatch for optimizing time
            //Stopwatch stopWatch = new Stopwatch();
            //stopWatch.Start();


            //create the kdtree
            KdTree.Math.DoubleMath doubleMath = new KdTree.Math.DoubleMath();
            var tree = new KdTree.KdTree<double, int>(3, doubleMath);

            //add the points from the gantry mesh to the tree
            foreach (var point in OnlyMeshPoints)
            {
                tree.Add(new double[] { point.X, point.Y, point.Z }, 1);
            }

            TotalTreePoints = nearbyBodyPositions.Count;
            //evaluate the body points vs the closest mesh point using the tree
            currentpoint = 0;
            List<double> distList = new List<double>();
            foreach (var point in nearbyBodyPositions)
            {



                var nearestNeighbor = tree.GetNearestNeighbours(new double[] { point.X, point.Y, point.Z }, 1);


                double[] nearPoint = nearestNeighbor[0].Point;

                double distance = (Math.Sqrt(((nearPoint[0] - point.X) * (nearPoint[0] - point.X)) + ((nearPoint[1] - point.Y) * (nearPoint[1] - point.Y))
                                        + ((nearPoint[2] - point.Z) * (nearPoint[2] - point.Z)))) / 10;
                if (distance < shortestDistance)
                {
                    shortestDistance = distance;
                    distList.Add(distance);

                }

                currentpoint++;
                
            }

            //stop the timer and display time to make and test points
            //stopWatch.Stop();
            //MessageBox.Show(stopWatch.Elapsed.TotalSeconds.ToString() + "   total seconds elapsed");


            //get the smallest distance between the mesh and body and assign it to your global variable to display in UI
            distList.Sort();
            UserControl1.shortestDistanceGlobal = distList.FirstOrDefault();



            //define everynthmesh so you can shuffle the points with rng
            List<Tuple<Point3D, string>> every10thMesh1 = cylinderMeshPositions;
            //shuffle the points in the list so you can use every 3rd without introducing aliasing
            Random rng = new Random();
            int n = every10thMesh1.Count;
            while (n > 1)
            {
                n--;
                int k = rng.Next(n + 1);
                var value = every10thMesh1[k];
                every10thMesh1[k] = every10thMesh1[n];
                every10thMesh1[n] = value;

            }




            //get every nth point from the body mesh
            //saves time by not plotting every point
            List<Point3D> every10thBody = nearbyBodyPositions.Where((item, index) => (index + 1) % 25 == 0).Distinct().ToList();
            //assign value to global variable to use outside scope when making the collision check model
            UserControl1.bodyMeshGlobalDisplay = every10thBody;
            List<Tuple<Point3D, string>> every10thMesh = every10thMesh1.Where((item, index) => (index + 1) % 1 == 0).Distinct().ToList();
            //assign value to global variable to use outside scope
            UserControl1.arcMeshGlobalDisplay = every10thMesh;


        }
        
        public static void CollisionCheck(PlanSetup plan)
        {
            //38 cm from iso = nono
            //couches are inserted in plan as support structures
            //If theres no couch it's prob a H/N with the baseplate included in the exteral

            //refer to where this global variables are defined above
            //they are assigned just before the output list is generated
            ShortestDistance(UserControl1.bodyMeshGlobal, UserControl1.arcMeshGlobal, plan.Beams.Where(x => x.IsSetupField == false).First().IsocenterPosition, plan);


        }

    }
}
