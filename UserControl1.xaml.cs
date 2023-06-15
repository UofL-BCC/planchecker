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
using System.Windows.Navigation;
using System.Windows.Shapes;
using VMS.TPS.Common.Model.API;
using VMS.TPS.Common.Model.Types;

namespace PlanChecks
{
    /// <summary>
    /// Interaction logic for UserControl1.xaml
    /// </summary>
    public partial class UserControl1 : UserControl
    {
        public List<Tuple<string, string, string, bool?>> OutputList = new List<Tuple<string, string, string, bool?>>();
        public List<Tuple<string, string, string, bool?>> OutputListRX = new List<Tuple<string, string, string, bool?>>();

        public UserControl1(ScriptContext context, Window window1)
        {
            InitializeComponent();

            //code behind goes here

            Patient mypatient = context.Patient;
            Course course = context.Course;

            PlanSetup plan = context.PlanSetup;
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
                if(expectedRes == actualRes)
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

            

            

            

            
            



            //int CTdiffdays = int.Parse(truncatedStr);


            

            //ReferencePoint refpoint = plan.AddReferencePoint(false, null, "TrackingPoint");

            

            //MessageBox.Show(truncatedStr);

            

           

            
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
                
                new Tuple<string, string, string, bool?>("Total Plan MU", "<=4000", totalMUdoub.ToString(),  (totalMUdoub <= 4000)? true : false),
                new Tuple<string, string, string, bool?>("Beam Max MU", "<=1200", maxBeamMU.ToString(),  (maxBeamMU<1200)? true : false),
                new Tuple<string, string, string, bool?>("Dose Max in Target",  globaldosemax.ToString() + "% ",  volname,  (samemax)? true : (bool?)null),
                new Tuple<string, string, string, bool?>("Structures in Calc Volume",  "100% ",  fullcoverage,  (fullcoverage=="100%")? true : false),
                new Tuple<string, string, string, bool?>("Couch Added", expectedCouches, findSupport(plan), (expectedCouches == findSupport(plan)) ? true : (bool?)null),




            //stray voxels - check if gaps between slices? check volume of parts somehow?  GetNumberOfSeparateParts()


            //tolerance table set
            //no contours(including body/external) outside dose grid [check that DVH structure dose coverage is 100%?. And sample coverage?]
            //CT/Structureset is older than plan and within 21 days from plan? 
            //correct CT? based on what? date? 
            //normalization mode?

            //if PRIMARY reference point, that it's getting Rx dose

            //reference point dose limit vs actual reference point dose

            //reference point doses are accurate to actual doses (cord point is true cord max, etc)
            //beam names make sense (laterality, and Beam name matches ID ((to avoid ARC1 name, ARC2 ID)))

            //check plan scheduling from here? 


        };

            if (machname== "TrueBeamNE")
            {
                var beamNE = plan.Beams.FirstOrDefault(s => s.IsSetupField != true);
                if(beamNE.Technique.Id == "SRS ARC" || beamNE.Technique.Id == "SRS STATIC"){
                    
                    OutputList.Add(new Tuple<string, string, string, bool?>("Technique", "NO SRS AT NE","NO SRS AT NE", false));
                }
            }
        
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



            ReportDataGrid.ColumnWidth = HorizontalStackPanel.Width / 4;

            ReportDataGrid_Rx.ColumnWidth = HorizontalStackPanel.Width / 4;


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

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {

        }

        private void TextBox_TextChanged_1(object sender, TextChangedEventArgs e)
        {

        }
    }
}
