using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Windows.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Navigation;
using System.Windows.Shapes;
using VMS.TPS.Common.Model.API;
using VMS.TPS.Common.Model.Types;
using HelixToolkit.Wpf;
using System.Numerics;
using MIConvexHull;
using System.Diagnostics;

namespace PlanChecks
{
    /// <summary>
    /// Interaction logic for UserControl1.xaml
    /// </summary>
    public partial class UserControl1 : UserControl
    {
        public static List<Tuple<string, string, string, bool?>> OutputList = new List<Tuple<string, string, string, bool?>>();
        public List<Tuple<string, string, string, bool?>> OutputListRX = new List<Tuple<string, string, string, bool?>>();

        public static ScriptContext context1;

        public static Window userControlWindowGlobal;

        public static ItemsControl checkBoxContainerGlobal;

        public static HelixToolkit.Wpf.HelixViewport3D viewPortGlobal;

        public static List<Tuple<Point3D, string>> arcMeshGlobal;

        public static List<Tuple<Point3D, string>> arcMeshGlobalDisplay;

        public static ModelVisual3D globalModelVisual3D;

        public static List<Point3D> bodyMeshGlobalDisplay;

        public static List<Point3D> bodyMeshGlobal;

        public static List<Point3D> cutoffMeshGlobal;

        public static double? shortestDistanceGlobal;

        public static int plotCounter = 0;

        public static DataGrid DataGridGlobal;

        public static PlanSetup plan;

        public UserControl1(ScriptContext context, Window window1)
        {
            InitializeComponent();

            //clear the ouput list because it is remembering the items from last time you opened the script????
            OutputList.Clear();

            userControlWindowGlobal = window1;


            DataGridGlobal = ReportDataGrid;
            //context1 = context;
            viewPortGlobal = viewport;
            //ComboBox PlanComboBox = this.PlanComboBox;
            // FillPlanComboBox(context, PlanComboBox);


            //}

            //public static void Main(ScriptContext context , ComboBox PlanComboBox, HelixViewport3D viewPort, List<Tuple<string, string, string, bool?>> OutputList, 
            //    List<Tuple<string, string, string, bool?>> OutputListRX, StackPanel HorizontalStackPanel, DataGrid ReportDataGrid, 
            //    DataGrid ReportDataGrid_Rx)
            //{
            //code behind goes here
            //UserControl userControl = new UserControl();

            //OutputList.Clear();
            //OutputListRX.Clear();

            context1 = context;

            Patient mypatient = context.Patient;
            Course course = context.Course;


            //get plan from ui comboBox
            // PlanSetup plan = context.PlansInScope.Where(c => c.Id == (string)PlanComboBox.SelectedItem).FirstOrDefault();
            plan = context.PlanSetup;



            PlanningItem ps1 = null;
            ps1 = (PlanningItem)plan;
            var image = plan.StructureSet.Image;

            bool onemachine = false;
            string machname = "";
            checkOneMachine(plan, out onemachine, out machname);

            bool sameiso = false;
            string isoname = "";
            checkIso(plan, out sameiso, out isoname);

            bool sametech = false;
            string techname = "";
            checktech(plan, out sametech, out techname);


            double expectedRes = 0.00;
            double actualRes = 99.00;
            bool? ResResult = false;
            bool noDose = false;

            expectedRes = techname.StartsWith("SRS") ? 0.125 : 0.250;

            if (plan.Dose != null)
            {
                actualRes = (plan.Dose.XRes / 10);
                if (expectedRes >= actualRes)
                {
                    ResResult = true;
                }
                else if (actualRes < expectedRes)
                {
                    ResResult = null;
                }
            }
            else
            {
                noDose = true;
                ResResult = null;
            }

            string planIntent = plan.PlanIntent;
            if (planIntent != "CURATIVE" && planIntent != "PALLIATIVE" && planIntent != "PROPHYLACTIC") { planIntent = "CHECK INTENT"; }

            double planNorm = plan.PlanNormalizationValue;



            bool samerate = false;
            string ratename = "";
            checkrate(plan, out samerate, out ratename);

            string temp = getPlanMode(plan);
            string algoexpected = "UNKNOWN";
            string algoused = "UNKNOWN";
            bool algomatch = false;

            if (temp == "PHOTON")
            {
                algoexpected = "AAA_1610";
                algoused = plan.PhotonCalculationModel;
                algomatch = (plan.PhotonCalculationModel == "AAA_1610");
            }
            else if (temp == "ELECTRON")
            {
                algoexpected = "EMC_1610";
                algoused = plan.ElectronCalculationModel;
                algomatch = (plan.ElectronCalculationModel == "EMC_1610");
            }

            List<string> couches = new List<string>();

            if (machname == "TrueBeamA" || machname == "TrueBeamF")
            {
                couches.Add("CouchInterior");
                couches.Add("CouchSurface");
                couches.Add("LeftInnerRail");
                couches.Add("LeftOuterRail");
                couches.Add("RightInnerRail");
                couches.Add("RightOuterRail");
            }
            else if (machname == "TrueBeamB" || machname == "TrueBeamNE")
            {
                couches.Add("CouchInterior");
                couches.Add("CouchSurface");
            }
            else
            {
                couches.Add("Error");
            }
            couches.Sort();
            string expectedCouches = String.Join(",\n", couches.ToArray());

            bool samemax = false;
            double globaldosemax = 0.00;
            string volname = "";
            if (!noDose) { maxDoseInPTV(plan, out samemax, out globaldosemax, out volname); }



            string toleranceTables = "";
            tolTablesUsed(plan, out toleranceTables);

            checkplantech(plan, out techname);

            string usebolus = "";
            foreach (var beam in plan.Beams)
            {
                foreach (var bolus in beam.Boluses)
                {
                    if (usebolus != bolus.Id)
                    {
                        usebolus += bolus.Id;
                    }
                }
            }

            /* RX CHECK STUFF
             * 
             * 
             * 
             */
            string modes = "";
            List<Tuple<string, string, string, bool?>> OutputList2 = new List<Tuple<string, string, string, bool?>>()
            {

            };

            if (plan.RTPrescription == null)
            {
                OutputList2.Add(new Tuple<string, string, string, bool?>("RX Available", "Need Rx", "No attached Rx", false));
            }
            else
            {
                foreach (var mode in plan.RTPrescription.EnergyModes)
                {
                    modes += mode;

                }
                string NumOfFx = "";
                string DosePerFx = "";
                string TypeOfRx = "";
                string TargetID = "";
                string ValueRX = "";
                double tempz = 0;

                foreach (var target in plan.RTPrescription.Targets) //currently only returning largest dose target
                {
                    if (tempz < target.DosePerFraction.Dose)
                    {
                        NumOfFx = target.NumberOfFractions.ToString();
                        DosePerFx = target.DosePerFraction.ToString();
                        TypeOfRx = target.Type.ToString();
                        TargetID = target.TargetId;
                        ValueRX = target.Value.ToString();
                        tempz = target.DosePerFraction.Dose;
                    }
                }

                List<string> energList = new List<string>();
                foreach (var energy in plan.RTPrescription.Energies)
                {
                    if (energy == "10xFFF")
                    {
                        energList.Add("10X-FFF"); ;
                    }
                    else if (energy == "6xFFF")
                    {
                        energList.Add("6X-FFF");
                    }
                    else
                    {
                        energList.Add(energy);
                    }
                    energList.Sort();

                }
                string energies = string.Join(", ", energList.ToArray());
                bool techpass = false;
                //checkplantech(plan, out techname, out techpass);
                checkplantechmatchesRX(plan, ref techname, out techpass);




                List<string> replaceStringList = new List<string>();

                if (plan.RTPrescription.Notes != null)
                {
                    string notes = plan.RTPrescription.Notes.Replace("\t", "").Replace("\n", "").Replace("\r", "");


                    if (notes.Length > 42 && notes.Length < 84)
                    {
                        string replaceString = notes.Insert(42, "\n");

                        replaceStringList.Add(replaceString);
                    }
                    if (notes.Length > 84)
                    {
                        string replaceString = notes.Insert(42, "\n");
                        string replaceString1 = replaceString.Insert(84, "\n");

                        replaceStringList.Add(replaceString1);

                    }
                    if (notes.Length > 126)
                    {
                        string replaceString = notes.Insert(42, "\n");
                        string replaceString1 = replaceString.Insert(84, "\n");
                        string replaceString2 = replaceString1.Insert(126, "\n");


                        replaceStringList.Add(replaceString2);

                    }
                    else
                    {
                        replaceStringList.Add(notes);
                    }
                }
                /* RX CHECK STUFF
             * 
             * 
             * 
             */
                //List<Tuple<string, string, string, bool?>> OutputList2 = new List<Tuple<string, string, string, bool?>>()
                //{
                OutputList2.Add(new Tuple<string, string, string, bool?>("Approval", plan.RTPrescription.Status, ((plan.PlanningApproverDisplayName != "") ? "PlanningApproved" : "Not PlanningApproved"), ((plan.RTPrescription.Status == "Approved") ? true : false)));
                OutputList2.Add(new Tuple<string, string, string, bool?>("By", plan.RTPrescription.HistoryUserDisplayName, plan.PlanningApproverDisplayName, (((plan.RTPrescription.HistoryUserDisplayName.ToLower().Contains("attending")) && (plan.RTPrescription.HistoryUserDisplayName == plan.PlanningApproverDisplayName)) ? true : (bool?)null)));
                OutputList2.Add(new Tuple<string, string, string, bool?>("# of Fx", NumOfFx, plan.NumberOfFractions.ToString(), (NumOfFx == plan.NumberOfFractions.ToString())));
                OutputList2.Add(new Tuple<string, string, string, bool?>("Dose/Fx", DosePerFx, plan.DosePerFraction.ToString(), (DosePerFx == plan.DosePerFraction.ToString())));
                OutputList2.Add(new Tuple<string, string, string, bool?>("Mode", modes, getPlanMode(plan), (modes == getPlanMode(plan))));
                OutputList2.Add(new Tuple<string, string, string, bool?>("Target", TargetID, plan.TargetVolumeID, ((TargetID == plan.TargetVolumeID) ? true : (bool?)null)));
                OutputList2.Add(new Tuple<string, string, string, bool?>("Energy", energies, getPlanEnergy(plan), (energies == getPlanEnergy(plan))));
                OutputList2.Add(new Tuple<string, string, string, bool?>("Technique", plan.RTPrescription.Technique, techname, ((techpass) ? true : (bool?)null)));
                OutputList2.Add(new Tuple<string, string, string, bool?>("Gating", ((plan.RTPrescription.Gating == "") ? "NOT GATED" : plan.RTPrescription.Gating), ((plan.UseGating) ? "GATED" : "NOT GATED"), evalGated(plan)));
                OutputList2.Add(new Tuple<string, string, string, bool?>("Bolus", plan.RTPrescription.BolusThickness, usebolus, null));
                OutputList2.Add(new Tuple<string, string, string, bool?>("Notes", replaceStringList.First(), "", null));





                //to add:
                //treatment site AND laterality
                //Time interval (BID)
                //image guidance 
                //};
            }





            //add CheckBoxes to your ItemsControl, name them according to the plan beams, check them
            //this will be the user control to plot or not plot arcs in the collision model
            foreach (var beam in plan.Beams)
            {

                if (beam.IsSetupField == false)
                {
                    CheckBox newCheckBox = new CheckBox();
                    newCheckBox.IsChecked = true;
                    newCheckBox.Content = beam.Id;
                    CheckBoxContainer.Items.Add(newCheckBox);

                    newCheckBox.Checked += NewCheckBox_Checked;
                    newCheckBox.Unchecked += NewCheckBox_Checked;
                }
            }

            //set the global variable to the UserControl ItemsControl so you can access it outside this scope
            checkBoxContainerGlobal = this.CheckBoxContainer;





            string jawtrackingexpected = "Off";
            if (techname == "VMAT" || techname == "SRS/SBRT" || techname == "IMRT")
            {
                jawtrackingexpected = "Enabled";
            }

            string isJawTrackingOn = (plan.OptimizationSetup.Parameters.Any(x => x is OptimizationJawTrackingUsedParameter)) ? "Enabled" : "Off";

            double totalMUdoub = 0;
            if (!noDose) { totalMUdoub = totalMU(plan); }


            double maxBeamMU = 0;
            if (!noDose) { maxBeamMU = maxMU(plan); }

            string diff = (plan.CreationDateTime.Value.Date - plan.StructureSet.Image.CreationDateTime.Value.Date).ToString();
            int CTdiffdays = 0;

            if (diff.ToLower().Contains('.'))
            {
                string[] parts = diff.Split('.');
                string truncatedStr = parts[0];
                CTdiffdays = int.Parse(truncatedStr);
            }
            else
            {

            }
            string artifactChecked = checkArtifact(plan);

            string fullcoverage = "No Dose";

            if (!noDose) {
                //MessageBox.Show("HERE");
                fullcoverage = checkDoseCoverage(plan);
            }


            string needflash = "Flash not used";

            string wrongObjectives = checkObjectives(plan);

            string flash1 = checkIfShouldUseFlash(plan, needflash);
            string flash2 = checkIfUsingFlash(plan, usebolus);

            string rpused = "No";
            string rpavail = "No";
            checkRapidplan(plan, out rpused, out rpavail);


            string checkWedgeMU = "No Dose";
            if (!noDose) { checkWedgeMU = checkEDWmin(plan); }




            //get Vertex beams and check for sketch gantry angles
            string vertexBeams = VertexBeamsToString(plan);
            string sketchVertexBeams = CheckVertexBeam(plan);


            //verify beam doserate is maximum doserate for energy
            string beamNoMaxDoserate = CheckDoseRate(plan);

            //origin is most inf slice, anterior patient R corner
            //CT FOV is 60cm diameter
            //try to find intersection of Body and CT FOV to check for cutoff
            ////////////////////////////////////////////////////////////////
            //string infoString = "";
            //infoString += "userOrigin (" + plan.StructureSet.Image.UserOrigin.x.ToString() + "," + plan.StructureSet.Image.UserOrigin.y.ToString() + "," + plan.StructureSet.Image.UserOrigin.z.ToString() + ")\n";
            //infoString += "Origin (" + plan.StructureSet.Image.Origin.x.ToString() + "," + plan.StructureSet.Image.Origin.y.ToString() + "," + plan.StructureSet.Image.Origin.z.ToString() + ")\n";

            ////find the x,y circle that bounds the FOV (shrunk 5mm?) 
            ////search the external for any points on that circle (sample the circumference at 5mm?)
            //VVector centerOfCirlce = new VVector(plan.StructureSet.Image.Origin.x + 300, plan.StructureSet.Image.Origin.y + 300, 0);

            //infoString += "Center of Circle (" + centerOfCirlce.x.ToString() + " ," + centerOfCirlce.y.ToString() + " )\n";


            //var body = plan.StructureSet.Structures.Where(c => (c.DicomType == "EXTERNAL") || (c.DicomType == "BODY")).FirstOrDefault();

            ////make the points on the circle
            //List<Point> circlePoints = new List<Point>();
            //for (double i = 0; i < 2*Math.PI; i+= 1*(3.14/180))
            //{
            //    double x = 300 * Math.Cos(i);
            //    double y = 300 * Math.Sin(i);

            //    double xOnCircle = centerOfCirlce.x + x;
            //    double yOnCircle = centerOfCirlce.y + y;

            //    circlePoints.Add(new Point(Math.Round(xOnCircle), Math.Round(yOnCircle)));

            //}

            ////select x and y points from the external that satisfy the equation of the circle
            ////need to do some rounding b/c points wont be exact
            //var bodyPoints = body.MeshGeometry.Positions;
            //var correctPoints = bodyPoints.Where(c => circlePoints.Contains(new Point(Math.Round(c.X), Math.Round(c.Y))) == true);

            //MessageBox.Show(correctPoints.Count().ToString());


            ////add the points to the collision model as yellow?
            //var correctPointList = correctPoints.ToList();

            //var corrPointsNoDuplicates = correctPointList.Distinct().ToList();

            //cutoffMeshGlobal = corrPointsNoDuplicates;
            //MessageBox.Show(infoString);

            ///////////////////////////////////////////////////////////////////




            shortestDistanceGlobal = null;

            //create the collision check visual model
            var isocenter = plan.Beams.Where(c => c.IsSetupField == false).FirstOrDefault().IsocenterPosition;
            var meshes = GetBodyAndGantryMeshes(plan);
            arcMeshGlobal = meshes.Item2;
            bodyMeshGlobal = meshes.Item1;
            AssignBodyAndMeshGlobal(meshes.Item2, meshes.Item1);
            CreateVisualModel(arcMeshGlobalDisplay, isocenter, bodyMeshGlobalDisplay, plan);

            bool planHasTargetVolume = false;
            bool planHasIGV = false;

            if (plan.TargetVolumeID != "" && plan.TargetVolumeID != null && jawtrackingexpected == "Enabled")
            {
                planHasTargetVolume = true;

                var IGVStructure = plan.StructureSet.Structures.Where(s => s.Id.ToLower().Contains("igv")).FirstOrDefault();
                if (IGVStructure != null) 
                {
                    planHasIGV = true;
                }
            }

            


            List<Tuple<string, string, string, bool?>> OutputList1 = new List<Tuple<string, string, string, bool?>>()
            {

                new Tuple<string, string, string, bool?>("PlanIntent", "Treatment", planIntent, (planIntent != "CHECK INTENT")? true: false),
                new Tuple<string, string, string, bool?>("Calc Algo", algoexpected, algoused, algomatch),
                new Tuple<string, string, string, bool?>("Calc Res (cm)", expectedRes.ToString(),  actualRes.ToString(), ResResult),
                new Tuple<string, string, string, bool?>("Photon Heterogeneity", "ON", plan.PhotonCalculationOptions["HeterogeneityCorrection"], (plan.PhotonCalculationOptions["HeterogeneityCorrection"] == "ON")),
                new Tuple<string, string, string, bool?>("Slice Thickness", "<= 3mm", image.ZRes.ToString()+ " mm",  (image.ZRes<=3)? true : (bool?)null),
                new Tuple<string, string, string, bool?>("Max # Slices", "< 300", plan.StructureSet.Image.ZSize.ToString(),  (plan.StructureSet.Image.ZSize<300)? true : (bool?)null),
                new Tuple<string, string, string, bool?>("Days b/w CT and Plan", "< 21 days", CTdiffdays.ToString() + " days",  (CTdiffdays<21)? true : (bool?)null),



                new Tuple<string, string, string, bool?>("User Origin matches CT ISO", "Match", findIsoStructure(plan),  (findIsoStructure(plan) == "MATCHES")? true: (bool?)null  ),
                new Tuple<string, string, string, bool?>("Same Machine", "All Fields", machname, onemachine),
                new Tuple<string, string, string, bool?>("Same Iso", "All Fields", isoname, sameiso),
                new Tuple<string, string, string, bool?>("Same Tech", "All Fields", techname, sametech),
                new Tuple<string, string, string, bool?>("Same Dose Rate", "All Fields", ratename, samerate),
                new Tuple<string, string, string, bool?>("Dose Rate is Maximum", "All Fields", beamNoMaxDoserate,  (beamNoMaxDoserate == "All Fields")? true: false),
                new Tuple<string, string, string, bool?>("DRRs attached", "All Fields", findDRR(plan).ToString(), findDRR(plan)),
                new Tuple<string, string, string, bool?>("Tol Table Set", "All Fields", toleranceTables, (toleranceTables != "placeholder" && toleranceTables!= "Mixed Tables" && toleranceTables!= "Error" && toleranceTables!="Missing for some beams")? true:  false),
                new Tuple<string, string, string, bool?>("Image and Tx Orientation", "Same", ((image.ImagingOrientation== plan.TreatmentOrientation) ? plan.TreatmentOrientation.ToString() : "DIFFERENT"), (image.ImagingOrientation== plan.TreatmentOrientation)),
                //baseplate?
                new Tuple<string, string, string, bool?>("Known Assigned HU", "if present",  artifactChecked, (!artifactChecked.ToLower().Contains("wrong"))? true :false),
                new Tuple<string, string, string, bool?>("Other Assigned HU",  "None",  getDensityOverrides(plan),  (getDensityOverrides(plan)=="None")? true : (bool?)null),
                new Tuple<string, string, string, bool?>("Empty Structures", "None", findEmptyStructure(plan), (findEmptyStructure(plan)== "None")),
                new Tuple<string, string, string, bool?>("IGV Structure", (planHasTargetVolume)? "Plan Target": "None Needed" , (planHasIGV)? "IGV Found": "No IGV Found",  ((planHasTargetVolume&&planHasIGV)||(!planHasTargetVolume&&!planHasIGV))? true : false),

                new Tuple<string, string, string, bool?>("Jaw Tracking", jawtrackingexpected.ToString(), isJawTrackingOn.ToString(),  (isJawTrackingOn == jawtrackingexpected)? true : false),

                new Tuple<string, string, string, bool?>("Wedges MU", ">=20", checkWedgeMU,  (checkWedgeMU == "Wedges ok" || checkWedgeMU == "No wedges")? true : false),



                new Tuple<string, string, string, bool?>("Total Plan MU", "<=4000", totalMUdoub.ToString(),  (totalMUdoub <= 4000)? true : (bool?)null),
                new Tuple<string, string, string, bool?>("Beam Max MU", "<=1200", maxBeamMU.ToString(),  (maxBeamMU<1200)? true : (bool?)null),
                new Tuple<string, string, string, bool?>("Dose Max in Target",  globaldosemax.ToString() + "% ",  volname,  (samemax)? true : (bool?)null),
                new Tuple<string, string, string, bool?>("Structures in Calc Volume",  "100% ",  fullcoverage,  (fullcoverage=="100%")? true : false),
                //new Tuple<string, string, string, bool?>("Flash VMAT", flash1, flash2, (flash1 == flash2) ? true : false),
                //new Tuple<string, string, string, bool?>("RapidPlan Used", rpavail, rpused, (rpavail == rpused) ? true : (bool?)null),
                new Tuple<string, string, string, bool?>("Objective Priorities", "<999", wrongObjectives, (wrongObjectives=="<999")? true : false),
                new Tuple<string, string, string, bool?>("Couch Added", expectedCouches, findSupport(plan), (expectedCouches == findSupport(plan)) ? true : (bool?)null),
                new Tuple<string, string, string, bool?>("Vertex Beams", (vertexBeams == "")? "no vertex beams": vertexBeams , (sketchVertexBeams == "")? "" : "check for clearence \n" + sketchVertexBeams, (sketchVertexBeams == "")? true: false),
                new Tuple<string, string, string, bool?>("Collision", "Collision not computed", null , null)


            
            //stray voxels - check if gaps between slices? check volume of parts somehow?  GetNumberOfSeparateParts()


            //normalization mode?

            //if PRIMARY reference point, that it's getting Rx dose

            //reference point dose limit vs actual reference point dose

            //reference point doses are accurate to actual doses (cord point is true cord max, etc)
            //beam names make sense (laterality, and Beam name matches ID ((to avoid ARC1 name, ARC2 ID)))

            //check plan scheduling from here? 


        };

            string findLat = findLaterality(plan);
            if (plan.RTPrescription != null)
            {
                if(findLat == "NAN")
                {

                }
                else if (plan.RTPrescription.Id.ToLower().Contains("prostate") && findLat == "CENTRAL")
                {
                    OutputList1.Add(new Tuple<string, string, string, bool?>("Target Laterality", plan.RTPrescription.Id, findLat, true));

                }
                else if ((plan.RTPrescription.Id.ToLower().Contains("left") ||
                    plan.RTPrescription.Id.ToLower().Contains("lul") ||
                    plan.RTPrescription.Id.ToLower().Contains("lll")) && findLat == "LEFT")
                {
                    OutputList1.Add(new Tuple<string, string, string, bool?>("Target Laterality", plan.RTPrescription.Id, findLat, true));

                }
                else if ((plan.RTPrescription.Id.ToLower().Contains("left") ||
                    plan.RTPrescription.Id.ToLower().Contains("lul") ||
                    plan.RTPrescription.Id.ToLower().Contains("lll")) && findLat != "LEFT")
                {
                    OutputList1.Add(new Tuple<string, string, string, bool?>("Target Laterality", plan.RTPrescription.Id, findLat, false));

                }
                else if ((plan.RTPrescription.Id.ToLower().Contains("right") ||
                    plan.RTPrescription.Id.ToLower().Contains("rul") ||
                    plan.RTPrescription.Id.ToLower().Contains("rll")) && findLat == "RIGHT")
                {
                    OutputList1.Add(new Tuple<string, string, string, bool?>("Target Laterality", plan.RTPrescription.Id, findLat, true));

                }
                else if ((plan.RTPrescription.Id.ToLower().Contains("right") ||
                    plan.RTPrescription.Id.ToLower().Contains("rul") ||
                    plan.RTPrescription.Id.ToLower().Contains("rll")) && findLat != "RIGHT")
                {
                    OutputList1.Add(new Tuple<string, string, string, bool?>("Target Laterality", plan.RTPrescription.Id, findLat, false));

                }
                else
                {
                    OutputList1.Add(new Tuple<string, string, string, bool?>("Target Laterality", plan.RTPrescription.Id, findLat, (bool?)null));
                }
            }

            string gantryDir = alternatingGantryDir(plan);
            if (gantryDir != "SKIP")
            {
                string diffCol = differentCollimatorAngles(plan);

                OutputList1.Add(new Tuple<string, string, string, bool?>("Gantry Directions", "Should Alternate", gantryDir, (gantryDir == "Alternates") ? true : (bool?)null));
                OutputList1.Add(new Tuple<string, string, string, bool?>("Diff Coll Angles", "Different", diffCol, (diffCol == "Different") ? true : (bool?)null));

            }

            //                OutputList2.Add(new Tuple<string, string, string, bool?>("Gating", ((plan.RTPrescription.Gating == "") ? "NOT GATED" : plan.RTPrescription.Gating), ((plan.UseGating) ? "GATED" : "NOT GATED"), evalGated(plan)));

            if (plan.Id.ToLower().Contains("bh_") || plan.Id.ToLower().Contains("g_"))
            {
                //breathhold or gating plan expected
                if (plan.UseGating) { 
                    OutputList1.Add(new Tuple<string, string, string, bool?>("G_ or BH_ Name", "Gating", "Gating", true));
                }
                else
                {
                    OutputList1.Add(new Tuple<string, string, string, bool?>("G_ or BH_ Name", "Gating", "NOT GATED", false));
                }
            }

            else
            {
                if (plan.UseGating)
                {
                    OutputList1.Add(new Tuple<string, string, string, bool?>("G_ or BH_ Name", "Gating", "PLAN NAME PREFIX", false));
                }
            }



            //make max X FS check conditional on if plan has vmat or dynamic conformal arc
            bool maxFSCheck = false;
            foreach (var beam in plan.Beams)
            {
                if (beam.IsSetupField == false)
                {
                    if (beam.MLCPlanType.ToString() == "VMAT" || beam.MLCPlanType.ToString() == "ArcDynamic")
                    {
                        maxFSCheck = true;
                    }
                    
                }
            }

            if (maxFSCheck)
            {
                var failMaxFSCheckList = CheckMaxFieldSize(plan);
                string failBeams = "";
                if (failMaxFSCheckList.Any())
                {
                    foreach (var beam in failMaxFSCheckList)
                    {
                        if (beam.MLCPlanType.ToString() == "ArcDynamic")
                        {
                            failBeams += beam.Id + " 3DConformalArc" + ";";
                        }
                        else
                        {
                            failBeams += beam.Id + ";";
                        }
                    }
                }

                bool? FSResult;
                if (failBeams.Contains("3DConformalArc") & failMaxFSCheckList.Count() == 1)
                {
                    FSResult = null;
                }
                else if (failMaxFSCheckList.Any() & failBeams.Contains("3DConformalArc") == false)
                {
                    FSResult = false;

                }
                else
                {
                    FSResult = true;

                }

                string reviewedBy = plan.ApprovalHistory.Where(approvalhistory => approvalhistory.ApprovalStatus == PlanSetupApprovalStatus.Reviewed).FirstOrDefault().UserDisplayName;
                string planApprovedBy = plan.ApprovalHistory.Where(approvalhistory => approvalhistory.ApprovalStatus == PlanSetupApprovalStatus.PlanningApproved).FirstOrDefault().UserDisplayName;


                if (reviewedBy == null) reviewedBy = "NOT REVIEWED";
                if (reviewedBy != "Joshua James" && reviewedBy != "Brian Vincent" && reviewedBy != "Megan Blackburn" && reviewedBy != "Keith Sowards" && reviewedBy != "Christine Swanson") reviewedBy = "NOT REVIEWED";

                if (techname == "VMAT" || techname == "SRS/SBRT" || techname == "IMRT")
                {
                    if (planApprovedBy != null) OutputList.Add(new Tuple<string, string, string, bool?>("Reviewed By Physics", "Reviewed", reviewedBy, (reviewedBy != "NOT REVIEWED") ? true : false));
                    OutputList.Add(new Tuple<string, string, string, bool?>("Max X Jaw Size", "<15.6cm", (failMaxFSCheckList.Any() == true) ? failBeams + " fail" : "all fields <15.6cm", FSResult));

                }
                OutputList.Add(new Tuple<string, string, string, bool?>("Plan Normalization", "95% to 105%", (Math.Round(planNorm, 2)).ToString(), (planNorm >= 95 && planNorm <= 105) ? true : (bool?)null));

                var pacemaker = plan.StructureSet.Structures.FirstOrDefault(s => s.Id.ToLower().Contains("pacemaker"));

                if (pacemaker != null) {
                    OutputList.Add(new Tuple<string, string, string, bool?>("Pacemaker", "6 or 6FFF only", getPlanEnergy(plan), (getPlanEnergy(plan) == "6X" || getPlanEnergy(plan) == "6X-FFF") ? true : false));
                 }




                if (!planHasSpecialChar(plan))
                {
                    OutputList1.Add(new Tuple<string, string, string, bool?>("Plan Name", "No Special Characters", "FALSE", false));

                }
                if (!fieldHasSpecialChar(plan))
                {
                    OutputList1.Add(new Tuple<string, string, string, bool?>("Field Name", "No Special Characters", "FALSE", false));

                }
                

            }

            if (plan.StructureSet.Image.Id.ToLower().Contains("bh"))
            {
                OutputList.Add(new Tuple<string, string, string, bool?>("CT Image", plan.StructureSet.Image.Id, ((plan.UseGating) ? "GATED" : "NOT GATED"), plan.UseGating));

            }


            double minMUDEG = 99999;
            bool atleastoneVMATARCDYN = false;

            foreach (var beam in plan.Beams)
            {
                if (beam.MLCPlanType.ToString() == "VMAT" || beam.MLCPlanType.ToString() == "ArcDynamic")
                {
                    atleastoneVMATARCDYN = true;

                    // Get from plan:
                    var metersetWeights = beam.ControlPoints.Select(cp => cp.MetersetWeight).ToList();
                    var gantryAngles = beam.ControlPoints.Select(ga => ga.GantryAngle).ToList();
                    var monitorUnit = beam.Meterset.Value;

                    double dMW = 0.0;
                    double dangle = 0.0;

                    for (int k = 0; k < metersetWeights.Count() - 1; k++)
                    {
                        dMW = (metersetWeights[k + 1] - metersetWeights[k]) * monitorUnit;
                        dangle = (180.0 - Math.Abs((Math.Abs(gantryAngles[k + 1] - gantryAngles[k]) - 180.0)));
                        if ((dMW / dangle) < minMUDEG && dMW>0)
                        {
                            minMUDEG = dMW / dangle;
                        }
                    }
                }
            }

            string modeZ = getPlanMode(plan);
            if (modeZ == "ELECTRON")
            {
                if (plan.Dose.DoseMax3D.Dose != 0)
                {
                    if (plan.Dose.DoseMax3D.Dose < 115)
                    {
                        OutputList1.Add(new Tuple<string, string, string, bool?>("Max Electron Dose", "<115%", Math.Round(plan.Dose.DoseMax3D.Dose, 2).ToString(), true));
                    }
                    else
                    {
                        OutputList1.Add(new Tuple<string, string, string, bool?>("Max Electron Dose", "<115%", Math.Round(plan.Dose.DoseMax3D.Dose, 2).ToString(), false));

                    }

                }
                else
                {
                    //no dose
                    OutputList1.Add(new Tuple<string, string, string, bool?>("Max Electron Dose", "<115%", "NO DOSE", false));

                }
            }


            if (atleastoneVMATARCDYN)
            {
                if (minMUDEG <= 0.1)
                {
                    OutputList1.Add(new Tuple<string, string, string, bool?>("Minimum MU/Deg", ">0.1", Math.Round(minMUDEG, 2).ToString(), false));
                }
                else
                {
                    OutputList1.Add(new Tuple<string, string, string, bool?>("Minimum MU/Deg", ">0.1", Math.Round(minMUDEG, 2).ToString(), true));
                }
            }



            //take this out, we do SRS at NE now with 6DoF couch
            //if (machname == "TrueBeamNE")
            //{
            //    var beamNE = plan.Beams.FirstOrDefault(s => s.IsSetupField != true);
            //    if (beamNE.Technique.Id == "SRS ARC" || beamNE.Technique.Id == "SRS STATIC")
            //    {

            //        OutputList1.Add(new Tuple<string, string, string, bool?>("Technique", "NO SRS AT NE", "NO SRS AT NE", false));
            //    }
            //}

            OutputList.Reverse();
            OutputListRX.Reverse();


            //output looks something like this
            OutputList.AddRange(OutputList1);
            OutputListRX.AddRange(OutputList2);



            //code for databinding the list of plan check items to the data grid
            this.ReportDataGrid.ItemsSource = OutputList;
            this.ReportDataGrid_Rx.ItemsSource = OutputListRX;

            ReportDataGrid.HeadersVisibility = DataGridHeadersVisibility.Column;
            ReportDataGrid_Rx.HeadersVisibility = DataGridHeadersVisibility.Column;


            ReportDataGrid.AutoGenerateColumns = true;
            ReportDataGrid_Rx.AutoGenerateColumns = true;


            this.ReportDataGrid.Items.Refresh();
            this.ReportDataGrid_Rx.Items.Refresh();



            //ReportDataGrid.ColumnWidth = HorizontalStackPanel.Width / 4;
            ReportDataGrid.ColumnWidth = HorizontalStackPanel.Width / 6;

            //ReportDataGrid_Rx.ColumnWidth = HorizontalStackPanel.Width / 4;
            ReportDataGrid_Rx.ColumnWidth = HorizontalStackPanel.Width / 6;

            //programmatically start the collision check on UI loading
            this.Loaded += UserControl1_Loaded;





        }



        private void UserControl1_Loaded(object sender, RoutedEventArgs e)
        {
            //add a slight delay before we programmatically click the progress bar button, otherwise the UI wont have time to load before we start the other thread

            DispatcherTimer dispatcherTimer = new DispatcherTimer();

            //this cant be too long or you try to get thw root window of the progress bar before its opened
            dispatcherTimer.Interval = TimeSpan.FromMilliseconds(15);

            dispatcherTimer.Tick += (s, args) =>
            {
                dispatcherTimer.Stop();

                Button_Click(null, null);
            };

            dispatcherTimer.Start();

            

        }

        public static bool planHasSpecialChar(PlanSetup plan)
         {
            string input = plan.Id;

            bool result = input.All(c => Char.IsLetterOrDigit(c) || c == '_' | c == ':' | c == ' ');

            return result;
            
        }

        public static bool structureHasSpecialChar(PlanSetup plan)
        {
            string input = "";
            bool result = true;

            foreach (Structure structurex in plan.StructureSet.Structures)
            {
                input = structurex.Id;
                result = input.All(c => Char.IsLetterOrDigit(c) || c == '_' | c == ':' | c == '-' | c == ' ');
                if (!result) { return result; } //if ever find a false, exit loop now. it's false. 

            }
            return result;
        }

        public static bool fieldHasSpecialChar(PlanSetup plan)
        {
            string input = "";
            bool result = true;

            foreach (var beam in plan.Beams)
            {
                input = beam.Id;
                result = input.All(c => Char.IsLetterOrDigit(c) || c == '_' | c == ':' | c == '-' | c == ' ');
                if (!result) { return result; } //if ever find a false, exit loop now. it's false. 

            }
            return result;
        }

        public static string findLaterality(PlanSetup plan)
        {
            var body = plan.StructureSet.Structures.Where(c => (c.DicomType == "EXTERNAL") || (c.DicomType == "BODY")).FirstOrDefault();

            var targetStructure = plan.StructureSet.Structures.Where(c => (c.DicomType.ToLower().Contains("ptv")) || (c.DicomType.ToLower().Contains("ctv")) || (c.DicomType.ToLower().Contains("gtv"))).FirstOrDefault();
            if (targetStructure == null) { return "NAN"; }
            string laterality = "";
            if (body.CenterPoint.x - targetStructure.CenterPoint.x > 10)
            {
                laterality = "RIGHT";
            }
            else if (body.CenterPoint.x - targetStructure.CenterPoint.x < -10)
            {
                laterality = "LEFT";
            }
            else
            {
                laterality = "CENTRAL";
            }

            return laterality;
        }

        public static string CheckDoseRate(PlanSetup plan)
        {
            Dictionary<string, int> DoseRateDict = new Dictionary<string, int>();

            DoseRateDict.Add("6X", 600);
            DoseRateDict.Add("6X-FFF", 1400);
            DoseRateDict.Add("10X", 600);
            DoseRateDict.Add("10X-FFF", 2400);
            DoseRateDict.Add("18X", 600);
            DoseRateDict.Add("6E", 1000);
            DoseRateDict.Add("9E", 1000);
            DoseRateDict.Add("12E", 1000);
            DoseRateDict.Add("16E", 1000);
            DoseRateDict.Add("20E", 1000);

            bool doserateMatch = true;
            string beamNoMaxDoserate = "All Fields";
            int counter = 0;
            foreach (var beam in plan.Beams.Where(c => c.IsSetupField == false).ToList())
            {

                if (doserateMatch == true)
                {

                    bool doesItMatch = (DoseRateDict[beam.EnergyModeDisplayName] == beam.DoseRate) ? true : false;

                    doserateMatch = doesItMatch;


                    if (doserateMatch == false)
                    {
                        if (counter == 0)
                        {
                            beamNoMaxDoserate = "";
                        }
                        counter++;
                        beamNoMaxDoserate += beam.Id + " ";
                    }

                }
                else
                {
                    bool doesItMatch = (DoseRateDict[beam.EnergyModeDisplayName] == beam.DoseRate) ? true : false;

                    doserateMatch = doesItMatch;
                    if (doserateMatch == false)
                    {
                        beamNoMaxDoserate += beam.Id + " ";

                    }
                }

            }

            if (counter > 0)
            {
                beamNoMaxDoserate += " Not Max Dose Rate";
            }


            return beamNoMaxDoserate;

        }


        public static string differentCollimatorAngles(PlanSetup plan)
        {

            bool isUnique = false;
            string returner = "Same";

            List<double> collAngles = new List<double>();

            foreach (var beam in plan.Beams) {
                if (!beam.IsSetupField)
                {
                    collAngles.Add(beam.ControlPoints.First().CollimatorAngle);
                }
            }

            isUnique = collAngles.Distinct().Count() == collAngles.Count();
            if (isUnique) { returner = "Different"; }

            return returner;
        }
        public static string alternatingGantryDir(PlanSetup plan)
        {

            var CheckBeamsForVMAT = plan.Beams.Where(b => b.IsSetupField != true).FirstOrDefault();

            if (CheckBeamsForVMAT.MLCPlanType != MLCPlanType.VMAT)
            {
                return "SKIP";
            }
            else
            {
                int beamcount = 0;
                int CWcount = 0;
                int CCWcount = 0;

                foreach (var beam in plan.Beams)
                {
                    if (!beam.IsSetupField)
                    {
                        beamcount++;
                        if (beam.GantryDirection == GantryDirection.Clockwise)
                        {
                            CWcount++;
                        }
                        else if (beam.GantryDirection == GantryDirection.CounterClockwise)
                        {
                            CCWcount++;
                        }
                    }
                }

                if (beamcount <= 1)
                {
                    return "SKIP";
                }
                else if (beamcount <= 3)
                {
                    if (CWcount >= 1 && CCWcount >= 1)
                    {
                        return "Alternates";
                    }
                    else return "NO";
                }
                else if (beamcount <= 5)
                {
                    if (CWcount >= 2 && CCWcount >= 2)
                    {
                        return "Alternates";
                    }
                    else return "NO";
                }
                else if (beamcount <= 7)
                {
                    if (CWcount >= 3 && CCWcount >= 3)
                    {
                        return "Alternates";
                    }
                    else return "NO";
                }
                else if (beamcount <= 9)
                {
                    if (CWcount >= 4 && CCWcount >= 4)
                    {
                        return "Alternates";
                    }
                    else return "NO";
                }
                else return "???";





            }


        }


        public static string VertexBeamsToString(PlanSetup plan)
        {
            string BeamString = "";

            try
            {
                List<string> vertexBeamList = plan.Beams.Where(c => (c.ControlPoints.First().PatientSupportAngle == 90)
            || (c.ControlPoints.First().PatientSupportAngle == 270)).Select(c => c.Id).ToList();


                foreach (var beamId in vertexBeamList)
                {
                    BeamString += beamId + " \n";
                }
            }
            catch (Exception)
            {
                BeamString = "";

            }

            return BeamString;
        }
        /// <summary>
        /// Checks the gantry angle stop and finish position for couch kick 90 and 270. If they are > 5 degrees off towards the body inferior direction it throws a warning
        /// with the name of the beam
        /// </summary>
        /// <param name="planSetup"></param>
        public static string CheckVertexBeam(PlanSetup planSetup)
        {
            List<string> sketchBeamList = new List<string>();
            foreach (var beam in planSetup.Beams.Where(c => c.IsSetupField == false).ToList())
            {
                //90 is 270 in Eclipse
                if (beam.ControlPoints.First().PatientSupportAngle == 270)
                {

                    if ((beam.ControlPoints.First().GantryAngle > 185 && beam.ControlPoints.First().GantryAngle < 355) == true ||
                        (beam.ControlPoints.Last().GantryAngle > 185 && beam.ControlPoints.Last().GantryAngle < 355) == true)
                    {
                        sketchBeamList.Add(beam.Id);
                    }

                }
                //270 is 90 in Eclipse
                else if (beam.ControlPoints.First().PatientSupportAngle == 90)
                {

                    if ((beam.ControlPoints.First().GantryAngle > 5 & beam.ControlPoints.First().GantryAngle < 175) ||
                        (beam.ControlPoints.Last().GantryAngle > 5 & beam.ControlPoints.Last().GantryAngle < 175))
                    {
                        sketchBeamList.Add(beam.Id);
                    }

                }
            }

            string sketchBeams = "";
            foreach (var thing in sketchBeamList)
            {
                sketchBeams += thing + " \n";
            }

            return sketchBeams;
        }



        public static void checkRapidplan(PlanSetup plan, out string rpused, out string rpavail)
        {
            rpused = "No";
            rpavail = "No";
            var CheckBeamsForVMAT = plan.Beams.Where(b => b.IsSetupField != true).FirstOrDefault();

            if (plan.DVHEstimates.Count() != 0)
            {
                rpused = "Yes";
            }

            if (CheckBeamsForVMAT.MLCPlanType == MLCPlanType.VMAT && (plan.Id.ToLower().Contains("prostate") || plan.Id.ToLower().Contains("lung") || plan.Id.ToLower().Contains("apbi") || plan.Id.ToLower().Contains("gbm") || plan.Id.ToLower().Contains("pelvis")))
            {
                rpavail = "Yes";
            }
        }

        public static List<Beam> CheckMaxFieldSize(PlanSetup plan)
        {
            List<Beam> failMaxFSList = new List<Beam>();

            List<Beam> vmatList = new List<Beam>();
            foreach (var beam in plan.Beams)
            {

                if (beam.ControlPoints.First().GantryAngle != beam.ControlPoints.Last().GantryAngle)
                {
                    vmatList.Add(beam);

                }

            }
            if (vmatList.Any())
            {
                foreach (var beam in vmatList)
                {
                    foreach (var controlPoint in beam.ControlPoints)
                    {
                        VRect<double> jawPositions = controlPoint.JawPositions;

                        double xFieldSize = Math.Abs(jawPositions.X1) + Math.Abs(jawPositions.X2);

                        if (xFieldSize > 156)
                        {
                            failMaxFSList.Add(beam);
                            break;
                        }
                    }
                }
            }

            return failMaxFSList;

        }

        public static string checkObjectives(PlanSetup plan)
        {

            var objectives = plan.OptimizationSetup.Objectives;
            foreach (var objective in objectives)
            {
                if (objective.Priority == 999)
                {
                    return "999!!!";
                }

            }
            return "<999";
        }

        public static string checkIfShouldUseFlash(PlanSetup plan, string needflash)
        {

            // MessageBox.Show(plan.Id);
            if (plan.Id.Contains("APBI"))
            {
                needflash = "Flash not used";
                return needflash;
            }

            var CheckBeamsForVMAT = plan.Beams.Where(b => b.IsSetupField != true).FirstOrDefault();

            if (CheckBeamsForVMAT.MLCPlanType == MLCPlanType.VMAT)
            {
                if (plan.Id.Contains("CW") || plan.Id.ToLower().Contains("breast") || plan.Id.ToLower().Contains("brst"))
                {
                    needflash = "Flash used";
                    return needflash;
                }
            }
            return needflash;
        }

        public static string checkIfUsingFlash(PlanSetup plan, string usebolus)
        {
            //when do we need flash? when it's chestwall or breast site. when it's VMAT technique. 
            //how to tell we used it? when there's an EXP structure and an unlinked bolus. (but could be flash + bolus...)
            string useflash = "Flash not used";
            var CheckExpStructures = plan.StructureSet.Structures.Where(s => s.Id.ToLower().Contains("exp") || s.Id.ToLower().Contains("opt")).FirstOrDefault();
            var CheckBolusStructures = plan.StructureSet.Structures.Where(s => s.DicomType == "BOLUS").FirstOrDefault();
            var CheckBeamsForVMAT = plan.Beams.Where(b => b.IsSetupField != true).FirstOrDefault();

            if (CheckExpStructures != null && usebolus == "" && CheckBolusStructures != null && CheckBeamsForVMAT.MLCPlanType == MLCPlanType.VMAT)
            {
                //if there's an "expanded" structure 
                //if there's no linked bolus to beams
                //if there's a bolus structure
                //if it's a vmat plan
                useflash = "Flash used";
            }

            return useflash;

        }

        public static string checkEDWmin(PlanSetup plan)
        {
            string result = "Wedges ok";
            bool anywedges = false;

            foreach (var beam in plan.Beams)
            {
                if (beam.Wedges.Count() > 0)
                { //beam has wedge
                    anywedges = true;
                    if (beam.Meterset.Value < 20.00)
                    {
                        result = beam.Meterset.Value.ToString("G17");
                    }
                }
            }

            if (anywedges == false)
            {
                result = "No wedges";
            }

            return result;
        }

        public static double maxMU(PlanSetup plan)
        {

            double maxsofar = 0;

            foreach (var beam in plan.Beams)
            {
                if (!beam.IsSetupField)
                {
                    maxsofar = (Math.Round(beam.Meterset.Value) > maxsofar) ? Math.Round(beam.Meterset.Value) : maxsofar;
                }
            }




            return (maxsofar);



        }

        public static double totalMU(PlanSetup plan)
        {

            double total = 0;

            foreach (var beam in plan.Beams)
            {
                if (!beam.IsSetupField)
                {
                    total += Math.Round(beam.Meterset.Value);
                }
            }



            return total;



        }


        public static void tolTablesUsed(PlanSetup plan, out string toleranceTables)
        {
            toleranceTables = "placeholder";

            foreach (var beam in plan.Beams)
            {
                if (beam.ToleranceTableLabel == "") //if any beam tolerance table is blank, this is a fail, exit now
                {
                    toleranceTables = "Missing for some beams";
                    break;
                }
                else if (beam.ToleranceTableLabel != toleranceTables) //if it's not blank, does it differ from our variable?
                {
                    if (toleranceTables == "placeholder") //does it differ because the variable is still a placeholder variable? 
                    {
                        toleranceTables = beam.ToleranceTableLabel; //remove placeholder and set variable to our new tolerance table value
                    }
                    else //or because there are two different tolerance tables being used? 
                    {
                        toleranceTables = "Mixed Tables"; //we have multiple tolerance tables

                    }
                }
                else if (beam.ToleranceTableLabel == toleranceTables)
                {
                }
                else
                {
                    toleranceTables = "Error";
                }

            }
        }
        public static void checkplantechmatchesRX(PlanSetup plan, ref string techname, out bool techpass)
        {
            //bool sametech = true;

            techpass = false;

            if (techname == "3 D CRT")
            {
                if (plan.RTPrescription.Technique.ToLower() == "ap/pa" || plan.RTPrescription.Technique.ToLower() == "opposed laterals" || plan.RTPrescription.Technique.ToLower() == "3 d crt")
                {
                    techpass = true;
                }

            }
            else if (techname == "SRS/SBRT")
            {
                if (plan.RTPrescription.Technique.ToLower() == "srs" || plan.RTPrescription.Technique.ToLower() == "sbrt")
                {
                    techpass = true;
                }

            }
            else if (plan.RTPrescription.Technique == techname)
            {
                techpass = true;
            }
            else if (plan.RTPrescription.Technique == "IMRT" && techname == "VMAT")
            {
                techpass = true;
            }
            else
            {
                techpass = false;
            }
        }

        public static void checkplantech(PlanSetup plan, out string techname)
        {

            bool sametech = true;
            string tech = "ZZZZZZZZZ";
            string temptech = "";
            int numControlPts = 0;
            //techpass = false;

            string[] electrons = { "6E", "9E", "12E", "16E", "20E" };

            foreach (var beam in plan.Beams)
            {
                if (!beam.IsSetupField)
                {
                    if (tech == "ZZZZZZZZZ")
                    {
                        if (electrons.Contains(beam.EnergyModeDisplayName))
                        {
                            tech = "Electron, en-face";
                        }
                        else if (beam.MLCPlanType == MLCPlanType.VMAT)
                        {
                            if (beam.Technique.Id.StartsWith("SRS"))
                            {
                                tech = "SRS/SBRT";
                            }
                            else
                            {
                                tech = "VMAT";
                            }
                        }
                        else if (beam.MLCPlanType == MLCPlanType.Static || beam.MLCPlanType == MLCPlanType.NotDefined)
                        {
                            tech = "3 D CRT";
                        }
                        else if (beam.MLCPlanType == MLCPlanType.DoseDynamic)
                        {
                            foreach (var controlPoint in beam.ControlPoints)
                            {
                                numControlPts++;
                            }
                            if (numControlPts <= 5)
                            {
                                tech = "FIF";
                            }
                            else
                            {
                                tech = "IMRT";
                            }
                            numControlPts = 0;
                        }
                        else
                        {
                            tech = beam.Technique.Id;
                        }
                    }
                    else
                    {
                        if (electrons.Contains(beam.EnergyModeDisplayName))
                        {
                            temptech = "Electron, en-face";
                        }
                        else if (beam.MLCPlanType == MLCPlanType.VMAT)
                        {
                            if (beam.Technique.Id.StartsWith("SRS"))
                            {
                                temptech = "SRS/SBRT";
                            }
                            else
                            {
                                temptech = "VMAT";
                            }
                        }
                        else if (beam.MLCPlanType == MLCPlanType.Static || beam.MLCPlanType == MLCPlanType.NotDefined)
                        {
                            temptech = "3 D CRT";
                        }
                        else if (beam.MLCPlanType == MLCPlanType.DoseDynamic)
                        {
                            foreach (var controlPoint in beam.ControlPoints)
                            {
                                numControlPts++;
                            }
                            if (numControlPts <= 5)
                            {
                                tech = "FIF";
                            }
                            else
                            {
                                tech = "IMRT";
                            }
                            numControlPts = 0;

                            
                        }
                        else
                        {
                            temptech = beam.Technique.Id;
                        }

                        if (temptech != tech)
                        {
                            sametech = false;
                        }
                    }
                }

            }






            //3D = all beams with MLC "Static"" or "NotDefined"
            //FIF = one beam with DoseDynamic AND <=5 control points
            //IMRT = DoseDynamic AND > 15 control points
            //VMAT = MLC technique = VMAT
            //what about "ArcDynamic"?


            if (sametech)
            {
                techname = tech;
            }
            else
            {
                techname = "Mixed";
            }
        }

        public static string getApprovalStatus(PlanSetup plan)
        {
            string approval = "";
            switch (plan.ApprovalStatus)
            {
                case PlanSetupApprovalStatus.Rejected:
                    approval = "Rejected";
                    break;
                case PlanSetupApprovalStatus.UnApproved:
                    approval = "Unapproved";
                    break;
                case PlanSetupApprovalStatus.Reviewed:
                    approval = "Reviewed";
                    break;
                case PlanSetupApprovalStatus.PlanningApproved:
                    approval = "PlanningApproved";
                    break;
                case PlanSetupApprovalStatus.TreatmentApproved:
                    approval = "TreatmentApproved";
                    break;
                case PlanSetupApprovalStatus.CompletedEarly:
                    approval = "CompletedEarly";
                    break;
                case PlanSetupApprovalStatus.Completed:
                    approval = "Completed";
                    break;
                case PlanSetupApprovalStatus.Retired:
                    approval = "Retired";
                    break;
                default:
                    approval = "";
                    break;
            }

            return approval;
        }

        public static string getPlanEnergy(PlanSetup plan)
        {

            HashSet<string> termsSet = new HashSet<string>();

            foreach (var beam in plan.Beams)
            {
                if (!beam.IsSetupField)
                {
                    termsSet.Add(beam.EnergyModeDisplayName);  //hashset only allows unique values, so duplicates are skipped in this step
                }
            }

            List<string> sortedList = new List<string>(termsSet);
            sortedList.Sort();
            termsSet = new HashSet<string>(sortedList);

            string combinedString = string.Join(", ", sortedList);

            return combinedString;



        }
        public static bool evalGated(PlanSetup plan)
        {
            if (plan.UseGating == !string.IsNullOrEmpty(plan.RTPrescription.Gating))
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        //END RX CHECK METHODS

        public static string checkDoseCoverage(PlanningItem plan)
        {

            var CheckStructures = plan.StructureSet.Structures.Where(s => s.DicomType != "BOLUS" && s.DicomType != "MARKER" && s.DicomType != "SUPPORT");

            double structCoverage = 0;
            string fullycovered = "100%";

            foreach (var cont in CheckStructures)
            {
                if (!cont.IsEmpty)
                {

                    DVHData dvhtemp = plan.GetDVHCumulativeData(cont, DoseValuePresentation.Absolute, VolumePresentation.AbsoluteCm3, 0.1);
                    structCoverage = dvhtemp.Coverage;
                    if (structCoverage < 1)
                    {
                        fullycovered = cont.Id;
                        break;
                    }
                }

            }
            return fullycovered;

        }

        public static void maxDoseInPTV(PlanSetup plan, out bool samemax, out double globaldosemax, out string volname)
        {
            double global_DMax = 0;
            samemax = false;
            volname = "";
            if (plan.Dose != null)
            {



                global_DMax = plan.Dose.DoseMax3D.Dose;

                var PTVStructures = plan.StructureSet.Structures.Where(s => s.DicomType == "PTV")
                                                         .OrderBy(s => s.Id)
                                                         .Select(s => s);
                var CTVStructures = plan.StructureSet.Structures.Where(s => s.DicomType == "CTV")
                                                        .OrderBy(s => s.Id)
                                                        .Select(s => s);
                var GTVStructures = plan.StructureSet.Structures.Where(s => s.DicomType == "GTV")
                                                        .OrderBy(s => s.Id)
                                                        .Select(s => s);

                double PTV_DMax = 0.00;
                volname = "NONE";

                foreach (var ptv in PTVStructures)
                {
                    var zPTV_DMax = plan.GetDoseAtVolume(ptv, 0.000001, VolumePresentation.Relative, DoseValuePresentation.Relative);
                    if (zPTV_DMax.Dose >= PTV_DMax)
                    {
                        volname = ptv.Id;
                        PTV_DMax = zPTV_DMax.Dose;
                    }
                }
                foreach (var ctv in CTVStructures)
                {
                    var zPTV_DMax = plan.GetDoseAtVolume(ctv, 0.000001, VolumePresentation.Relative, DoseValuePresentation.Relative);
                    if (zPTV_DMax.Dose >= PTV_DMax)
                    {
                        volname = ctv.Id;
                        PTV_DMax = zPTV_DMax.Dose;
                    }
                }
                foreach (var gtv in GTVStructures)
                {
                    var zPTV_DMax = plan.GetDoseAtVolume(gtv, 0.000001, VolumePresentation.Relative, DoseValuePresentation.Relative);
                    if (zPTV_DMax.Dose >= PTV_DMax)
                    {
                        volname = gtv.Id;
                        PTV_DMax = zPTV_DMax.Dose;
                    }
                }

                PTV_DMax = Math.Round(PTV_DMax, 1);

                global_DMax = Math.Round(global_DMax, 1);

                samemax = (global_DMax == PTV_DMax) ? true : false;

                //this is more likely in 3D plans. Make distinction for that here? 
                if ( PTV_DMax == 0.00)
                {
                    volname = "NO TARGETS";
                }
                else if (global_DMax != PTV_DMax )
                {
                    volname = "OUTSIDE OF TARGETS";
                }
                else
                {
                    volname = "in " + volname;
                }
            }
            else
            {

            }


            globaldosemax = global_DMax;
        }
        public static string getDensityOverrides(PlanSetup plan)
        {
            double assignedHU = 99999;
            string output = "";
            foreach (Structure structurex in plan.StructureSet.Structures)
            {
                if (structurex != null)
                {
                    
                    if (structurex.GetAssignedHU(out assignedHU) && assignedHU != 99999)
                    {
                        string tempID = structurex.Id;
                        if (tempID != "CouchInterior" && tempID != "CouchSurface" && tempID != "LeftInnerRail" && tempID != "LeftOuterRail" && tempID != "RightInnerRail" && tempID.ToLower() != "artifact" && tempID != "RightOuterRail")
                        {
                            if (output != "") { output += "\n"; }
                            output += structurex.Id + " " + assignedHU.ToString();
                               
                            
                        }
                    }
                }


            }
            if (output == "") { output = "None"; }
            return output;
        }
        public static string checkArtifact(PlanSetup plan)
        {
            string output = "";
            double assignedHU = 99999;

            var artifactStructure = plan.StructureSet.Structures.FirstOrDefault(s => s.Id.ToLower().Contains("artifact"));

            if (artifactStructure != null)
            {
                artifactStructure.GetAssignedHU(out assignedHU);
                if (assignedHU != 0) { output += "WRONG HU"; }
                if (output != "") { output += "\n"; }
                output += artifactStructure.Id; // + " " + assignedHU.ToString();

            }
            return output;

            assignedHU = 99999;

            var vagMarkerStructure = plan.StructureSet.Structures.FirstOrDefault(s => s.StructureCode != null && s.Id == "VaginalMarker");

            if (vagMarkerStructure != null)
            {
                vagMarkerStructure.GetAssignedHU(out assignedHU);
                if (assignedHU != 0) { output += "WRONG HU"; }
                if (output != "") { output += "\n"; }
                output += vagMarkerStructure.Id + " " + assignedHU.ToString();

            }
            assignedHU = 99999;

            vagMarkerStructure = plan.StructureSet.Structures.FirstOrDefault(s => s.StructureCode != null && s.Id == "Vaginal marker");

            if (vagMarkerStructure != null)
            {
                vagMarkerStructure.GetAssignedHU(out assignedHU);
                if (assignedHU != 0) { output += "WRONG HU"; }
                if (output != "") { output += "\n"; }
                output += vagMarkerStructure.Id + " " + assignedHU.ToString();

            }
            assignedHU = 99999;

            foreach (var wireStructure in plan.StructureSet.Structures.Where(s => s.StructureCode != null && s.StructureCode.DisplayName == "Wire").ToList())
            {
                if (wireStructure != null)
                {
                    wireStructure.GetAssignedHU(out assignedHU);
                    if (assignedHU != -1000) { output += "WRONG HU"; }
                    if (output != "") { output += "\n"; }
                    output += wireStructure.Id + " " + assignedHU.ToString();

                }
            }

            assignedHU = 99999;

            foreach (var wireStructure in plan.StructureSet.Structures.Where(s =>  s.Id.ToLower().Contains("wire") && !s.Id.ToLower().Contains("iso")).ToList())
            {
                if (wireStructure != null)
                {
                    wireStructure.GetAssignedHU(out assignedHU);
                    if (assignedHU != -1000) { output += "WRONG HU"; }
                    if (output != "") { output += "\n"; }
                    output += wireStructure.Id + " " + assignedHU.ToString();

                }
            }

            if (output == "") output = "NONE";

            return output;
        }

      


        public static string findEmptyStructure(PlanSetup plan)
        {

            foreach (Structure structurex in plan.StructureSet.Structures)
            {
                if (structurex.IsEmpty)
                {
                    return "Empty Structure(s) found";
                }
            }
            return "None";
        }

        public static string findSupport(PlanSetup plan)
        {
            var supportStructures = plan.StructureSet.Structures.Where(s => s.StructureCode != null && s.StructureCode.Code.ToString() == "Support")
                                                     .OrderBy(s => s.Id)
                                                     .Select(s => s.Id);

            return string.Join(",\n", supportStructures);
        }

        public static string getPlanMode(PlanSetup plan)
        {
            HashSet<string> photons = new HashSet<string> { "6X", "10X", "10X-FFF", "6X-FFF", "18X" };
            HashSet<string> electrons = new HashSet<string> { "6E", "9E", "12E", "16E", "20E" };
            bool phot = false;
            bool elec = false;

            foreach (var beam in plan.Beams)
            {
                if (photons.Contains(beam.EnergyModeDisplayName))
                {
                    phot = true;
                }
                else if (electrons.Contains(beam.EnergyModeDisplayName))
                {
                    elec = true;
                }
            }


            if (phot && elec)
            {
                return "MIXED";
            }
            else if (phot)
            {
                return "PHOTON";
            }
            else if (elec)
            {
                return "ELECTRON";
            }
            else return "ERROR";

        }
        public static bool findDRR(PlanSetup plan)
        {
            bool alldrr = true;

            foreach (var beam in plan.Beams)
            {
                if (beam.ReferenceImage == null && beam.Id.ToLower() != "cbct")
                {
                    alldrr = false;
                }
            }
            return alldrr;

        }

        public static void checkrate(PlanSetup plan, out bool samerate, out string ratename)
        {

            samerate = true;
            int rate = 987654;

            foreach (var beam in plan.Beams)
            {
                if (!beam.IsSetupField)
                {
                    if (rate == 987654)
                    {
                        rate = beam.DoseRate;

                    }
                    else if (rate != beam.DoseRate)
                    {
                        samerate = false;
                        break;
                    }
                }
            }
            ratename = samerate ? rate.ToString() : "Different Dose Rates";

            if (rate < 600) { ratename = "LOW DOSE RATE"; samerate = false; }
        }
        public static void checktech(PlanSetup plan, out bool sametech, out string techname)
        {
            sametech = true;
            string tech = null;

            foreach (var beam in plan.Beams)
            {
                if (!beam.IsSetupField)
                {
                    if (tech == null)
                    {
                        tech = beam.Technique.Id;

                    }
                    else if (tech != beam.Technique.Id)
                    {
                        sametech = false;
                        break;
                    }
                }
            }

            techname = sametech ? tech : "Different Techniques";

        }

        public static void checkOneMachine(PlanSetup plan, out bool onemachine, out string machname)
        {
            onemachine = false;
            machname = "Error";

            var termsSet = new HashSet<string>();
            foreach (var beam in plan.Beams)
            {
                termsSet.Add(beam.TreatmentUnit.Id);
                if (termsSet.Count > 1)
                {
                    break;
                }
            }
            if (termsSet.Count == 1)
            {
                onemachine = true;
                machname = termsSet.First();
            }

        }

        public static void checkIso(PlanSetup plan, out bool sameiso, out string isoname)
        {

            sameiso = true;
            double x = 987.654;
            double y = 987.654;
            double z = 987.654;
            var ct = plan.StructureSet.Image;

            foreach (var beam in plan.Beams)
            {
                if (x == 987.654)
                {
                    x = (beam.IsocenterPosition.x - ct.UserOrigin.x) / 10;
                    y = (beam.IsocenterPosition.y - ct.UserOrigin.y) / 10;
                    z = (beam.IsocenterPosition.z - ct.UserOrigin.z) / 10;
                }
                else if (x != (beam.IsocenterPosition.x - ct.UserOrigin.x) / 10 ||
                        y != (beam.IsocenterPosition.y - ct.UserOrigin.y) / 10 ||
                        z != (beam.IsocenterPosition.z - ct.UserOrigin.z) / 10)
                {
                    sameiso = false;
                    break;
                }
            }
            if (sameiso)
            {
                isoname = x.ToString(("0.00")) + ", " + y.ToString("0.00") + ", " + z.ToString("0.00");
            }
            else
            {
                isoname = "Different Isos";
            }

            //isoname = sameiso ? $"{x.Value:F2}, {y.Value:F2}, {z.Value:F2}" : "Different Isos";

        }

        public static string findIsoStructure(PlanSetup plan)
        {
            string result = "FAIL";

            var isoStructures = plan.StructureSet.Structures.Where(s => s.Id.ToLower().Contains("iso") && s.DicomType != "MARKER").ToList();

            int isos = isoStructures.Count();

            //MessageBox.Show(isoStructures.Count().ToString());
            //iso structure and iso marker may vary slighty. we are going to rely on the structure. 

            if (isoStructures.Any())
            {
                foreach (var isoStruct in isoStructures)
                {
                    var isoPoint = isoStruct.CenterPoint;
                    var originPoint = plan.StructureSet.Image.UserOrigin;

                    if (Math.Abs(isoPoint.x - originPoint.x) < 0.02 &&
                        Math.Abs(isoPoint.y - originPoint.y) < 0.02 &&
                        Math.Abs(isoPoint.z - originPoint.z) < 0.02)
                    {
                        if (isos > 1) { result = isoStruct.Id; }
                        else { result = "MATCHES"; }
                        
                    }

                }

                    
            }

            return result;
        }

        public static (List<Point3D> , List<Tuple<Point3D, string>>) GetBodyAndGantryMeshes(PlanSetup plan)
        {
            var supportStructures = plan.StructureSet.Structures.Where(c => c.DicomType == "SUPPORT").ToList();
            var body = plan.StructureSet.Structures.Where(c => c.DicomType == "EXTERNAL").FirstOrDefault();



            var basePlate = plan.StructureSet.Structures.Where(c => c.Id.ToLower().Contains("baseplate") || c.Id.ToLower().Contains("base plate")).FirstOrDefault();

            Point3DCollection bodyMeshPoints = body.MeshGeometry.Positions;

            Point3DCollection BodyPlusCouch = new Point3DCollection();


            //check if couches/baseplate are present
            if (basePlate == null && supportStructures.Any() == false)
            {
                //couch structures or baseplate are missing
                BodyPlusCouch = bodyMeshPoints;

            }
            else if (supportStructures.Any())
            {

                List<Point3D> couchMeshes = new List<Point3D>();
                //add couch mesh to the body mesh
                foreach (var couchStruct in supportStructures)
                {
                    if (couchStruct.Id.ToLower().Contains("couchsurface") || couchStruct.Id.ToLower().Contains("rail"))
                    {
                        couchMeshes.AddRange(couchStruct.MeshGeometry.Positions);
                    }
                }
                var concattedLists = bodyMeshPoints.Concat(couchMeshes);
                Point3DCollection point3Ds = new Point3DCollection(concattedLists);
                BodyPlusCouch = point3Ds;

            }
            else
            {
                //if you cant find support structures just use the body
                BodyPlusCouch = bodyMeshPoints;
            }

            var GantryCirclePoints = AddCylinderToMesh(plan);

            List<Point3D> BodyPlusCouchList = BodyPlusCouch.ToList();

            return (BodyPlusCouchList, GantryCirclePoints);

            


        }

        public static void AssignBodyAndMeshGlobal(List<Tuple<Point3D, string>> cylinderMeshPositions, List<Point3D> bodyMeshPositions)
        {
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
            //assign value to global variable to use outside scope
            bodyMeshGlobalDisplay = every10thBody;
            List<Tuple<Point3D, string>> every10thMesh = every10thMesh1.Where((item, index) => (index + 1) % 1 == 0).Distinct().ToList();
            //assign value to global variable to use outside scope
            arcMeshGlobalDisplay = every10thMesh;






        }


        public static void CollisionCheck(PlanSetup plan)
        {
            //38 cm from iso = nono
            //couches are inserted in plan as support structures
            //If theres no couch it's prob a H/N with the baseplate included in the exteral

            //refer to where this global variables are defined above
            //they are assigned just before the output list is generated
           // ShortestDistance(bodyMeshGlobal, arcMeshGlobal, plan.Beams.Where(x => x.IsSetupField == false).First().IsocenterPosition, plan);


        }

        //converts dicom coords in mm to user coords in cm
        public static List<VVector> changeDICOMtoUserCoords(Tuple<Point3D, Point3D, double> tuple, PlanSetup plan)
        {
            var usercorrds = plan.StructureSet.Image.DicomToUser(new VVector(tuple.Item1.X, tuple.Item1.Y, tuple.Item1.Z), plan);
            var usercorrds1 = plan.StructureSet.Image.DicomToUser(new VVector(tuple.Item2.X, tuple.Item2.Y, tuple.Item2.Z), plan);


            var usercoordscm = new VVector(usercorrds.x / 10, usercorrds.y / 10, usercorrds.z / 10);
            var usercoordscm1 = new VVector(usercorrds1.x / 10, usercorrds1.y / 10, usercorrds1.z / 10);

            return new List<VVector>() { usercoordscm, usercoordscm1 };

        }

        public static Point3D changeUserToDICOMCoords(Point3D point, PlanSetup plan)
        {
            var usercorrds = plan.StructureSet.Image.DicomToUser(new VVector(point.X, point.Y, point.Z), plan);
            var usercoords1 = new Point3D(usercorrds.x, usercorrds.y, usercorrds.z);

            return usercoords1;

        }


        public static List<Tuple<Point3D, string>> AddCylinderToMesh(PlanSetup plan)
        {

            VVector isocenter = plan.Beams.First(c => c.IsSetupField == false).IsocenterPosition;
            var body = plan.StructureSet.Structures.Where(c => c.DicomType == "EXTERNAL").FirstOrDefault();



            List<Tuple<Point3D, string>> GantryCirclePoints;

            //make e elctron cone, or gantry arc, or gantry plane
            if (plan.Beams.FirstOrDefault(c => c.IsSetupField == false).EnergyModeDisplayName.ToLower().Contains("e"))
            {
                GantryCirclePoints = CreateConePlane(isocenter, plan.StructureSet.Image, plan);
            }
            else if (plan.Beams.FirstOrDefault(c => c.IsSetupField == false).ControlPoints.First().GantryAngle !=
                plan.Beams.FirstOrDefault(c => c.IsSetupField == false).ControlPoints.Last().GantryAngle)
            {
                GantryCirclePoints = CreateGantryArc(isocenter, 380, plan.StructureSet.Image, 10, plan);
            }
            else
            {
                //loop through each static beam and make the gantry //plane// now using a circle to model gantry head
                List<double> gantryAngleList = new List<double>();
                List<Tuple<Point3D, string>> pointList = new List<Tuple<Point3D, string>>();

                foreach (var beam in plan.Beams.Where(c => c.IsSetupField == false))
                {
                    if (gantryAngleList.Contains(beam.ControlPoints.FirstOrDefault().GantryAngle) == false)
                    {
                        pointList.AddRange(MakeGantryCapCircle(plan, isocenter));

                        //pointList.AddRange(CreateStaticPlane(isocenter, plan.StructureSet.Image, beam));
                        gantryAngleList.Add(beam.ControlPoints.FirstOrDefault().GantryAngle);
                    }
                }
                GantryCirclePoints = pointList;
            }

            return GantryCirclePoints;


        }

        /// <summary>
        /// Finds the slice number that your plan isocenter is on
        /// </summary>
        /// <param name="isocenter"></param>
        /// <param name="body"></param>
        /// <param name="plan"></param>
        /// <returns>Slice number from the most inferior slice that has the isocenter on it</returns>
        public static double? FindIsoSlice(VVector isocenter, Structure body, PlanSetup plan)
        {
            var sliceThickness = plan.StructureSet.Image.ZRes;

            List<VVector> contoursSorted = new List<VVector>();
            List<VVector[][]> vVectors = new List<VVector[][]>();
            for (int i = 0; i < 20; i++)
            {
                var contours = body.MeshGeometry.Positions;
                var contours1 = contours.Select(e => new VVector(e.X, e.Y, e.Z)).ToList();
                contoursSorted = contours1.OrderBy(c => c.z).ToList();

            }

            if (contoursSorted.Any())
            {
                double firstZVal = contoursSorted.First().z;
                double numberOfSlices = Math.Abs(isocenter.z - firstZVal) / sliceThickness;

                return numberOfSlices;

            }
            else
            {
                return null;
            }




        }

        /// <summary>
        /// creates a circle of points on the same slice as the Isocenter
        /// </summary>
        /// <param name="isocenter"> Isocenter Location</param>
        /// <param name="circleRadius"> size of the circle</param>
        /// <param name="image"> image so we can determine slice thickness</param>
        /// <param name="thetaDegrees"> sampling rate f points along the perimeter of the circle in degrees</param>
        /// <returns>The points on the circle</returns>
        public static List<Tuple<Point3D, string>> CreateGantryArc(VVector isocenter, double circleRadius, VMS.TPS.Common.Model.API.Image image, double thetaDegrees, PlanSetup plan)
        {
            List<Tuple<Point3D, double?, string>> oneSlicePointList = new List<Tuple<Point3D, double?, string>>();

            //radians along the circle where we will put points
            double smaplingRate = thetaDegrees * (Math.PI / 180);

            for (double i = 0; i < ((Math.PI * 2) / smaplingRate); i += smaplingRate)
            {
                //72 iterations of 0.87 radians makes a full circle (every 5 degrees)
                //i is theta
                double x;
                double y;

                //in mm
                if (plan.TreatmentOrientation == PatientOrientation.HeadFirstProne)
                {
                    y = circleRadius * Math.Sin(i);

                    x = -circleRadius * Math.Cos(i);


                }
                else if (plan.TreatmentOrientation == PatientOrientation.FeetFirstSupine)
                {
                    y = -circleRadius * Math.Sin(i);

                    x = -circleRadius * Math.Cos(i);
                }
                else if (plan.TreatmentOrientation == PatientOrientation.FeetFirstProne)
                {
                    y = circleRadius * Math.Sin(i);

                    x = circleRadius * Math.Cos(i);
                }
                else
                {
                    //normal head first supine orientation (y is negative)
                    y = -circleRadius * Math.Sin(i);

                    x = circleRadius * Math.Cos(i);
                }



                Point3D circleCoord = new Point3D(isocenter.x + x, isocenter.y + y, isocenter.z);


                //check if the current angle on the circle falls in one of the arc sectors
                foreach (var beam in plan.Beams.Where(c => c.IsSetupField == false))
                {
                    var arcsectors = GetArcSectors(beam);

                    double? couchKick = beam.ControlPoints.First().PatientSupportAngle;

                    Tuple<Point3D, double?, string> circleTup = new Tuple<Point3D, double?, string>(circleCoord, couchKick, beam.Id);

                    //MessageBox.Show("arcsector1 " + arcsectors.Item1 + " arcsector2 " + arcsectors.Item2);


                    if (arcsectors.Item3 == GantryDirection.CounterClockwise)
                    {
                        if (arcsectors.Item1 > arcsectors.Item2)
                        {
                            //passing through 0 polar

                            if (i >= arcsectors.Item1 && i <= 359 * (Math.PI / 180))
                            {
                                oneSlicePointList.Add(circleTup);
                            }
                            else if (i >= 0 && i <= arcsectors.Item2)
                            {
                                oneSlicePointList.Add(circleTup);
                            }
                        }
                        else
                        {

                            if (i >= arcsectors.Item1 && i <= arcsectors.Item2)
                            {
                                oneSlicePointList.Add(circleTup);

                            }
                        }

                    }
                    else
                    {
                        if (arcsectors.Item1 < arcsectors.Item2 && (arcsectors.Item2 >= 270 * (Math.PI / 180)))
                        {

                            //passing through 0 polar
                            if (i >= 0 && i <= arcsectors.Item1)
                            {
                                oneSlicePointList.Add(circleTup);

                            }
                            else if (i <= 359 * (Math.PI / 180) && i >= arcsectors.Item2)
                            {
                                oneSlicePointList.Add(circleTup);

                            }
                        }
                        else
                        {

                            if (i <= arcsectors.Item1 && i >= arcsectors.Item2)
                            {

                                oneSlicePointList.Add(circleTup);
                            }
                        }
                    }
                }



            }





            double sliceThickness = image.ZRes;
            List<Tuple<Point3D, string>> AllPoints = new List<Tuple<Point3D, string>>();


            List<Point3D> pointsToBeRotated = new List<Point3D>();
            //extend the circle to a cylinder, only go ~38cm north and south (about the size of the gantry head)
            //iterate every 5mm
            for (int i = 0; i <= 388; i += 4)
            {
                foreach (var tup in oneSlicePointList)
                {
                    Point3D point = tup.Item1;


                    if (tup.Item2 == 0)
                    {

                        double PosZ = point.Z + i;
                        double NegZ = point.Z - i;

                        Point3D PosPoint = new Point3D(point.X, point.Y, PosZ);
                        Point3D NegPoint = new Point3D(point.X, point.Y, NegZ);

                        Tuple<Point3D, string> PosPointAsTup = new Tuple<Point3D, string>(PosPoint, tup.Item3);
                        Tuple<Point3D, string> NegPointAsTup = new Tuple<Point3D, string>(NegPoint, tup.Item3);

                        AllPoints.Add(PosPointAsTup);
                        AllPoints.Add(NegPointAsTup);


                    }
                    else
                    {
                        double PosZ = point.Z + i;
                        double NegZ = point.Z - i;

                        Point3D PosPoint = new Point3D(point.X, point.Y, PosZ);
                        Point3D NegPoint = new Point3D(point.X, point.Y, NegZ);



                        Point3D rotatedPoint = RotateAroundYAxis(PosPoint, isocenter, (double)tup.Item2);
                        Point3D rotatedPoint1 = RotateAroundYAxis(NegPoint, isocenter, (double)tup.Item2);

                        Tuple<Point3D, string> PosPointAsTup = new Tuple<Point3D, string>(rotatedPoint, tup.Item3);
                        Tuple<Point3D, string> NegPointAsTup = new Tuple<Point3D, string>(rotatedPoint1, tup.Item3);



                        AllPoints.Add(PosPointAsTup);
                        AllPoints.Add(NegPointAsTup);
                    }


                }

            }



            var gantryCapList = MakeGantryCapCircle(plan, isocenter);


            //AllPoints.Clear();


            AllPoints.AddRange(gantryCapList);


            return AllPoints;

        }


        public static List<Tuple<Point3D, string>> MakeGantryCapCircle(PlanSetup plan, VVector isocenter)
        {

            List<Beam> planBeams = plan.Beams.Where(c => c.IsSetupField == false).ToList();

            double x;
            double y;


            Vector3 circleNorm = new Vector3(0, 0, 1);


            Vector3 isoVect3 = new Vector3((float)isocenter.x, (float)isocenter.y, (float)isocenter.z);

            List<Tuple<Point3D, string>> resultingPoints = new List<Tuple<Point3D, string>>();

            foreach (var beam in planBeams)
            {
                List<Vector3> circlePoints = new List<Vector3>();

                //points on the circumference
                for (double i = 0; i < Math.PI * 2; i += (Math.PI / 75))
                {
                    //circle size
                    for (double k = 0; k <= 380; k += 63.3)
                    {
                        y = k * Math.Sin(i);

                        x = -k * Math.Cos(i);

                        circlePoints.Add(new Vector3((float)x, (float)y, 0));


                    }


                }

                //doesnt work for couch kicks?
                VVector startingSource = beam.GetSourceLocation(beam.ControlPoints.First().GantryAngle);
                VVector endingSource = beam.GetSourceLocation(beam.ControlPoints.Last().GantryAngle); ;




                Vector3 isoToStart = new Vector3((float)(startingSource.x - isocenter.x), (float)(startingSource.y - isocenter.y), (float)(startingSource.z - isocenter.z));
                Vector3 isoToEnd = new Vector3((float)(endingSource.x - isocenter.x), (float)(endingSource.y - isocenter.y), (float)(endingSource.z - isocenter.z));

                Vector3 rotationAxisStart = Vector3.Cross(circleNorm, isoToStart);
                Vector3 rotationAxisEnd = Vector3.Cross(circleNorm, isoToEnd);


                float rotationAngle1 = (float)Math.Acos(Vector3.Dot(circleNorm, Vector3.Normalize(isoToStart)));
                float rotationAngle2 = (float)Math.Acos(Vector3.Dot(circleNorm, Vector3.Normalize(isoToEnd)));


                rotationAxisStart = Vector3.Normalize(rotationAxisStart);
                rotationAxisEnd = Vector3.Normalize(rotationAxisEnd);

                Matrix4x4 rotationMatrix1 = Matrix4x4.CreateFromAxisAngle(rotationAxisStart, rotationAngle1);
                Matrix4x4 rotationMatrix2 = Matrix4x4.CreateFromAxisAngle(rotationAxisEnd, rotationAngle2);

                foreach (var point in circlePoints)
                {

                    var pointStart = Vector3.Transform(point, rotationMatrix1);
                    var pointEnd = Vector3.Transform(point, rotationMatrix2);


                    var pointStartRes = pointStart + isoVect3 + Vector3.Normalize(isoToStart) * 380;
                    var pointEndRes = pointEnd + isoVect3 + Vector3.Normalize(isoToEnd) * 380;

                    Point3D startPointResult = new Point3D(pointStartRes.X, pointStartRes.Y, pointStartRes.Z);
                    Point3D endPointResult = new Point3D(pointEndRes.X, pointEndRes.Y, pointEndRes.Z);

                    Tuple<Point3D, string> startAsTuple = new Tuple<Point3D, string>(startPointResult, beam.Id);
                    Tuple<Point3D, string> endAsTuple = new Tuple<Point3D, string>(endPointResult, beam.Id);


                    resultingPoints.Add(startAsTuple);
                    resultingPoints.Add(endAsTuple);


                }


            }


            return resultingPoints;

        }


        public static Point3D RotateAroundYAxis(Point3D point3D, VVector isocenter, double couchKick)
        {
            Point3D isoAsPoint = new Point3D(isocenter.x, isocenter.y, isocenter.z);

            Point3D OriginPoints = new Point3D(point3D.X - isoAsPoint.X, point3D.Y - isoAsPoint.Y, point3D.Z - isoAsPoint.Z);

            double couchKickRad = couchKick * (Math.PI / 180);

            float x = (float)(OriginPoints.X * Math.Cos(couchKickRad) + OriginPoints.Z * Math.Sin(couchKickRad));
            float z = (float)(OriginPoints.Z * Math.Cos(couchKickRad) - OriginPoints.X * Math.Sin(couchKickRad));

            return new Point3D(x + isocenter.x, OriginPoints.Y + isocenter.y, z + isocenter.z);


        }

        public static VVector RotateAroundYAxis1(Point3D point3D, VVector isocenter, double couchKick, Beam beam)
        {
            Point3D isoAsPoint = new Point3D(isocenter.x, isocenter.y, isocenter.z);

            Point3D OriginPoints = new Point3D(point3D.X - isoAsPoint.X, point3D.Y - isoAsPoint.Y, point3D.Z - isoAsPoint.Z);

            double couchKickRad = couchKick * (Math.PI / 180);

            float x = (float)(OriginPoints.X * Math.Cos(couchKickRad) + OriginPoints.Z * Math.Sin(couchKickRad));
            float z = (float)(OriginPoints.Z * Math.Cos(couchKickRad) - OriginPoints.X * Math.Sin(couchKickRad));

            return new VVector(x + isocenter.x, OriginPoints.Y + isocenter.y, z + isocenter.z);


        }

        public static List<Tuple<Point3D, string>> CreateConePlane(VVector isocenter, VMS.TPS.Common.Model.API.Image image, PlanSetup planSetup)
        {
            //find the iso position
            //find the SSD? (electron plans are fixed SSD setups)
            //find the direction of the beam (source position to isocenter position)
            //make a plane perpendicular to the beam ~3.5cm upstream from the isocenter
            //the 3.5cm is the clearance from iso to the bottom of the cone
            //the size of the plane will correspond to the size of the e cone


            List<Point3D> AllPoints = new List<Point3D>();
            List<Tuple<Point3D, string>> AllPointsTranslated = new List<Tuple<Point3D, string>>();


            var firstBeam = planSetup.Beams.FirstOrDefault(c => c.IsSetupField == false);
            var gantryAngle = planSetup.Beams.FirstOrDefault(c => c.IsSetupField == false).ControlPoints.First().GantryAngle;

            VVector sourceLocation = firstBeam.GetSourceLocation(gantryAngle);
            VVector sourceToIso = isocenter - sourceLocation;

            sourceToIso.ScaleToUnitLength();

            //bottom of cone is ~ 3.5cm back from the iso
            var bottomOfCone = isocenter - sourceToIso * 35;

            //define a plane using a normal vector
            //the normal being the source to iso vector(direction of the beam )
            Plane perpendicularPlane = new Plane(new Vector3((float)sourceToIso.x, (float)sourceToIso.y, (float)sourceToIso.z), 0);



            Matrix4x4 rotMatrix = Matrix4x4.CreateFromAxisAngle(new Vector3(0, 0, 1), (float)(gantryAngle * (Math.PI / 180)));

            //cm
            int coneSize = int.Parse(firstBeam.Applicator.Id.Substring(1));

            //make dictionary of additional margin that needs to be added to cone size
            //these are the physical measurements of the cone
            Dictionary<double, double> ApplicatorMarginDict = new Dictionary<double, double>();
            ApplicatorMarginDict.Add(6, 15);
            ApplicatorMarginDict.Add(10, 18.8);
            ApplicatorMarginDict.Add(15, 23.7);
            ApplicatorMarginDict.Add(20, 28.5);
            ApplicatorMarginDict.Add(25, 33.7);


            //the additional margin value in cm
            double MarginValue = ApplicatorMarginDict.FirstOrDefault(c => c.Key == coneSize).Value;

            //Create x-z plane of points
            //add margin value on both sides to account for actual applicator size
            for (double x = -(MarginValue * 10) / 2; x <= (MarginValue * 10) / 2; x += 10)
            {
                for (double z = -(MarginValue * 10) / 2; z <= (MarginValue * 10) / 2; z += 10)
                {
                    Vector3 point = new Vector3((float)x, 0, (float)z);
                    Vector3 rotatedVector = Vector3.Transform(point, rotMatrix);


                    VVector point3D = new VVector(rotatedVector.X, rotatedVector.Y, rotatedVector.Z);
                    VVector addedPoint = bottomOfCone + point3D;
                    Point3D finalPoint = new Point3D(addedPoint.x, addedPoint.y, addedPoint.z);

                    Tuple<Point3D, string> finalAsTup = new Tuple<Point3D, string>(finalPoint, firstBeam.Id);

                    //newPointList.Add(finalPoint);
                    AllPointsTranslated.Add(finalAsTup);

                }
            }


            return AllPointsTranslated;


        }

        public static List<Tuple<Point3D, string>> CreateStaticPlane(VVector isocenter, VMS.TPS.Common.Model.API.Image image, Beam beam)
        {

            List<Point3D> AllPoints = new List<Point3D>();
            List<Tuple<Point3D, string>> AllPointsTranslated = new List<Tuple<Point3D, string>>();


            //var firstBeam = planSetup.Beams.FirstOrDefault(c => c.IsSetupField == false);
            var gantryAngle = beam.ControlPoints.First().GantryAngle;

            VVector sourceLocation = beam.GetSourceLocation(gantryAngle);
            VVector sourceToIso = isocenter - sourceLocation;

            sourceToIso.ScaleToUnitLength();

            //gantry head ~ 38cm from iso
            var gantryPlane = isocenter - sourceToIso * 380;

            //define a plane using a normal vector
            //the normal being the source to iso vector(direction of the beam )
            Plane perpendicularPlane = new Plane(new Vector3((float)sourceToIso.x, (float)sourceToIso.y, (float)sourceToIso.z), 0);


            //make the rotation matrix
            Matrix4x4 rotMatrix = Matrix4x4.CreateFromAxisAngle(new Vector3(0, 0, 1), (float)(gantryAngle * (Math.PI / 180)));

            //mm
            double coneSize = 387.5;


            //Create x-z plane of points

            for (double x = -coneSize; x <= coneSize; x += 10)
            {
                for (double z = -coneSize; z <= coneSize; z += 10)
                {
                    Vector3 point = new Vector3((float)x, 0, (float)z);
                    Vector3 rotatedVector = Vector3.Transform(point, rotMatrix);


                    VVector point3D = new VVector(rotatedVector.X, rotatedVector.Y, rotatedVector.Z);
                    VVector addedPoint = gantryPlane + point3D;
                    Point3D finalPoint = new Point3D(addedPoint.x, addedPoint.y, addedPoint.z);

                    Tuple<Point3D, string> finalAsTup = new Tuple<Point3D, string>(finalPoint, beam.Id);


                    //newPointList.Add(finalPoint);
                    AllPointsTranslated.Add(finalAsTup);

                }
            }


            return AllPointsTranslated;


        }

        //find the starting and stop gantry angles
        //not finished yet
        /// <summary>
        /// Finds the start and end point of the arc in polar coords in radians
        /// </summary>
        /// <param name="beam"></param>
        /// <returns>a tuple containing the arc start, end, and gantry direction</returns>
        public static Tuple<double, double, GantryDirection> GetArcSectors(Beam beam)
        {

            var FirstGantryAngle = beam.ControlPoints.First().GantryAngle;
            var LastGantryAngle = beam.ControlPoints.Last().GantryAngle;


            var polarFirst1 = ConvertGantryAngleToPolar(FirstGantryAngle);
            var polarLast1 = ConvertGantryAngleToPolar(LastGantryAngle);

            var polarFirst = polarFirst1 * (Math.PI / 180);
            var polarLast = polarLast1 * (Math.PI / 180);

            Tuple<double, double, GantryDirection> tuple = new Tuple<double, double, GantryDirection>(polarFirst, polarLast, beam.GantryDirection);


            return tuple;
        }

        /// <summary>
        /// converts varian IEC gantry angles in degrees to polar angles in degrees
        /// </summary>
        /// <param name="gantryAngle"></param>
        /// <returns>the polar angle in degrees</returns>
        public static double ConvertGantryAngleToPolar(double gantryAngle)
        {
            double polarAngle;
            if (gantryAngle <= 90)
            {

                polarAngle = Math.Abs(90 - gantryAngle);
            }
            else
            {
                polarAngle = 450 - gantryAngle;
            }

            return polarAngle;
        }


        

        /// <summary>
        /// Checks distance between points on the body and points on the mesh.
        /// </summary>
        /// <param name="bodyMeshPositions"></param>
        /// <param name="cylinderMeshPositions"></param>
        /// <param name="isocenter"></param>
        /// <returns>The first point under 2cm distance or the shortest distance it finds after comparing all the points.</returns>
        //public static void ShortestDistance(List<Point3D> bodyMeshPositions, List<Tuple<Point3D, string>> cylinderMeshPositions, VVector isocenter, PlanSetup plan)
        //{

        //    double shortestDistance = 2000000;


        //    //only use body points which are in the neighborhood of the iso in the z direction
        //    var zList = cylinderMeshPositions.Select(c=> c.Item1).ToList().Select(c => c.Z).ToList();
        //    zList.Sort();
        //    var zMin = zList.First();
        //    var zMax = zList.Last();

        //    List<Point3D> nearbyBodyPositions = new List<Point3D>();


        //    //find the neaby body points that you want to measure distance from the mesh
        //    //use some extra body points if you are comparing e cone (easier to see body this way)
        //    if (Math.Abs(zMax-zMin) <= 300)
        //    {
        //        nearbyBodyPositions = bodyMeshPositions.Where(c => c.Z <= zMax + 100 && c.Z >= zMin - 100).ToList();
               
        //    }
        //    else
        //    {
        //        nearbyBodyPositions = bodyMeshPositions.Where(c => c.Z <= zMax && c.Z >= zMin).ToList();

        //        if (nearbyBodyPositions.Any() == false)
        //        {
        //            nearbyBodyPositions = bodyMeshPositions;
        //        }
                

        //    }

        //    //arrange mesh points into array of arrays of doubles
        //    var OnlyMeshPoints = cylinderMeshPositions.Select(c=> c.Item1).ToList();


        //    //stopwatch for optimizing time
        //    //Stopwatch stopWatch = new Stopwatch();
        //    //stopWatch.Start();


        //    //create the kdtree
        //    KdTree.Math.DoubleMath doubleMath = new KdTree.Math.DoubleMath();
        //    var tree = new KdTree.KdTree<double, int>(3, doubleMath);

        //    //add the points from the gantry mesh to the tree
        //    foreach (var point in OnlyMeshPoints)
        //    {
        //        tree.Add(new double[] { point.X, point.Y, point.Z }, 1);
        //    }

        //    var TotalTreePoints = nearbyBodyPositions.Count;
        //    //evaluate the body points vs the closest mesh point using the tree
        //    var currentpoint = 0;
        //    List<double> distList = new List<double>();
        //    foreach (var point in nearbyBodyPositions)
        //    {



        //        var nearestNeighbor = tree.GetNearestNeighbours(new double[] { point.X, point.Y, point.Z }, 1);


        //        double[] nearPoint = nearestNeighbor[0].Point;

        //        double distance = (Math.Sqrt(((nearPoint[0] - point.X)* (nearPoint[0] - point.X)) + ((nearPoint[1] - point.Y)*(nearPoint[1] - point.Y))
        //                                + ((nearPoint[2] - point.Z)* (nearPoint[2] - point.Z)))) / 10;
        //        if (distance < shortestDistance)
        //        {
        //            shortestDistance = distance;
        //            distList.Add(distance);

        //        }
                
        //        currentpoint++;
        //    }

        //    //stop the timer and display time to make and test points
        //    //stopWatch.Stop();
        //    //MessageBox.Show(stopWatch.Elapsed.TotalSeconds.ToString() + "   total seconds elapsed");


        //    //get the smallest distance between the mesh and body and assign it to your global variable to display in UI
        //    distList.Sort();
        //    shortestDistanceGlobal = distList.FirstOrDefault();



        //    //define everynthmesh so you can shuffle the points with rng
        //    List<Tuple<Point3D, string>> every10thMesh1 = cylinderMeshPositions;
        //    //shuffle the points in the list so you can use every 3rd without introducing aliasing
        //    Random rng = new Random();
        //    int n = every10thMesh1.Count;
        //    while (n > 1)
        //    {
        //        n--;
        //        int k = rng.Next(n + 1);
        //        var value = every10thMesh1[k];
        //        every10thMesh1[k] = every10thMesh1[n];
        //        every10thMesh1[n] = value;

        //    }


            

        //    //get every nth point from the body mesh
        //    //saves time by not plotting every point
        //    List<Point3D> every10thBody = nearbyBodyPositions.Where((item, index) => (index + 1) % 25 == 0).Distinct().ToList();
        //    //assign value to global variable to use outside scope when making the collision check model
        //    bodyMeshGlobalDisplay = every10thBody;
        //    List<Tuple<Point3D, string>> every10thMesh = every10thMesh1.Where((item, index) => (index + 1) % 1 == 0).Distinct().ToList();
        //    //assign value to global variable to use outside scope
        //    arcMeshGlobalDisplay = every10thMesh;


        //}

        public static void CreateVisualModel(List<Tuple<Point3D, string>>  every10thMesh, VVector isocenter, List<Point3D> every10thBody, /*List<Point3D> corrPointsNoDuplicates*/ PlanSetup plan)
        {
            //instantiate the model3D class we will add to the viewPort of Helix3D
            ModelVisual3D modelVisual3D = new ModelVisual3D();



            PlotArcsInCollisionModel(every10thMesh, modelVisual3D, isocenter, every10thBody /* corrPointsNoDuplicates*/);


            //orient the visual model correctly depending on treatment orientation
            //orient view cube correctly depending on treatment orientation
            viewPortGlobal.ViewCubeFrontText = "L";
            viewPortGlobal.ViewCubeBackText = "R";
            viewPortGlobal.ViewCubeBottomText = "I";
            viewPortGlobal.ViewCubeTopText = "S";
            viewPortGlobal.ViewCubeLeftText = "P";
            viewPortGlobal.ViewCubeRightText = "A";

            Vector3D Updirection;
            Vector3D Lookdirection;
            Point3D position;

            if (plan.TreatmentOrientation == PatientOrientation.HeadFirstProne)
            {
                Updirection = new Vector3D(0, 0.853, 0);
                position = new Point3D(2.9, -2148, -3758);
                Lookdirection = new Vector3D(0, 2296, 3747);

                viewPortGlobal.ViewCubeLeftText = "P";
                viewPortGlobal.ViewCubeRightText = "A";


            }
            else if (plan.TreatmentOrientation == PatientOrientation.FeetFirstSupine)
            {
                Updirection = new Vector3D(0, -0.853, 0);
                Lookdirection = new Vector3D(-17, -988, -4281);
                position = new Point3D(-37, 1237, 4243);

                viewPortGlobal.ViewCubeFrontText = "L";
                viewPortGlobal.ViewCubeBackText = "R";


            }
            else if (plan.TreatmentOrientation == PatientOrientation.FeetFirstProne)
            {
                Updirection = new Vector3D(0, 0.853, 0);
                Lookdirection = new Vector3D(-17, -988, -4281);
                position = new Point3D(-37, 1237, 4243);

                viewPortGlobal.ViewCubeFrontText = "L";
                viewPortGlobal.ViewCubeBackText = "R";
                viewPortGlobal.ViewCubeLeftText = "P";
                viewPortGlobal.ViewCubeRightText = "A";


            }
            else
            {
                //normal head first supine orientation (y is negative)
                Updirection = new Vector3D(0, -0.853, 0);
                position = new Point3D(2.9, -2148, -3758);
                Lookdirection = new Vector3D(0, 2296, 3747);
            }





            //set the default camera view
            PerspectiveCamera camera = new PerspectiveCamera()
            {
                Position = position,
                LookDirection = Lookdirection,
                UpDirection = Updirection,
                FieldOfView = 10,




            };

            //change the zoom sensitivity
            viewPortGlobal.ZoomSensitivity = 0.5;

            //viewPort.ShowCameraInfo = true;

            viewPortGlobal.Camera = camera;
        }

        public static double ComputeDistance(Point3D point1, Point3D point2)
        {
            //take the sqrt and math.pow out?
            double distance = (Math.Sqrt((Math.Pow((point2.X - point1.X), 2)) + (Math.Pow((point2.Y - point1.Y), 2))
                                        + (Math.Pow((point2.Z - point1.Z), 2)))) / 10;

            return distance;
        }


        public static void PlotArcsInCollisionModel(List<Tuple<Point3D, string>> every10thMesh, ModelVisual3D modelVisual3D, VVector isocenter, List<Point3D> every10thBody /*List<Point3D> corrPointsNoDuplicates*/)
        {
            
            List<string> checkedBeams = new List<string>();
            //only add the points to the cylinder mesh if checkboxes are selected in UI
            foreach (var item in checkBoxContainerGlobal.Items)
            {
                CheckBox checkBox = (CheckBox)item;

                if (checkBox.IsChecked == true)
                {
                    checkedBeams.Add((string)checkBox.Content);
                }

            }
         

            //do the same for the cylinder mesh
            PointsVisual3D pointsVisual3Dcyl = new PointsVisual3D()
            {
                Color = Colors.Green,
                Size = 2,
                Points = new Point3DCollection(every10thMesh.Where(c => checkedBeams.Contains(c.Item2)).Select(c => c.Item1))
            };

            //clear plotted data and then plot the cylinder mesh
            modelVisual3D.Children.Clear();
            modelVisual3D.Children.Add(pointsVisual3Dcyl);
      

            plotCounter++;
            viewPortGlobal.Children.Clear();
            viewPortGlobal.Children.Add(modelVisual3D);


            //add the body points to a model
            PointsVisual3D pointsVisual3D = new PointsVisual3D()
            {
                Color = Colors.Blue,
                Size = 2,
                Points = new Point3DCollection(every10thBody)


            };


            globalModelVisual3D = modelVisual3D;
            //adds the body points to 3D view
            modelVisual3D.Children.Add(pointsVisual3D);

            //for plotting body cutoff points on CT FOV
            //PointsVisual3D pointsVisual3DCutoff = new PointsVisual3D()
            //{
            //    Color = Colors.Coral,
            //    Size = 4,
            //    Points = new Point3DCollection(corrPointsNoDuplicates)

            //};
            ////ads iso to view as a yellow block
            //modelVisual3D.Children.Add(pointsVisual3DCutoff);





            //for showing the isocenter
            var iso1 = new Point3D(isocenter.x, isocenter.y, isocenter.z);
            var isoList1 = new List<Point3D>() { iso1 };
            PointsVisual3D pointsVisual3Diso = new PointsVisual3D()
            {
                Color = Colors.Yellow,
                Size = 10,
                Points = new Point3DCollection(isoList1)

            };
            //ads iso to view as a yellow block
            modelVisual3D.Children.Add(pointsVisual3Diso);




        }




        //check if targets have slices skipped contours
        //may not work because individual nodal volumes
        //not currently working
        public static List<string> checkForMissingSlices(PlanSetup plan)
        {
            var planStructures = plan.StructureSet.Structures;
            var targetStructures = plan.StructureSet.Structures.Where(c => c.DicomType.ToLower().Contains("ctv") ||
            c.DicomType.ToLower().Contains("gtv")).ToList();


            //List<Tuple<string, int>> targetsMissingSlices = new List<Tuple<string, int>>();
            List<string> targetsMissingSlices = new List<string>();

            if (targetStructures.Any())
            {

                foreach (var structure in targetStructures)
                {
                    int counter = 0;
                    List<int> MissingSlices = new List<int>();



                    if (structure.HasSegment == true)
                    {
                        for (int i = 0; i < 400; i += 1)
                        {

                            var vector = structure.GetContoursOnImagePlane(i);


                            if (vector.Any() == true)
                            {
                                counter++;
                            }
                            else if (vector.Any() == false & counter != 0)
                            {
                                MissingSlices.Add(i);
                                MessageBox.Show("in");

                            }

                        }

                    }

                    if (MissingSlices.Any())
                    {
                        //Tuple<string, int> tuple = new Tuple<string, int>(structure.Id, MissingSlices.First());
                        //targetsMissingSlices.Add(tuple);

                        targetsMissingSlices.Add(structure.Id);

                    }




                }

                return targetsMissingSlices;

            }
            else
            {
                return null;
            }

            
        }

       


        //check if target contours are inside body (volume type PTV/CTV/GTV?)
        //not currently working yet
        public static List<Structure> checkTargetInsideBody(PlanSetup plan)
        {



            var targetStructures = plan.StructureSet.Structures.Where(c=> c.DicomType.ToLower().Contains("ctv") ||
            c.DicomType.ToLower().Contains("gtv") || c.DicomType.ToLower().Contains("ptv")).ToList();

            MessageBox.Show(targetStructures.First().Id);

            var body = plan.StructureSet.Structures.Where(c => c.DicomType == "EXTERNAL").FirstOrDefault();

            MessageBox.Show(plan.StructureSet.Structures.Where(c => c.Id.ToLower().Contains("external")).First().DicomType);

            List<Structure> targetOutsideBody = new List<Structure>();


            Structure brainStem = plan.StructureSet.Structures.FirstOrDefault(c => c.Id == "Brainstem");

           



            if (targetStructures.Any())
            {
                MessageBox.Show("in loop");
                foreach (var target in targetStructures)
                {

                    //body.SegmentVolume = body.Or(target.SegmentVolume);


                    MessageBox.Show(target.Id);
                    //var subtraction = target.Sub(body);
                    //var AndStruct = subtraction.And(body);

                    var mesh = target.MeshGeometry;
                    var targetBounds = mesh.Bounds;

                    
                    if (true)
                    {
                        targetOutsideBody.Add(target);
                        MessageBox.Show(target.Id + " outside body");
                    }
                }


            }

            return targetOutsideBody;
            

        }


        //method for coloring rows
        private void ReportDataGrid_LoadingRow(object sender, DataGridRowEventArgs e)
        {
            var rowString = e.Row.Item.ToString();

            //MessageBox.Show(String.Format("{0}{1}{2}", rowTuple.Item1, rowTuple.Item2, rowTuple.Item3));

            SolidColorBrush brush = System.Windows.Media.Brushes.LightGoldenrodYellow;
           // MessageBox.Show("HERE" + rowString.ToLower());
            if (rowString.ToLower().Contains("false"))
            {
                // MessageBox.Show("HERE" + rowString.ToLower());
                brush = System.Windows.Media.Brushes.Pink;
            }
            else if (rowString.ToLower().Contains("true")){
                brush = System.Windows.Media.Brushes.LightGreen;
            }
           
            else if (rowString.ToLower().Contains("displayonly"))
            {
                brush = System.Windows.Media.Brushes.White;
            }

            e.Row.Background = brush;

        }
        private void ReportDataGridRx_LoadingRow(object sender, DataGridRowEventArgs e)
        {
            var rowString = e.Row.Item.ToString();

            //MessageBox.Show(String.Format("{0}{1}{2}", rowTuple.Item1, rowTuple.Item2, rowTuple.Item3));

            SolidColorBrush brush = System.Windows.Media.Brushes.LightGoldenrodYellow;
            if (rowString.ToLower().Contains("false"))
            {
                brush = System.Windows.Media.Brushes.Pink;
            }
            else if (rowString.ToLower().Contains("true"))
            {
                brush = System.Windows.Media.Brushes.LightGreen;
            }
            else if (rowString.ToLower().Contains("displayonly"))
            {
                brush = System.Windows.Media.Brushes.White;
            }

            e.Row.Background = brush;

        }
        

        //this isnt working, ignore
        private void ReportDataGrid_Loaded(object sender, RoutedEventArgs e)
        {
           

            //headers if you want them
            ReportDataGrid.HeadersVisibility = DataGridHeadersVisibility.Column;
            ReportDataGrid.Columns[2].Header = "Plan";
            ReportDataGrid.Columns[0].Header = "TxCheck";
            ReportDataGrid.Columns[1].Header = "Expected";
            ReportDataGrid.Columns[3].Header = "Result";


        }
        private void ReportDataGridRx_Loaded(object sender, RoutedEventArgs e)
        {


            //headers if you want them
            ReportDataGrid_Rx.HeadersVisibility = DataGridHeadersVisibility.Column;
            ReportDataGrid_Rx.Columns[2].Header = "Plan";
            ReportDataGrid_Rx.Columns[0].Header = "RxCheck";
            ReportDataGrid_Rx.Columns[1].Header = "RX";
            ReportDataGrid_Rx.Columns[3].Header = "Result";


        }

       

        private void NewCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            try
            {
                PlotArcsInCollisionModel(arcMeshGlobalDisplay, globalModelVisual3D, context1.PlanSetup.Beams.FirstOrDefault(c=> c.IsSetupField==false).IsocenterPosition, bodyMeshGlobalDisplay /* cutoffMeshGlobal*/);

            }
            catch (Exception m)
            {
                MessageBox.Show(m.Message);
                
            }
        }



        //button action which will trigger the collision check
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            //instantiate your ProgressBar 
            ProgressBar progressBar = new ProgressBar();


           

            if (progressBar.Execute())
            {

            }


            //refresh the grid to display the result
            ReportDataGrid.Items.Refresh();


        }


        //private void CheckBox_MouseUp(object sender, MouseButtonEventArgs e)
        //{
        //    CollisionCheck(context1.PlanSetup);

        //}

        //private static void FillPlanComboBox(ScriptContext scriptContext, ComboBox comboBox)
        //{
        //    List <PlanSetup> scopePlans = scriptContext.PlansInScope.ToList();

        //    foreach (var plan in scopePlans)
        //    {
        //        if (plan.PlanIntent.ToLower() != "verification")
        //        {
        //            comboBox.Items.Add(plan.Id);
        //        }

        //    }

        //    foreach (var item in comboBox.Items)
        //    {
        //        if ((string)item == scriptContext.PlanSetup.Id)
        //        {
        //            comboBox.SelectedItem = item;
        //        }
        //    }


        //}

        //private void PlanComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        //{

        //    Main(context1, PlanComboBox, viewPort, OutputList, OutputListRX, HorizontalStackPanel, ReportDataGrid, ReportDataGrid_Rx);
        //}
    }
}
