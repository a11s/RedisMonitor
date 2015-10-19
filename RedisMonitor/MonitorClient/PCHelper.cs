using MonitorClient;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedisPerformanceCounter
{
    public class PCHelper
    {
        static bool _isMono = false;
        static Type t;
        public static bool IsMono
        {
            get
            {
                if (t == null)
                {
                    t = Type.GetType("Mono.Runtime");
                    _isMono = t != null;
                }
                return _isMono;
            }
        }
        public const string CategoryNamePrefix = "Redis";
        /// <summary>
        /// after all,it doesnt support multi server.may be next version support multi instance
        /// </summary>
        public static string InstanceName = "localhost";

        static Dictionary<string, PerformanceCounterCategory> AllPCC;

        static CounterCreationDataCollection ccdc;



        static bool created = false;
        static bool countercreated = false;

        static Dictionary<string, PerformanceCounter> allCounters;
        public static void CheckPCCategory(Dictionary<string, Dictionary<string, string>> data)
        {
            if (AllPCC == null)
            {
                AllPCC = new Dictionary<string, PerformanceCounterCategory>();
                allCounters = new Dictionary<string, PerformanceCounter>();
            }
            if (!created)
            {                
                #region create all cat
                foreach (var item in data)
                {
                    bool isRedisCategoryExist = false;
                    var catname = CategoryNamePrefix + item.Key;
                    if (!AllPCC.ContainsKey(catname))
                    {
                        isRedisCategoryExist = PerformanceCounterCategory.Exists(catname);//doesnot support remote performancecounter
                        if (!isRedisCategoryExist)
                        {
                            ccdc = CreateCounterCreationDataCollection(item);
                            if (ccdc == null)
                            {
                                continue;
                            }
                            //try create new one
                            var pcc = PerformanceCounterCategory.Create(catname, catname, PerformanceCounterCategoryType.MultiInstance, ccdc);
                            AllPCC[pcc.CategoryName] = pcc;
                        }
                        else
                        {
                            var pcc = PerformanceCounterCategory.GetCategories().First(a => a.CategoryName == catname);
                            try
                            {
                                //pcc.GetCounters(InstanceName);
                                AllPCC[pcc.CategoryName] = pcc;
                            }
                            catch (Exception ee)
                            {


                            }
                        }
                    }
                    else
                    {
                        //update?

                    }
                }
                #endregion

                #region createAllCounters
                foreach (var cat in data)
                {
                    var catname = CategoryNamePrefix + cat.Key;
                    if (!AllPCC.ContainsKey(catname))
                    {
                        continue;
                    }
                    var pcc = AllPCC[catname];
                    //var allcounters = pcc.GetCounters().ToDictionary(a => a.CounterName);

                    var allitems = cat.Value;
                    foreach (var item in allitems)
                    {
                        var s = item.Value;
                        PerformanceCounter pc = new PerformanceCounter(catname, item.Key, InstanceName, false);
                        allCounters[item.Key] = pc;
                    }
                    //for (int i = 0; i < allcounters.Length; i++)
                    //{
                    //    var pc = allcounters[i];
                    //}
                }
                countercreated = true;
                #endregion
                created = true;
            }
            else
            {
                ////should update counter
                //if (countercreated)
                //{
                //    UpdatePC(data);
                //}
                //else
                //{
                //    UpdatePC(data, true);
                //    countercreated = true;
                //}


                UpdatePC(data);
            }
        }

        //private static void UpdatePC(Dictionary<string, Dictionary<string, string>> data, bool setupCounter = false)
        //{
        //    foreach (var cat in data)
        //    {
        //        var catname = CategoryNamePrefix + cat.Key;
        //        if (!AllPCC.ContainsKey(catname))
        //        {
        //            continue;
        //        }
        //        var pcc = AllPCC[catname];
        //        //var allcounters = pcc.GetCounters().ToDictionary(a => a.CounterName);

        //        PerformanceCounter[] allcounters;
        //        //if (setupCounter)
        //        //{
        //            allcounters = allCounters.Where(a => a.Value.CategoryName == catname).Select(a => a.Value).ToArray();
        //        //}
        //        //else
        //        //{
        //        //    allcounters = pcc.GetCounters(InstanceName);
        //        //}
        //        var allitems = cat.Value;
        //        for (int i = 0; i < allcounters.Length; i++)
        //        {
        //            var pc = allcounters[i];
        //            string strval = null;
        //            if (cat.Value.TryGetValue(pc.CounterName, out strval))
        //            {
        //                double v;
        //                pc.ReadOnly = false;
        //                if (setupCounter)
        //                {
        //                    pc.InstanceName = InstanceName;
        //                }
        //                try
        //                {

        //                    if (InfoClient.StringToNumber(strval, out v))
        //                    {
        //                        pc.RawValue = (long)v;
        //                    }
        //                    else
        //                    {
        //                        pc.RawValue = 0;
        //                    }
        //                }
        //                catch (Exception ex)
        //                {
        //                    System.Diagnostics.Debug.Assert(false, pc.CounterName);

        //                }
        //            }
        //            else
        //            {
        //                System.Diagnostics.Debug.Assert(false, pc.CounterName);
        //            }
        //        }

        //    }
        //}
        private static void UpdatePC(Dictionary<string, Dictionary<string, string>> data)
        {
            foreach (var cat in data)
            {
                var catname = CategoryNamePrefix + cat.Key;
                if (!AllPCC.ContainsKey(catname))
                {
                    continue;
                }
                foreach (var item in cat.Value)
                {
                    PerformanceCounter pc;
                    if (allCounters.TryGetValue(item.Key, out pc))
                    {
                        string strval = item.Value;
                        double v;
                        try
                        {
                            if (InfoClient.StringToNumber(strval, out v))
                            {
                                pc.RawValue = (long)v;
                            }
                            else
                            {
                                pc.RawValue = 0;
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.Assert(false, pc.CounterName);

                        }
                    }
                    else
                    {
                        //do nothing
                    }
                }
            }
        }

        private static CounterCreationDataCollection CreateCounterCreationDataCollection(KeyValuePair<string, Dictionary<string, string>> item)
        {
            //"#Memory  used_memory 1234"
            var ccdc = new CounterCreationDataCollection();

            foreach (var classitem in item.Value)
            {
                var subitems = InfoClient.GetSubItems(classitem.Value);
                CounterCreationData data1 = new CounterCreationData(classitem.Key, classitem.Key, PerformanceCounterType.NumberOfItems32);
                ccdc.Add(data1);
            }
            if (ccdc.Count == 0)
            {
                return null;
            }

            return ccdc;
        }


        public static void CleanUp()
        {
            RemovePCCategory();
        }
        public static void RemovePCCategory()
        {
            if (AllPCC == null)
            {
                return;
            }
            foreach (var item in AllPCC)
            {
                PerformanceCounterCategory.Delete(item.Key);
            }
        }
        public static void RemovePCCategoryUNSAFE()
        {
            var AllPCC = PerformanceCounterCategory.GetCategories().Where(a => a.CategoryName.StartsWith(CategoryNamePrefix));
            foreach (var item in AllPCC)
            {
                PerformanceCounterCategory.Delete(item.CategoryName);
            }
        }
    }
}
