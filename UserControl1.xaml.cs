using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

namespace PlanChecks
{
    /// <summary>
    /// Interaction logic for UserControl1.xaml
    /// </summary>
    public partial class UserControl1 : UserControl
    {
        public List<Tuple<string, string, string, bool?>> OutputList = new List<Tuple<string, string, string, bool?>>();
        public List<Tuple<string, string, string, bool?>> OutputListRX = new List<Tuple<string, string, string, bool?>>();

        ScriptContext context1;
       
        public static HelixToolkit.Wpf.HelixViewport3D viewPort;

        public UserControl1(ScriptContext context, Window window1)
        {
            InitializeComponent();

            context1 = context;
            viewPort = viewport;
            ComboBox PlanComboBox = this.PlanComboBox;
            FillPlanComboBox(context, PlanComboBox);
            

        }

        public static void Main(ScriptContext context , ComboBox PlanComboBox, HelixViewport3D viewPort, List<Tuple<string, string, string, bool?>> OutputList, 
            List<Tuple<string, string, string, bool?>> OutputListRX, StackPanel HorizontalStackPanel, DataGrid ReportDataGrid, 
            DataGrid ReportDataGrid_Rx)
        {
            //code behind goes here
            UserControl userControl = new UserControl();

            OutputList.Clear();
            OutputListRX.Clear();
            


            Patient mypatient = context.Patient;
            Course course = context.Course;

            
            //get plan from ui comboBox
            PlanSetup plan = context.PlansInScope.Where(c => c.Id == (string)PlanComboBox.SelectedItem).FirstOrDefault();
            //PlanSetup plan = context.PlanSetup;



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

            expectedRes = techname.StartsWith("SRS") ? 0.125 : 0.250;

            if (plan.Dose != null)
            {
                actualRes = (plan.Dose.XRes / 10);
                if (expectedRes == actualRes)
                {
                    ResResult = true;
                }
                else if (actualRes < expectedRes)
                {
                    ResResult = null;
                }
            }


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
            //if electrons? hoskins, kenneth patient plan for reference

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
            maxDoseInPTV(plan, out samemax, out globaldosemax, out volname);



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

            double? shortestDistance;
            try
            {
                //make the viewport using helix3Dtoolkit for the collision model
                //call the collision model method and calculate the shortes distance between the gantry cylinder and the body/couches/baseplate
                //return the shortest distance
                shortestDistance = CollisionCheck(plan);



            }
            catch (Exception e)
            {
                MessageBox.Show("Collision check encountered an error.\n" + e.Message);
                shortestDistance = null;
                throw;
            }


            string jawtrackingexpected = "Off";
            if (techname == "VMAT" || techname == "SRS/SBRT" || techname == "IMRT")
            {
                jawtrackingexpected = "Enabled";
            }

            string isJawTrackingOn = (plan.OptimizationSetup.Parameters.Any(x => x is OptimizationJawTrackingUsedParameter)) ? "Enabled" : "Off";

            double totalMUdoub = totalMU(plan);

            double maxBeamMU = maxMU(plan);

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
            double artifactChecked = checkArtifact(plan);

            string fullcoverage = checkDoseCoverage(plan);

            string needflash = "Flash not used";

            string wrongObjectives = checkObjectives(plan);

            string flash1 = checkIfShouldUseFlash(plan, needflash);
            string flash2 = checkIfUsingFlash(plan, usebolus);

            string rpused = "No";
            string rpavail = "No";
            checkRapidplan(plan, out rpused, out rpavail);

            string checkWedgeMU = checkEDWmin(plan);


            List<Tuple<string, string, string, bool?>> OutputList1 = new List<Tuple<string, string, string, bool?>>()
            {

                new Tuple<string, string, string, bool?>("Calc Algo", algoexpected, algoused, algomatch),
                new Tuple<string, string, string, bool?>("Calc Res (cm)", expectedRes.ToString(),  actualRes.ToString(), ResResult),
                new Tuple<string, string, string, bool?>("Photon Heterogeneity", "ON", plan.PhotonCalculationOptions["HeterogeneityCorrection"], (plan.PhotonCalculationOptions["HeterogeneityCorrection"] == "ON")),
                new Tuple<string, string, string, bool?>("Slice Thickness", "<= 3mm", image.ZRes.ToString()+ " mm",  (image.ZRes<=3)? true : (bool?)null),
                new Tuple<string, string, string, bool?>("Max # Slices", "< 300", plan.StructureSet.Image.ZSize.ToString(),  (plan.StructureSet.Image.ZSize<300)? true : (bool?)null),
                new Tuple<string, string, string, bool?>("Days b/w CT and Plan", "< 21 days", CTdiffdays.ToString() + " days",  (CTdiffdays<21)? true : (bool?)null),



                new Tuple<string, string, string, bool?>("User Origin matches CT ISO", "Match", findIsoStructure(plan).ToString(),   findIsoStructure(plan)),
                new Tuple<string, string, string, bool?>("Same Machine", "All Fields", machname, onemachine),
                new Tuple<string, string, string, bool?>("Same Iso", "All Fields", isoname, sameiso),
                new Tuple<string, string, string, bool?>("Same Tech", "All Fields", techname, sametech),
                new Tuple<string, string, string, bool?>("Same Dose Rate", "All Fields", ratename, samerate),
                new Tuple<string, string, string, bool?>("DRRs attached", "All Fields", findDRR(plan).ToString(), findDRR(plan)),
                new Tuple<string, string, string, bool?>("Tol Table Set", "All Fields", toleranceTables, (toleranceTables != "placeholder" && toleranceTables!= "Mixed Tables" && toleranceTables!= "Error" && toleranceTables!="Missing for some beams")? true:  false),
                new Tuple<string, string, string, bool?>("Image and Tx Orientation", "Same", ((image.ImagingOrientation== plan.TreatmentOrientation) ? plan.TreatmentOrientation.ToString() : "DIFFERENT"), (image.ImagingOrientation== plan.TreatmentOrientation)),
                //baseplate?
                new Tuple<string, string, string, bool?>("Artifact HU", "0 HU, if present",  ((artifactChecked== 99999) ? "NONE FOUND" : artifactChecked.ToString()), (artifactChecked== 0 || artifactChecked==99999)? true : (bool?)null),
                new Tuple<string, string, string, bool?>("Other Assigned HU",  "None",  getDensityOverrides(plan),  (getDensityOverrides(plan)=="None")? true : (bool?)null),
                new Tuple<string, string, string, bool?>("Empty Structures", "None", findEmptyStructure(plan), (findEmptyStructure(plan)== "None")),
                new Tuple<string, string, string, bool?>("Jaw Tracking", jawtrackingexpected.ToString(), isJawTrackingOn.ToString(),  (isJawTrackingOn == jawtrackingexpected)? true : false),
                new Tuple<string, string, string, bool?>("Wedges MU", ">=20", checkWedgeMU,  (checkWedgeMU == "Wedges ok" || checkWedgeMU == "No wedges")? true : false),

                new Tuple<string, string, string, bool?>("Total Plan MU", "<=4000", totalMUdoub.ToString(),  (totalMUdoub <= 4000)? true : false),
                new Tuple<string, string, string, bool?>("Beam Max MU", "<=1200", maxBeamMU.ToString(),  (maxBeamMU<1200)? true : false),
                new Tuple<string, string, string, bool?>("Dose Max in Target",  globaldosemax.ToString() + "% ",  volname,  (samemax)? true : (bool?)null),
                new Tuple<string, string, string, bool?>("Structures in Calc Volume",  "100% ",  fullcoverage,  (fullcoverage=="100%")? true : false),
                //new Tuple<string, string, string, bool?>("Flash VMAT", flash1, flash2, (flash1 == flash2) ? true : false),
                //new Tuple<string, string, string, bool?>("RapidPlan Used", rpavail, rpused, (rpavail == rpused) ? true : (bool?)null),
                new Tuple<string, string, string, bool?>("Objective Priorities", "<999", wrongObjectives, (wrongObjectives=="<999")? true : false),
                new Tuple<string, string, string, bool?>("Couch Added", expectedCouches, findSupport(plan), (expectedCouches == findSupport(plan)) ? true : (bool?)null),
                /*in the following line, there is a compact if else statement within a compact if else statement     */
                new Tuple<string, string, string, bool?>("Collision", "No Collision, closest approach >=2cm",(shortestDistance != null) ? Math.Round((double)shortestDistance, 2).ToString()+
                " cm" : null , (shortestDistance >= 2) ? ((shortestDistance>4)? true:(bool?)null) : false)


            
            //stray voxels - check if gaps between slices? check volume of parts somehow?  GetNumberOfSeparateParts()


            //normalization mode?

            //if PRIMARY reference point, that it's getting Rx dose

            //reference point dose limit vs actual reference point dose

            //reference point doses are accurate to actual doses (cord point is true cord max, etc)
            //beam names make sense (laterality, and Beam name matches ID ((to avoid ARC1 name, ARC2 ID)))

            //check plan scheduling from here? 


        };

            if (machname == "TrueBeamNE")
            {
                var beamNE = plan.Beams.FirstOrDefault(s => s.IsSetupField != true);
                if (beamNE.Technique.Id == "SRS ARC" || beamNE.Technique.Id == "SRS STATIC")
                {

                    OutputList.Add(new Tuple<string, string, string, bool?>("Technique", "NO SRS AT NE", "NO SRS AT NE", false));
                }
            }

            OutputList.Reverse();
            OutputListRX.Reverse();


            //output looks something like this
            OutputList.AddRange(OutputList1);
            OutputListRX.AddRange(OutputList2);



            //code for databinding the list of plan check items to the data grid
            ReportDataGrid.ItemsSource = OutputList;
            ReportDataGrid_Rx.ItemsSource = OutputListRX;

            ReportDataGrid.HeadersVisibility = DataGridHeadersVisibility.Column;
            ReportDataGrid_Rx.HeadersVisibility = DataGridHeadersVisibility.Column;


            ReportDataGrid.AutoGenerateColumns = true;
            ReportDataGrid_Rx.AutoGenerateColumns = true;


            ReportDataGrid.Items.Refresh();
            ReportDataGrid_Rx.Items.Refresh();



            //ReportDataGrid.ColumnWidth = HorizontalStackPanel.Width / 4;
            ReportDataGrid.ColumnWidth = HorizontalStackPanel.Width / 6;

            //ReportDataGrid_Rx.ColumnWidth = HorizontalStackPanel.Width / 4;
            ReportDataGrid_Rx.ColumnWidth = HorizontalStackPanel.Width / 6;
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
                    if (Math.Round(beam.Meterset.Value) < 20)
                    {
                        result = Math.Round(beam.Meterset.Value).ToString();
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
                    maxsofar = (Math.Round(beam.Meterset.Value)>maxsofar) ? Math.Round(beam.Meterset.Value) : maxsofar;
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
                else if(beam.ToleranceTableLabel != toleranceTables) //if it's not blank, does it differ from our variable?
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
                else if(beam.ToleranceTableLabel == toleranceTables)
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
            else if(plan.RTPrescription.Technique == "IMRT" && techname == "VMAT")
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

                var PTVStructures = plan.StructureSet.Structures.Where(s => s.StructureCode != null && (s.DicomType == "PTV"))
                                                         .OrderBy(s => s.Id)
                                                         .Select(s => s);
                var CTVStructures = plan.StructureSet.Structures.Where(s => s.StructureCode != null && (s.DicomType == "CTV"))
                                                        .OrderBy(s => s.Id)
                                                        .Select(s => s);
                var GTVStructures = plan.StructureSet.Structures.Where(s => s.StructureCode != null && (s.DicomType == "GTV"))
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
                if (global_DMax != PTV_DMax)
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
                        if (tempID != "CouchInterior" && tempID != "CouchSurface" && tempID != "LeftInnerRail" && tempID != "LeftOuterRail" && tempID != "RightInnerRail" && tempID.ToLower() != "artifact" && tempID !="RightOuterRail" )
                        {
                            if (structurex.StructureCode != null)
                            {
                                if (structurex.StructureCode.DisplayName != "Artifact")
                                {

                                    if (output != "") { output += "\n"; }
                                    output += structurex.Id + " " + assignedHU.ToString();
                                }
                            }
                        }
                    }
                }


            }
            if (output == "") { output = "None"; }
            return output;
        }
        public static double checkArtifact(PlanSetup plan)
        {
            var artifactStructure = plan.StructureSet.Structures.FirstOrDefault(s => s.StructureCode != null && s.StructureCode.DisplayName == "Artifact");

            double assignedHU = 99999;

            if (artifactStructure != null)
            {
                artifactStructure.GetAssignedHU(out assignedHU);

            }
            return assignedHU;
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

        public static bool findIsoStructure(PlanSetup plan)
        {

            var isoStructure = plan.StructureSet.Structures.FirstOrDefault(s => s.Id.Contains("ISO") && s.DicomType != "MARKER");
            //iso structure and iso marker may vary slighty. we are going to rely on the structure. 

            if (isoStructure != null)
            {
                var isoPoint = isoStructure.CenterPoint;
                var originPoint = plan.StructureSet.Image.UserOrigin;
               
                if (Math.Abs(isoPoint.x - originPoint.x) < 0.001 &&
                    Math.Abs(isoPoint.y - originPoint.y) < 0.001 &&
                    Math.Abs(isoPoint.z - originPoint.z) < 0.001)
                {
                    return true;
                }
            }

            return false;
        }


        
        //add cone for electron checks (about 3.5cm clearance)
        //check full circle and only arclength collision? Seperate checks?
        public static double CollisionCheck(PlanSetup plan)
        {
            //38 cm from iso = nono
            //couches are inserted in plan as support structures
            //If theres no couch it's prob a H/N with the baseplate included in the exteral
            //need a solution for static photon beams
            


            var supportStructures = plan.StructureSet.Structures.Where(c => c.DicomType == "SUPPORT").ToList();
            var body = plan.StructureSet.Structures.Where(c => c.DicomType == "EXTERNAL").FirstOrDefault();
          


            var basePlate =  plan.StructureSet.Structures.Where(c => c.Id.ToLower().Contains("baseplate") || c.Id.ToLower().Contains("base plate")).FirstOrDefault();

            Point3DCollection bodyMeshPoints = body.MeshGeometry.Positions;

            Point3DCollection BodyPlusCouch = new Point3DCollection();


            //check if couches/baseplate are present
            if (basePlate == null && supportStructures.Any() == false )
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

            var shortestDistance = ShortestDistance(BodyPlusCouch.ToList(), GantryCirclePoints, plan.Beams.Where(x=> x.IsSetupField == false).First().IsocenterPosition);
            //list of 2 points,  body and cylinder points that have the shortes distance between them
            var resultingCoords = changeDICOMtoUserCoords(shortestDistance, plan);
            //show results if necessary for debugging
            //MessageBox.Show(resultingCoords.First().x.ToString()+ " : " + resultingCoords.First().y.ToString() + " : " + resultingCoords.First().z.ToString() + " \n"
            //    + resultingCoords.Last().x.ToString() + " : " + resultingCoords.Last().y.ToString() + " : " + resultingCoords.Last().z.ToString() + " \n" +
            //    shortestDistance.Item3.ToString() + " cm");


            return shortestDistance.Item3;

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


        public static List<Point3D> AddCylinderToMesh(PlanSetup plan)
        {

            VVector isocenter = plan.Beams.First(c => c.IsSetupField == false).IsocenterPosition;
            var body = plan.StructureSet.Structures.Where(c => c.DicomType == "EXTERNAL").FirstOrDefault();



            List<Point3D> GantryCirclePoints;

            //make e elctron cone, or gantry arc, or gantry plane
            if (plan.Beams.FirstOrDefault(c => c.IsSetupField == false).EnergyModeDisplayName.ToLower().Contains("e"))
            {
                GantryCirclePoints = CreateConePlane(isocenter, plan.StructureSet.Image, plan);
            }
            else if(plan.Beams.FirstOrDefault(c => c.IsSetupField == false).ControlPoints.First().GantryAngle !=
                plan.Beams.FirstOrDefault(c => c.IsSetupField == false).ControlPoints.Last().GantryAngle)
            {
                GantryCirclePoints = CreateGantryArc(isocenter, 380, plan.StructureSet.Image, 10, plan);
            }
            else
            {
                //loop through each static beam and make the gantry plane
                List<double> gantryAngleList = new List<double>();
                List<Point3D> pointList = new List<Point3D>();

                foreach (var beam in plan.Beams.Where(c=> c.IsSetupField == false))
                {
                    if (gantryAngleList.Contains(beam.ControlPoints.FirstOrDefault().GantryAngle) == false)
                    {
                        pointList.AddRange(CreateStaticPlane(isocenter, plan.StructureSet.Image, beam));
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
        public static List<Point3D> CreateGantryArc(VVector isocenter, double circleRadius, VMS.TPS.Common.Model.API.Image image, double thetaDegrees, PlanSetup plan)
        {
            List<Point3D> oneSlicePointList = new List<Point3D>();

            //radians along the circle where we will put points
            double smaplingRate = thetaDegrees *(Math.PI / 180);

            for (double i = 0; i < ((Math.PI*2)/ smaplingRate); i+= smaplingRate)
            {
                //72 iterations of 0.87 radians makes a full circle (every 5 degrees)
                //i is theta
                double x;
                double y;

                //in mm
                x = circleRadius * Math.Cos(i);
                //y direction is reversed in eclipse
                y = -circleRadius * Math.Sin(i);

                Point3D circleCoord = new Point3D(isocenter.x + x, isocenter.y + y, isocenter.z);


                //check if the current angle on the circle falls in one of the arc sectors
                foreach (var beam in plan.Beams.Where(c=> c.IsSetupField == false))
                {
                    var arcsectors = GetArcSectors(beam);

                    //MessageBox.Show("arcsector1 " + arcsectors.Item1 + " arcsector2 " + arcsectors.Item2);


                    if (arcsectors.Item3 == GantryDirection.CounterClockwise)
                    {
                        if (arcsectors.Item1> arcsectors.Item2)
                        {
                            //passing through 0 polar

                            if (i>= arcsectors.Item1 && i<= 359*(Math.PI/180))
                            {
                                oneSlicePointList.Add(circleCoord);
                            }
                            else if (i>=0 && i<= arcsectors.Item2)
                            {
                                oneSlicePointList.Add(circleCoord);
                            }
                        }
                        else
                        {

                            if (i  >= arcsectors.Item1 && i <= arcsectors.Item2)
                            {
                                oneSlicePointList.Add(circleCoord);

                            }
                        }

                    }
                    else
                    {
                        if (arcsectors.Item1 < arcsectors.Item2 && (arcsectors.Item2 >= 270*(Math.PI/180)))
                        {

                            //passing through 0 polar
                            if (i >= 0 &&  i <=arcsectors.Item1)
                            {
                                oneSlicePointList.Add(circleCoord);

                            }
                            else if (i<= 359 * (Math.PI/180) && i>= arcsectors.Item2)
                            {
                                oneSlicePointList.Add(circleCoord);

                            }
                        }
                        else
                        {

                            if (i <= arcsectors.Item1 && i >= arcsectors.Item2)
                            {

                                oneSlicePointList.Add(circleCoord);
                            }
                        }
                    }
                }


            }

            double sliceThickness = image.ZRes;
            List<Point3D> AllPoints = new List<Point3D>();


            //extend the circle to a cylinder, only go ~30cm north and south
            //iterate according to the slice thickness
            for (int i = 1; i < Math.Round(300 / sliceThickness); i++)
            {
                foreach (var point in oneSlicePointList)
                {
                    double PosZ = point.Z + i * sliceThickness;
                    double NegZ = point.Z - i * sliceThickness;

                    Point3D PosPoint = new Point3D(point.X, point.Y, PosZ);
                    Point3D NegPoint = new Point3D(point.X, point.Y, NegZ);

                    AllPoints.Add(PosPoint);
                    AllPoints.Add(NegPoint);

                }

            }
           

            return AllPoints;

        }


        public static List<Point3D> CreateConePlane(VVector isocenter, VMS.TPS.Common.Model.API.Image image, PlanSetup planSetup)
        {
            //find the iso position
            //find the SSD? (electron plans are fixed SSD setups)
            //find the direction of the beam (source position to isocenter position)
            //make a plane perpendicular to the beam ~3.5cm upstream from the isocenter
            //the 3.5cm is the clearance from iso to the bottom of the cone
            //the size of the plane will correspond to the size of the e cone


            List<Point3D> AllPoints = new List<Point3D>();
            List<Point3D> AllPointsTranslated = new List<Point3D>();


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
            for (double x = - (MarginValue*10)/2; x <= (MarginValue * 10) / 2; x += 10)
            {
                for (double z = -(MarginValue * 10) / 2; z <= (MarginValue * 10) / 2; z += 10)
                {
                    Vector3 point = new Vector3((float)x, 0, (float)z);
                    Vector3 rotatedVector = Vector3.Transform(point, rotMatrix);
                    

                    VVector point3D = new VVector(rotatedVector.X, rotatedVector.Y, rotatedVector.Z);
                    VVector addedPoint = bottomOfCone + point3D;
                    Point3D finalPoint = new Point3D(addedPoint.x, addedPoint.y, addedPoint.z);

                    //newPointList.Add(finalPoint);
                    AllPointsTranslated.Add(finalPoint);

                }
            }


            return AllPointsTranslated;


        }

        public static List<Point3D> CreateStaticPlane(VVector isocenter, VMS.TPS.Common.Model.API.Image image, Beam beam)
        {
            
            List<Point3D> AllPoints = new List<Point3D>();
            List<Point3D> AllPointsTranslated = new List<Point3D>();


            //var firstBeam = planSetup.Beams.FirstOrDefault(c => c.IsSetupField == false);
            var gantryAngle = beam.ControlPoints.First().GantryAngle;

            VVector sourceLocation = beam.GetSourceLocation(gantryAngle);
            VVector sourceToIso = isocenter - sourceLocation;

            sourceToIso.ScaleToUnitLength();

            //bottom of cone is ~ 3.5cm back from the iso
            var gantryPlane = isocenter - sourceToIso * 380;

            //define a plane using a normal vector
            //the normal being the source to iso vector(direction of the beam )
            Plane perpendicularPlane = new Plane(new Vector3((float)sourceToIso.x, (float)sourceToIso.y, (float)sourceToIso.z), 0);



            Matrix4x4 rotMatrix = Matrix4x4.CreateFromAxisAngle(new Vector3(0, 0, 1), (float)(gantryAngle * (Math.PI / 180)));

            //mm
            double coneSize = 387.5;


            //Create x-z plane of points
       
            for (double x = -coneSize ; x <= coneSize; x += 10)
            {
                for (double z = -coneSize; z <= coneSize; z += 10)
                {
                    Vector3 point = new Vector3((float)x, 0, (float)z);
                    Vector3 rotatedVector = Vector3.Transform(point, rotMatrix);


                    VVector point3D = new VVector(rotatedVector.X, rotatedVector.Y, rotatedVector.Z);
                    VVector addedPoint = gantryPlane + point3D;
                    Point3D finalPoint = new Point3D(addedPoint.x, addedPoint.y, addedPoint.z);

                    //newPointList.Add(finalPoint);
                    AllPointsTranslated.Add(finalPoint);

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
            if (gantryAngle <=90)
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
        public static Tuple<Point3D, Point3D, double> ShortestDistance(List<Point3D> bodyMeshPositions, List<Point3D> cylinderMeshPositions, VVector isocenter)
        {
            Point3D returnPoint1 = new Point3D();
            Point3D returnPoint2 = new Point3D();
            double shortestDistance = 2000000;
            int i = 0;

            //only use body points which are in the neighborhood of the iso in the z direction
            var zList = cylinderMeshPositions.Select(c => c.Z).ToList();
            zList.Sort();
            var zMin = zList.First();
            var zMax = zList.Last();

            List<Point3D> nearbyBodyPositions = new List<Point3D>();


            //find the neaby body points that you want to measure distance from the mesh
            //use some extra body points if you are comparing e cone (easier to see body this way)
            if (Math.Abs(zMax-zMin) <= 300)
            {
                nearbyBodyPositions = bodyMeshPositions.Where(c => c.Z <= zMax + 100 && c.Z >= zMin - 100).ToList();
            }
            else
            {
                nearbyBodyPositions = bodyMeshPositions.Where(c => c.Z <= zMax && c.Z >= zMin).ToList();
            }


            //group the cylinder positions together with cylinder positions? to prevent the farthest ones away to have to compare
            var groupedCylinderMeshLists = cylinderMeshPositions.GroupBy(c => c.Z).ToList();
            var groupedNearbyBodyPositionsLists = nearbyBodyPositions.GroupBy(c => c.Z).ToList();

            //compare the body and mesh points in groups to save time
            //Should prevent redundant distance checks
            List<double> bodyZList = new List<double>(); 
            foreach (var groupCylinder in groupedCylinderMeshLists)
            {
                foreach (var groupBody in groupedNearbyBodyPositionsLists)
                {
                    if (Math.Abs(groupCylinder.Key - groupBody.Key) <= 3  && (bodyZList.Contains(groupBody.Key)==false))
                    {

                        foreach (Point3D point1 in groupBody)
                        {
                            foreach (Point3D point2 in groupCylinder)
                            {
                                if (shortestDistance >= 0.3)
                                {
                                    i++;
                                    double distance = (Math.Sqrt((Math.Pow((point2.X - point1.X), 2)) + (Math.Pow((point2.Y - point1.Y), 2))
                                        + (Math.Pow((point2.Z - point1.Z), 2)))) / 10;
                                   
                                    if (distance < shortestDistance)
                                    {
                                        shortestDistance = distance;
                                        returnPoint1 = point1;
                                        returnPoint2 = point2;
                                    }
                                }
                                
                            }
                        }


                        bodyZList.Add(groupBody.Key);
                    }

                }

            }



            //get every nth point from the body mesh
            //saves time by not plotting every point
            List<Point3D> every10thBody = nearbyBodyPositions.Where((item, index) => (index + 1) % 25 == 0).ToList();
            List<Point3D> every10thMesh = cylinderMeshPositions.Where((item, index) => (index + 1) % 2 == 0).ToList();
            //add the points to a model
            PointsVisual3D pointsVisual3D = new PointsVisual3D()
            {
                Color = Colors.Blue,
                Size = 2,
                Points = new Point3DCollection(every10thBody)
            };

            ModelVisual3D modelVisual3D = new ModelVisual3D();
            modelVisual3D.Children.Add(pointsVisual3D);


            //do the same for the cylinder mesh
            PointsVisual3D pointsVisual3Dcyl = new PointsVisual3D()
            {
                Color = Colors.Green,
                Size = 2,
                Points = new Point3DCollection(every10thMesh)
            };

            modelVisual3D.Children.Add(pointsVisual3Dcyl);
            viewPort.Children.Clear();
            viewPort.Children.Add(modelVisual3D);


            //set the default camera view
            PerspectiveCamera camera = new PerspectiveCamera()
            {
                Position = new Point3D(2.9, -2148, -3758),
                LookDirection = new Vector3D(0,2296,3747),
                UpDirection = new Vector3D(0, -0.853, 0),
                FieldOfView = 10,
                


            };

            //change the zoom sensitivity
            viewPort.ZoomSensitivity = 0.5;

            //viewPort.ShowCameraInfo = true;

            viewPort.Camera = camera;

            Tuple<Point3D, Point3D, double> returnTuple = new Tuple<Point3D, Point3D, double>(returnPoint1, returnPoint2, shortestDistance);

            return returnTuple;
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

        private static void FillPlanComboBox(ScriptContext scriptContext, ComboBox comboBox)
        {
            List <PlanSetup> scopePlans = scriptContext.PlansInScope.ToList();

            foreach (var plan in scopePlans)
            {
                
                comboBox.Items.Add(plan.Id);
            }

            foreach (var item in comboBox.Items)
            {
                if ((string)item == scriptContext.PlanSetup.Id)
                {
                    comboBox.SelectedItem = item;
                }
            }


        }

        private void PlanComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            
            Main(context1, PlanComboBox, viewPort, OutputList, OutputListRX, HorizontalStackPanel, ReportDataGrid, ReportDataGrid_Rx);
        }
    }
}
