using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace XamlFormatter
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("--- Rees.biz XAML/XML Formatter ---");

            var formatter = new XamlXmlFormatter();
            int count = 0, total = 0, exceptions = 0, readonlyExceptions = 0;

            foreach (string item in args)
            {
                foreach (string fileName in GetFileNames(item))
                {
                    total++;
                    Console.WriteLine(fileName);
                    try
                    {
                        formatter.Format(fileName, fileName);
                        count++;
                        if (formatter.UnusedNames.Count > 0)
                        {
                            Console.WriteLine("Possibly unused x:Name's:");
                            foreach (string name in formatter.UnusedNames)
                            {
                                Console.WriteLine("     {0}", name);
                            }

                            Console.WriteLine("     {0} Names listed.", formatter.UnusedNames.Count);
                        }

                        if (formatter.UnusedKeys.Count > 0)
                        {
                            Console.WriteLine("Possibly unused x:Key's:");
                            foreach (string name in formatter.UnusedKeys)
                            {
                                Console.WriteLine("     {0}", name);
                            }

                            Console.WriteLine("     {0} Keys listed.", formatter.UnusedKeys.Count);
                        }

                        if (formatter.UnusedNamespaces.Count > 0)
                        {
                            Console.WriteLine("Unused Namespaces:");
                            foreach (string name in formatter.UnusedNamespaces)
                            {
                                Console.WriteLine("     {0}", name);
                            }

                            Console.Write("     {0} Namespaces listed.", formatter.UnusedNamespaces.Count);
                        }
                    }
                    catch (UnauthorizedAccessException ex)
                    {
                        readonlyExceptions++;
                        exceptions++;
                        Console.WriteLine(ex.ToString());
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.ToString());
                        exceptions++;
                    }

                    Console.WriteLine(" ");
                }
            }

            Console.WriteLine("Finished: {0} files of {1} total.", count, total);
            if (exceptions > 0)
            {
                Console.WriteLine("{0} EXCEPTIONS OCCURED", exceptions);
            }

            if (readonlyExceptions > 0)
            {
                Console.WriteLine("{0} Destination files throw readonly exceptions. Make sure you check the files out first.", readonlyExceptions);
            }

            try
            {
                Console.ReadKey();
            }
            catch
            {
            }
        }

        private static IEnumerable<string> GetFileNames(string item)
        {
            var retVal = new List<string>();
            if (string.IsNullOrEmpty(item))
            {
                return retVal;
            }

            if (item.EndsWith(".xaml", true, CultureInfo.CurrentCulture))
            {
                retVal.Add(item);
            }
            else
            {
                GetFiles(item, retVal);
            }

            return retVal;
        }

        private static void GetFiles(string folder, ICollection<string> list)
        {
            foreach (string fileName in Directory.GetFiles(folder, "*.xaml"))
            {
                list.Add(fileName);
            }

            foreach (string subfolder in Directory.GetDirectories(folder))
            {
                GetFiles(subfolder, list);
            }
        }
    }
}