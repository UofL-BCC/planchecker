using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Interop;
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
            SetCloseOnFinish(true);
            
            
        }

        public static bool Continue;

        public static Window progressWindowGlobal;

        public static System.Diagnostics.ProcessThread globalProcessThread;

        public volatile bool Done = false;

        public static double TotalTreePoints;

        public static double currentpoint;

       



       
        public override bool Run()
        {

            
            ShortestDistance(UserControl1.bodyMeshGlobal, UserControl1.arcMeshGlobal, UserControl1.plan.Beams.Where(x => x.IsSetupField == false).First().IsocenterPosition, UserControl1.plan);


            return true;
        }

        public bool ShortestDistance(List<Point3D> bodyMeshPositions, List<Tuple<Point3D, string>> cylinderMeshPositions, VVector isocenter, PlanSetup plan)
        {
            Continue = true;

            SetCloseOnFinish(true, 1);

            

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


            //create the kdtree
            KdTree.Math.DoubleMath doubleMath = new KdTree.Math.DoubleMath();
            var tree = new KdTree.KdTree<double, int>(3, doubleMath);

            //add the points from the gantry mesh to the tree
            foreach (var point in OnlyMeshPoints)
            {
                tree.Add(new double[] { point.X, point.Y, point.Z }, 1);
            }

            int TotalTreePoints = nearbyBodyPositions.Count;
            //evaluate the body points vs the closest mesh point using the tree
            double currentpoint = 0;
            List<double> distList = new List<double>();


            //we need to find the progress window from the thread process (dont have the window object available from progressbar class)
            //get the current process
            var process = System.Diagnostics.Process.GetCurrentProcess();
            //find the handle of the window on the progress window process
            var progressWindowHandle = process.MainWindowHandle;
            HwndSource hwndSource = HwndSource.FromHwnd(progressWindowHandle);

            //find the window object from the handle
            Window progressWindow = hwndSource.RootVisual as Window;



            double userControlWindowRight = UserControl1.userControlWindowGlobal.Left + UserControl1.userControlWindowGlobal.Width;

            //use the dispatcher of the progress window UI to move the window
            //need to use the dispatcher because it controls the UI form the thread it is being run on, otherwise it throws
            progressWindow.Dispatcher.Invoke( () =>
            {
                progressWindow.Left = userControlWindowRight - progressWindow.Width - 60;
               
            });

            
            //make an event for when you close the window
            //use the event to abort the shortest distance check if the window is closed
            progressWindow.Closed += ProgressWindow_Closed;
            



            foreach (var point in nearbyBodyPositions)
            {
                //checks if window was closed or not
                if (Continue)
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
                    if (currentpoint % 50 == 0)
                    {
                        ProvideUIUpdate((int)Math.Round((currentpoint / TotalTreePoints) * 100), $"{currentpoint}");
                    }
                }
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

            var removeTuple = UserControl1.OutputList.Where(c => c.Item1.ToLower().Contains("collision")).FirstOrDefault();


            //if window was closed dont update PlanCheck Data
            if (Continue)
            {


                UserControl1.OutputList.Remove(removeTuple);

                UserControl1.OutputList.Add(new Tuple<string, string, string, bool?>("Collision2", "No Collision, closest approach >=2cm", (UserControl1.shortestDistanceGlobal != null) ? Math.Round((double)UserControl1.shortestDistanceGlobal, 2).ToString() +
                " cm" : null, (UserControl1.shortestDistanceGlobal >= 2) ? ((UserControl1.shortestDistanceGlobal > 4) ? true : (bool?)null) : false));

                UserControl1.DataGridGlobal.Items.Refresh();
            }

            if (Continue)
            {
                progressWindow.Dispatcher.Invoke(() =>
                {
                    progressWindow.Close();

                });
            }
            //close the progress bar when it finishes
            

            return true;
        }

        

        private void ProgressWindow_Closed(object sender, EventArgs e)
        {
            //trigger variable change to abort the shortest distance check
            Continue = false;
            return;

        }
    }
}
