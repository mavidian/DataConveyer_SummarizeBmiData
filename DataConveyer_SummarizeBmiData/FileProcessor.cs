// Copyright © 2019 Mavidian Technologies Limited Liability Company. All Rights Reserved.

using Mavidian.DataConveyer.Common;
using Mavidian.DataConveyer.Entities.KeyVal;
using Mavidian.DataConveyer.Logging;
using Mavidian.DataConveyer.Orchestrators;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace DataConveyer_SummarizeBmiData
{
   /// <summary>
   /// Represents Data Conveyer functionality specific to summarizing BMI data by state.
   /// </summary>
   internal class FileProcessor
   {
      private readonly IOrchestrator Orchestrator;

      internal FileProcessor(string inFile, string outLocation)
      {
         var config = new OrchestratorConfig()
         //To facilitate troubleshooting logging data can be sent to a DataConveyer.log file:
         //var config = new OrchestratorConfig(LoggerCreator.CreateLogger(LoggerType.LogFile, "testBMI", LogEntrySeverity.Information))
         {
            GlobalCacheElements = new string[] { "AK","AL","AR","AZ","CA","CO","CT","DC","DE","FL","GA","HI","IA","ID","IL","IN","KS",
                                                 "KY","LA","MA","MD","ME","MI","MN","MO","MS","MT","NC","ND","NE","NH","NJ","NM","NV",
                                                 "NY","OH","OK","OR","PA","RI","SC","SD","TN","TX","UT","VA","VT","WA","WI","WV","WY" },
            RecordInitiator = InitializeGlobalCache,
            InputDataKind = KindOfTextData.JSON,
            InputFileName = inFile,
            XmlJsonIntakeSettings = "CollectionNode|,RecordNode|",  //an array of objects (i.e. records)
            ExplicitTypeDefinitions = "BirthDate|D,Height.ft|I,Height.in|I,Weight|I",  //matches JSON types, so no type conversion occurs in Data Conveyer
            AppendFootCluster = true,  // contains summarized data
            AllowOnTheFlyInputFields = true,
            ConcurrencyLevel = 4,
            TransformerType = TransformerType.Universal,
            UniversalTransformer = CumulateBmiData,
            AllowTransformToAlterFields = true,
            OutputDataKind = KindOfTextData.XML,
            OutputFileName = outLocation + Path.DirectorySeparatorChar + Path.GetFileNameWithoutExtension(inFile) + "_summary.xml",
            XmlJsonOutputSettings = "CollectionNode|StateSummaryRecords,RecordNode|StateSummaryRecord,IndentChars|  "
         };

         Orchestrator = OrchestratorCreator.GetEtlOrchestrator(config);
      }

      /// <summary>
      /// Execute Data Conveyer process.
      /// </summary>
      /// <returns>Task containing the process results.</returns>
      internal async Task<ProcessResult> ProcessFileAsync()
      {
         var result = await Orchestrator.ExecuteAsync();
         Orchestrator.Dispose();

         return result;
      }


      /// <summary>
      /// Record initiator used here to initialize global cache elements.
      /// </summary>
      /// <param name="rec"></param>
      /// <param name="traceBin"></param>
      /// <returns></returns>
      private bool InitializeGlobalCache(IRecord rec, IDictionary<string,object> traceBin)
      {
         if (rec.RecNo == 1)  //initialize global cache only once
         {
            var gc = rec.GlobalCache;
            foreach (var elem in gc.Elements)
            {
               //Each value stored in global cache is a tuple containing AllCount, OwCount, HeightTotal, WeightTotal, BmiTotal
               gc.ReplaceValue<object, (int, int, int, int, float)>(elem.Key, _ => (0, 0, 0, 0, 0f));
            }
         }
         return true; //return value is irrelevant here
      }

      /// <summary>
      /// Universal transformer to cumulate BMI data in global cache and remove cluser from output (only summary data (foot cluster) is sent to output).
      /// In case of foot cluster, prepare summary data to output.
      /// </summary>
      /// <param name="cluster"></param>
      /// <returns>Nothing (i.e. clusters are filtered out), except for the foot cluster (which contains summary dat).</returns>
      private IEnumerable<ICluster> CumulateBmiData(ICluster cluster)
      {
         //Each value stored in global cache is a tuple containing AllCount, OwCount, HeightTotal, WeightTotal, BmiTotal
         var gc = cluster.GlobalCache;

         if (cluster.StartRecNo == Constants.FootClusterRecNo)
         {  //Foot cluster 
            //Note that Data Conveyer guarantees that foot cluster will be processed AFTER all other clusters have been processed.

            //Prepare a foot cluster containing a single record with summary data to output
            foreach (var element in gc.Elements.OrderBy(el => el.Key))
            {
               var stateTotals = ((int allCount, int owCount, int heightTotal, int weightTotal, float bmiTotal))element.Value;
               var footRec = cluster.ObtainEmptyRecord();
               footRec.AddItem("State", element.Key);
               footRec.AddItem("TotalHeadount", stateTotals.allCount);
               var (feet, inches) = Average(stateTotals.heightTotal, stateTotals.allCount).ToHeight();
               footRec.AddItem("AverageHeight", feet + "'" + inches + "''");
               footRec.AddItem("AverageWeight", Average(stateTotals.weightTotal, stateTotals.allCount) + "lbs");
               footRec.AddItem("AverageBMI", string.Format("{0:##0.0}", stateTotals.allCount == 0 ? 0d : stateTotals.bmiTotal / stateTotals.allCount));
               footRec.AddItem("OverweightHeadount", stateTotals.owCount);
               footRec.AddItem("PercentageOverweight", Average(stateTotals.owCount * 100, stateTotals.allCount) + "%");

               cluster.AddRecord(footRec);
            }

            return Enumerable.Repeat(cluster, 1);
         }

         //Regular cluster - save data into global cache
         var rec = cluster[0];  //all clusters contain single records

         var state = (string)rec["Residence"];
         var height = ((int)rec["Height.ft"], (int)rec["Height.in"]).ToInches();
         var weight = (int)rec["Weight"];
         var age = ((DateTime)rec["BirthDate"]).ToAge();
         var isMale = ((string)rec["Gender"]).ToUpper()[0] == 'M';

         gc.ReplaceValue<(int, int, int, int, float), (int, int, int, int, float)>(state, prevTotals => UpdateStateTotals(prevTotals, height, weight, age, isMale));

         return Enumerable.Empty<ICluster>(); //no data from regular cluster is sent to output
      }

      /// <summary>
      /// Helper function to update state totals tuple
      /// </summary>
      /// <param name="prevTotals"></param>
      /// <param name="height"></param>
      /// <param name="weight"></param>
      /// <param name="age"></param>
      /// <param name="isMale"></param>
      /// <returns></returns>
      private (int allCount, int owCount, int heightTotal, int weightTotal, float bmiTotal) UpdateStateTotals
                   (
                     (int allCount, int owCount, int heightTotal, int weightTotal, float bmiTotal) prevTotals,
                     int height, int weight, int age, bool isMale
                   )
      {
         var bmi = (height, weight).ToBmi();
         return (prevTotals.allCount + 1,
                 prevTotals.owCount + ((bmi, age, isMale).IsOverweight() ? 1 : 0),
                 prevTotals.heightTotal + height,
                 prevTotals.weightTotal + weight,
                 prevTotals.bmiTotal + bmi);
      }

      /// <summary>
      /// Calculate average (to the nearest integer value)
      /// </summary>
      /// <param name="total"></param>
      /// <param name="count"></param>
      /// <returns></returns>
      private static int Average(int total, int count)
      {
         if (count == 0) return 0;
         return (int)Math.Round((double)total / count);
      }

   }
}
