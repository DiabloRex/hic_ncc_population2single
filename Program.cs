using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace hic_ncc_population2single
{
    class Program
    {
        static void Main(string[] args)
        {

            if (args.Length < 3)
            {
                Console.WriteLine("Not Enough Parameters");
                help();
                return;
            }

            string input_file = args[0];
            string output_file = args[1];
            int lowest_n = Convert.ToInt32(args[2]);

            //string input_file = "A8_R1.ncc";
            //string output_file = "A8_R1.new.ncc";
            //int lowest_n = 3;

            Console.WriteLine($"Transforming populational hic data: {input_file} to single cell hic data {output_file}");
            Console.WriteLine("step 1: reading input file ...");

            //benchmark
            //int processed = 0;
            var start = DateTime.Now;

            Method m = Method.PointMax;

            if(args.Length == 4)
            {
                m = (Method)Convert.ToInt32(args[3]);
            }


            Console.WriteLine("\tUsing Method: " + m);


            if (m == Method.PointMax)
            {
                // read orignal ncc file
                // key: contact <location sorted>, format <chr1:position1-chr2:postion2> (only re1 position is considered), string for storing ncc lines for write
                Dictionary<string, contact> contacts_count = new Dictionary<string, contact>();
                HashSet<string> postions = new HashSet<string>();

                using (StreamReader sr = new StreamReader(input_file))
                {
                    while (!sr.EndOfStream)
                    {
                        string ts = sr.ReadLine();
                        var items = ts.Split(' ');
                        string chr1 = (items[0].LastIndexOf("chr") > -1 ? items[0].Substring(items[0].LastIndexOf("chr")) : items[0]) + ":" + items[3] ;
                        string chr2 = (items[6].LastIndexOf("chr") > -1 ? items[6].Substring(items[6].LastIndexOf("chr")) : items[6]) + ":" + items[9] ;

                        if (!postions.Contains(chr1)) postions.Add(chr1);
                        if (!postions.Contains(chr2)) postions.Add(chr2);

                        //for chr1
                        if (contacts_count.ContainsKey(chr1))
                        {
                            contacts_count[chr1].TotalCount++;
                            contacts_count[chr1].TotalLines.Add(ts);
                            //if (contacts_count[chr1].EachCount.ContainsKey(chr2))
                            //{
                            //    contacts_count[chr1].EachCount[chr2]++;
                            //}
                            //else
                            //{
                            //    contacts_count[chr1].EachCount.Add(chr2, 1);
                            //    contacts_count[chr1].EachLine.Add(chr2, ts);
                            //}
                        }
                        else
                        {
                            contacts_count.Add(chr1, new contact()
                            {
                                TotalCount = 1,
                                TotalLines = new List<string>() { ts }
                                //EachLine = new Dictionary<string, string>() { { chr1, ts } },
                                //EachCount = new Dictionary<string, int>() { { chr1, 1 } }
                            });
                        }

                        //for chr2
                        if (contacts_count.ContainsKey(chr2))
                        {
                            contacts_count[chr2].TotalCount++;
                            contacts_count[chr2].TotalLines.Add(ts);

                            //if (contacts_count[chr2].EachCount.ContainsKey(chr1))
                            //{
                            //    contacts_count[chr2].EachCount[chr1]++;
                            //}
                            //else
                            //{
                            //    contacts_count[chr2].EachCount.Add(chr1, 1);
                            //    contacts_count[chr2].EachLine.Add(chr1, ts);
                            //}
                        }
                        else
                        {
                            contacts_count.Add(chr2, new contact()
                            {
                                TotalCount = 1,
                                TotalLines = new List<string>() { ts }
                                //EachLine = new Dictionary<string, string>() { { chr2, ts } },
                                //EachCount = new Dictionary<string, int>() { { chr2, 1 } }
                            });
                        }
                    }
                }

                //sorting contacts
                Console.WriteLine("step 2: sorting contacts based on contact number ...");

                List<(string, int)> list = new List<(string, int)>();
                foreach (var item in contacts_count)
                {
                    list.Add((item.Key, item.Value.TotalCount));
                }

                list = list.OrderByDescending(a => a.Item2).ToList();

                //convert list to sorted hashset list
                List<HashSet<string>> sorted_hashset = new List<HashSet<string>>();
                sorted_hashset.Add(new HashSet<string>());

                //select positions
                // setting for filter
                int lowest_contact = lowest_n;

                int current_set = list[0].Item2;
                for (int i = 0; i < list.Count; i++)
                {
                    if (list[i].Item2 < current_set)
                    {
                        if (list[i].Item2 < lowest_contact) break;
                        current_set = list[i].Item2;
                        sorted_hashset.Add(new HashSet<string>() { list[i].Item1 });
                        continue;
                    }
                    sorted_hashset[sorted_hashset.Count - 1].Add(list[i].Item1);
                }


                //process contacts

                int percent = 101;
                int total_num = contacts_count.Count;
                HashSet<string> left_positions = contacts_count.Keys.ToHashSet();

                Console.WriteLine("step 3: processing contacts and outputing result file ...");

                using (StreamWriter sw = new StreamWriter(output_file))
                {
                    foreach (var pos in contacts_count.Keys.ToList())
                    {
                        //processed++;
                        //if(processed == 1000)
                        //{
                        //    var end = DateTime.Now;
                        //    Console.WriteLine((end-start).TotalMilliseconds + " Milliseconds used for first 1000 reads!");
                        //}

                        if (sorted_hashset.Count == 0)
                        {
                            Console.WriteLine(left_positions.Count + " Contacts not processed due to contacts lower than " + lowest_contact + ". Stopping processing...");
                            break;
                        }


                        if (left_positions.Count < 2) break;

                        if (left_positions.Count * 100 / total_num < percent)
                        {
                            percent = left_positions.Count * 100 / total_num;
                            Console.WriteLine(percent + "% Left...");
                            sw.Flush();
                        }

                        if (!left_positions.Contains(pos)) continue;

                        processingContacts(contacts_count, sw, left_positions, sorted_hashset);
                    }
                }
            }
            else if (m == Method.TotalRandom)
            {
                //read all the reads in the ncc file
                List<string> contacts = new List<string>();

                contacts = File.ReadAllLines(input_file).ToList();

                Console.WriteLine($"Total contacts: {contacts.Count}...");


                Console.WriteLine("step 2: Randomizing reads and outputing result ...");

                HashSet<string> included_pos = new HashSet<string>();
                Random r = new Random();
                var sw = new StreamWriter(output_file);
                int cSize = contacts.Count;
                int percent = -1;


                for (int i = 0; i < cSize; i++)
                {
                    if (i * 100 / cSize > percent)
                    {
                        percent = i * 100 / cSize;
                        Console.WriteLine(percent + "% contacts processed... " + contacts.Count + " contacts lefted...    " + DateTime.Now);
                    }
                    var index = r.Next(contacts.Count);
                    var ts = contacts[index];
                    var items = ts.Split(' ');
                    string chr1 = items[0][items[0].LastIndexOf("chr")..] + ":" + items[3];
                    string chr2 = items[6][items[6].LastIndexOf("chr")..] + ":" + items[9];

                    if (!included_pos.Contains(chr1) && !included_pos.Contains(chr2))
                    {
                        sw.WriteLine(ts);
                        included_pos.Add(chr1);
                        included_pos.Add(chr2);
                    }
                    contacts.RemoveAt(index);
                }

                sw.Close();
            }
            else if (m == Method.EdgeMax)
            {
                //List<Line> lines = new List<Line>();

                Dictionary<string, int> line_count = new Dictionary<string, int>();
                Dictionary<string, string> line_store = new Dictionary<string, string>();
                HashSet<string> included_pos = new HashSet<string>();

                using (StreamReader sr = new StreamReader(input_file))
                {
                    while (!sr.EndOfStream)
                    {
                        var ts = sr.ReadLine();
                        var items = ts.Split(' ');
                        string chr1 = items[0][items[0].LastIndexOf("chr")..] + ":" + items[3];
                        string chr2 = items[6][items[6].LastIndexOf("chr")..] + ":" + items[9];

                        string key = "";
                        if (string.Compare(chr1, chr2) < 0)
                        {
                            key = chr1 + "-" + chr2;
                        }
                        else
                        {
                            key = chr2 + "-" + chr1;
                        }

                        if (line_count.ContainsKey(key))
                        {
                            line_count[key]++;
                        }
                        else
                        {
                            line_count.Add(key, 1);
                            line_store.Add(key, ts);
                        }

                    }

                    Console.WriteLine($"Total lines: {line_count.Count}...");
                }

                int cSize = line_count.Count;
                int percent = -1;

                Console.WriteLine("step 2: randmizing and sorting line based on contacts ...");

                var line_list = line_count.ToList();
                List<KeyValuePair<double, KeyValuePair<string, int>>> pairs = new List<KeyValuePair<double, KeyValuePair<string, int>>>();
                Random r = new Random();
                foreach (var item in line_list)
                {
                    pairs.Add(new KeyValuePair<double, KeyValuePair<string, int>>(r.NextDouble(), item));
                }
                pairs = pairs.OrderBy(a => a.Key).ToList();
                line_list = new List<KeyValuePair<string, int>>();
                foreach (var item in pairs)
                {
                    line_list.Add(item.Value);
                }
                line_list = line_list.OrderByDescending(a => a.Value).ToList();

                Console.WriteLine("step 3: processing contacts and outputing ...");


                using (StreamWriter sw = new StreamWriter(output_file))
                {
                    for (int i = 0; i < line_list.Count; i++)
                    {
                        if (i * 100 / cSize > percent)
                        {
                            percent = i * 100 / cSize;
                            Console.WriteLine(percent + "% contacts processed... " + DateTime.Now);
                        }


                        var chrs = line_list[i].Key.Split('-');
                        var chr1 = chrs[0];
                        var chr2 = chrs[1];

                        if (!included_pos.Contains(chr1) && !included_pos.Contains(chr2))
                        {
                            sw.WriteLine(line_store[line_list[i].Key]);
                            included_pos.Add(chr1);
                            included_pos.Add(chr2);
                        }
                    }
                }

            }
            var end = DateTime.Now;

            Console.WriteLine("Completed transformation. Total time consumption: " + (end - start).TotalSeconds + " seconds!");

        }

        private static void help()
        {
            Console.WriteLine("\tUsage: [1.Input file] [2.output file] [3.threshold] [4.method]");
            Console.WriteLine("\tMethod: 0 -> PointMax, 1 -> EdgeMax, 2 -> TotalRandom");
        }

        enum Method
        {
            PointMax,
            EdgeMax,
            TotalRandom
        }


        private static void processingContacts(Dictionary<string, contact> contacts_count, StreamWriter sw, HashSet<string> left_positions, List<HashSet<string>> sorted_hashset)
        {
            //Console.WriteLine("Each step time usage: ");
            //var s = DateTime.Now;

            var pos1 = getMaxContactSeg(sorted_hashset);

            //var e = DateTime.Now;
            //Console.WriteLine((e - s).TotalMilliseconds + " Milliseconds used for getMax!");

            //var pos1 = samplingPosition(max_pos);

            //s = e;
            //e = DateTime.Now;
            //Console.WriteLine((e - s).TotalMilliseconds + " Milliseconds used for samplingPosition!");


            var line = samplingContact(contacts_count[pos1], left_positions);

            //s = e;
            //e = DateTime.Now;
            //Console.WriteLine((e - s).TotalMilliseconds + " Milliseconds used for samplingContact!");


            //means this is not dangling position -> remove its contact and write
            if (line != "NO_CONTACTS")
            {
                sw.WriteLine(line);

                var items = line.Split(' ');
                string pos2a = (items[0].LastIndexOf("chr") > -1 ? items[0].Substring(items[0].LastIndexOf("chr")) : items[0]) + ":" + items[3];
                string pos2b = (items[6].LastIndexOf("chr") > -1 ? items[6].Substring(items[6].LastIndexOf("chr")) : items[6]) + ":" + items[9];
                string pos2 = "";

                if(pos2a == pos2b)
                {
                    pos2 = pos2a;
                }
                else if (pos1 == pos2a)
                {
                    pos2 = pos2b;
                }else if (pos1 == pos2b)
                {
                    pos2 = pos2a;
                }

                left_positions.Remove(pos2);
                for (int i = 0; i < sorted_hashset.Count; i++)
                {
                    if (sorted_hashset[i].Contains(pos2))
                    {
                        sorted_hashset[i].Remove(pos2);
                        if (sorted_hashset[i].Count == 0) sorted_hashset.RemoveAt(i);
                        break;
                    }
                }

                //contacts_count.Remove(pos2);

                //s = e;
                //e = DateTime.Now;
                //Console.WriteLine((e - s).TotalMilliseconds + " Milliseconds used for remove pos2!");

            }
            left_positions.Remove(pos1);
            if (sorted_hashset.Count > 0)
            {
                sorted_hashset[0].Remove(pos1);
                if (sorted_hashset[0].Count == 0) sorted_hashset.RemoveAt(0);
            }
            //contacts_count.Remove(pos1);

            //s = e;
            //e = DateTime.Now;
            //Console.WriteLine((e - s).TotalMilliseconds + " Milliseconds used for remove pos1!");
            //Console.WriteLine("Benchmark round completed");

        }


        private static string getMaxContactSeg(List<HashSet<string>> sorted_hashset)
        {
            Random r = new Random();

            return sorted_hashset[0].ElementAt(r.Next(sorted_hashset[0].Count));
        }
        private static string samplingPosition(List<string> pos)
        {
            if(pos.Count == 1)
            {
                return pos[0];
            }else if (pos.Count < 1)
            {
                return "";
            }
            else
            {
                Random r = new Random();
                return pos[r.Next(pos.Count)];
            }
        }
        private static string samplingContact(contact contact, HashSet<string> pos)
        {
            //clean lines for exsiting
            for (int i = contact.TotalLines.Count - 1; i > -1; i--)
            {
                var items = contact.TotalLines[i].Split(' ');
                string pos2 = (items[6].LastIndexOf("chr") > -1 ? items[6].Substring(items[6].LastIndexOf("chr")) : items[6]) + ":" + items[9];

                if (!pos.Contains(pos2))
                {
                    contact.TotalLines.RemoveAt(i);
                }
            }

            if (contact.TotalLines.Count == 0)
            {
                return "NO_CONTACTS";
            }else if(contact.TotalLines.Count == 1)
            {
                return contact.TotalLines[0];
            }
            else
            {
                Random r = new Random();
                return contact.TotalLines[r.Next(contact.TotalLines.Count)];
            }
        }

    }
}
