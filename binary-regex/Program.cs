using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Runtime.Remoting.Metadata.W3cXsd2001;
using System.IO;
using System.Text.RegularExpressions;

//Reference: http://stackoverflow.com/questions/12962239/using-c-sharp-to-edit-text-within-a-binary-file

/*
    ToDo: Add in command line arguments

*/


namespace binary_regex
{
    class Program
    {
        public static void Main(string[] args)
        {
            //declare the arrays with the regex rules
            string[] astrPattern = { };
            string[] astrReplace = { };

            //check for the regex rules file
            string strRulesPath = Directory.GetCurrentDirectory() + @"\rules.txt";
            if (!File.Exists(strRulesPath))
            {
                Console.WriteLine("Could not find: " + strRulesPath);
                Console.WriteLine("Program will now exit.");
                Console.ReadKey();
                return;
            }
            else
            {
                ProcessRulesFile(strRulesPath, ref astrPattern, ref astrReplace);
            }

            //check that at least one file or path was passed in args
            int intFilePathArguments = 0;
            foreach (string path in args)
            {
                if (File.Exists(path))
                {
                    intFilePathArguments++;
                }
                else if (Directory.Exists(path))
                {
                    intFilePathArguments++;
                }
            }
            if(intFilePathArguments < 1)
            {
                Console.WriteLine("At least one command line argument is required.");
                Console.WriteLine("Usage: binary-regex.exe <path-to-file> [<path-to-file>]");
                Console.WriteLine("Usage: binary-regex.exe <path-to-directory> [<path-to-directory>]");
                Console.WriteLine("Program will now exit.");
                Console.ReadKey();
                return;
            }


            //check for options in args
            bool blVerbose = false;
            bool blNoRename = false;
            bool blModifiedFilesOnly = false;
            bool blNoLogFile = false;
            bool blFileNameMask = false;
            bool blExtensionMask = false;
            bool blCustomOutputPath = false;
            string strFileNameMask = "";
            string strExtensionMask = "";
            string strCustomOutputPath = "";
            foreach (string arg in args)
            {
                
                switch (arg.ToLower().Substring(0,2))
                {
                    case "-v":
                        //verbose mode
                        Console.WriteLine("verbose mode");
                        blVerbose = true;
                        break;

                    case "-r":
                        //no rename
                        Console.WriteLine("no rename");
                        blNoRename = true;
                        break;

                    case "-m":
                        //only copy modified files
                        Console.WriteLine("only copy modified files");
                        blModifiedFilesOnly = true;
                        break;

                    case "-l":
                        //don't create log file
                        Console.WriteLine("don't create log file");
                        blNoLogFile = true;
                        break;

                    case "-f":
                        //filename mask
                        try
                        {
                            if (arg.Substring(2, 1) == "=")
                            {
                                blFileNameMask = true;
                                strFileNameMask = arg.Substring(3, arg.Length - 3);
                                Console.WriteLine("Using filename mask: " + strFileNameMask);
                            }
                            else
                            {
                                throw new ArgumentException();
                            }
                        }
                        catch
                        {
                            Console.WriteLine("Invalid filename mask.");
                            Console.WriteLine("Usage: -f=myfilename");
                            Console.WriteLine("Program will now exit.");
                            Console.ReadKey();
                            return;
                        }
                        break;

                    case "-e":
                        //extension mask
                        try
                        {
                            if (arg.Substring(2, 1) == "=")
                            {
                                blExtensionMask = true;
                                strExtensionMask = arg.Substring(3, arg.Length - 3);
                                Console.WriteLine("Using extension mask: " + strExtensionMask);
                            }
                            else
                            {
                                throw new ArgumentException();
                            }
                        }
                        catch
                        {
                            Console.WriteLine("Invalid extension mask.");
                            Console.WriteLine("Usage: -e=ext");
                            Console.WriteLine("Program will now exit.");
                            Console.ReadKey();
                            return;
                        }
                        break;

                    case "-o":
                        //output path

                        try
                        {
                            if (arg.Substring(2, 1) == "=")
                            {
                                strCustomOutputPath = arg.Substring(3, arg.Length - 3);
                                if(Directory.Exists(strCustomOutputPath))
                                {
                                    blCustomOutputPath = true;
                                    Console.WriteLine("Using ouput path: " + strCustomOutputPath);
                                }
                                else
                                {
                                    throw new ArgumentException();
                                }
                            }
                            else
                            {
                                throw new ArgumentException();
                            }
                        }
                        catch
                        {
                            Console.WriteLine("Invalid ouput path.");
                            Console.WriteLine(@"Usage: -o=C:\path\to\files");
                            Console.WriteLine("Program will now exit.");
                            Console.ReadKey();
                            return;
                        }
                        break;

                    default:
                        break;
                }
            }

            string strRootPath = "";
            string strOutputPath = "";
            string strReplaceDir = @"\regex";





            //check for the input file/directory
            foreach (string path in args)
            {
                if (File.Exists(path))
                {
                    // This path is a file
                    strRootPath = Directory.GetParent(path).FullName;
                    if (Directory.Exists(strRootPath + strReplaceDir))
                    {
                        Console.WriteLine(strRootPath + strReplaceDir + " already exists.");
                        Console.WriteLine("If you continue, the contents of this directory will be deleted.");
                        Console.WriteLine("Are you sure you want to continue? (y) (n)");
                        ConsoleKeyInfo ckiInput = Console.ReadKey();
                        if (ckiInput.Key.ToString().ToUpper() == "Y")
                        {
                            DeleteDirectory(strRootPath + strReplaceDir);
                            Console.Clear();
                        }
                        else
                        {
                            //exit the application
                            return;
                        }
                    }
                    Directory.CreateDirectory(strRootPath + strReplaceDir);
                    ProcessFile(strRootPath, strReplaceDir, path, astrPattern, astrReplace);
                }
                else if (Directory.Exists(path))
                {
                    // This path is a directory
                    strRootPath = Directory.GetParent(path).FullName;
                    if (Directory.Exists(strRootPath + strReplaceDir))
                    {
                        Console.WriteLine(strRootPath + strReplaceDir + " already exists.");
                        Console.WriteLine("If you continue, the contents of this directory will be deleted.");
                        Console.WriteLine("Are you sure you want to continue? (y) (n)");
                        ConsoleKeyInfo ckiInput = Console.ReadKey();
                        if (ckiInput.Key.ToString().ToUpper() == "Y")
                        {
                            DeleteDirectory(strRootPath + strReplaceDir);
                        }
                        else
                        {
                            //exit the application
                            return;
                        }

                    }
                    Directory.CreateDirectory(strRootPath + strReplaceDir);
                    ProcessDirectory(strRootPath, strReplaceDir, path, astrPattern, astrReplace);
                }
            }
        }


        public static void ProcessDirectory(string strRootPath, string strReplaceDir, string targetDirectory, string[] astrPattern, string[] astrReplace)
        {
            // Process the list of files found in the directory.
            string[] fileEntries = Directory.GetFiles(targetDirectory);
            foreach (string fileName in fileEntries)
                ProcessFile(strRootPath, strReplaceDir, fileName, astrPattern, astrReplace);

            // Recurse into subdirectories of this directory.
            string[] subdirectoryEntries = Directory.GetDirectories(targetDirectory);
            foreach (string subdirectory in subdirectoryEntries)
                ProcessDirectory(strRootPath, strReplaceDir, subdirectory, astrPattern, astrReplace);
        }

        public static void ProcessFile(string strRootPath, string strReplaceDir, string strInputFile, string[] astrPattern, string[] astrReplace)
        {

            string strOutputFile = "";
            Console.WriteLine("Running replace on: " + Path.GetFileNameWithoutExtension(strInputFile));
            string strTemp = "";
            string strInputString = "";
            string last = astrPattern.Last();

            int intLoopStartPos = 0;
            int intReplace = 0;
            bool strReplaced = false;

            List<byte> lbyteWorking = new List<byte>();
            Encoding ascii = Encoding.ASCII;

            byte[] byteInputString = { };


            //run the regex over the filename
            string strTempOutFileName = Path.GetFileNameWithoutExtension(strInputFile);
            intReplace = 0;
            foreach (string strPattern in astrPattern)
            {
                Regex regexOutFileName = new Regex(strPattern);
                strTempOutFileName = regexOutFileName.Replace(Path.GetFileNameWithoutExtension(strTempOutFileName), astrReplace[intReplace]);
                intReplace++;
            }
            string strTempOutPath = Path.GetDirectoryName(strInputFile).Replace(strRootPath, strRootPath + strReplaceDir);
            //run the regex over the directory
            intReplace = 0;
            foreach (string strPattern in astrPattern)
            {
                Regex regexOutFileName = new Regex(strPattern);
                strTempOutPath = regexOutFileName.Replace(strTempOutPath, astrReplace[intReplace]);
                intReplace++;
            }
            if (!Directory.Exists(strTempOutPath))
            {
                Directory.CreateDirectory(strTempOutPath);
            }
            strOutputFile = strTempOutPath + @"\" + strTempOutFileName + Path.GetExtension(strInputFile);

            //run the regex over the input file
            intReplace = 0;
            byteInputString = File.ReadAllBytes(strInputFile);
            strInputString = ascii.GetString(byteInputString);
            foreach (string strPattern in astrPattern)
            {
                Regex regexInputString = new Regex(strPattern);
                Regex regexSubstring = new Regex(strPattern);
                Match matchInputString = regexInputString.Match(strInputString);
                intLoopStartPos = 0;
                //Console.WriteLine("     Now searching for: " + strPattern);

                while (matchInputString.Success)
                {
                    strReplaced = true;
                    for (int i = intLoopStartPos; i < matchInputString.Index; i++)
                        lbyteWorking.Add(byteInputString[i]);               //pipe the irrelevant bytes to file until we reach a match


                    strTemp = strInputString.Substring(matchInputString.Index, matchInputString.Length);
                    strTemp = regexSubstring.Replace(strTemp, astrReplace[intReplace]);         //run the regex over a substring of the input string

                    foreach (char c in strTemp)
                        lbyteWorking.Add(Convert.ToByte(c));                //convert that substring back to bytes and add it to the array
                    Match nmatch = matchInputString.NextMatch();            //move on to the next match
                    int end = (nmatch.Success) ? nmatch.Index : byteInputString.Length;
                    for (int i = matchInputString.Index + matchInputString.Length; i < end; i++)
                        lbyteWorking.Add(byteInputString[i]);
                    intLoopStartPos = end;
                    matchInputString = nmatch;
                }

                intReplace++;                                               //increment so to use the next replace pattern in the array

                
                if (strPattern.Equals(last) == false)
                {
                    if (lbyteWorking.Count > 0)
                    {
                        byteInputString = lbyteWorking.ToArray();                //copy the <List> to the [array] so we can run expressions over it again
                    }
                    lbyteWorking.Clear();
                }

            }
            
            if (strReplaced)
            {
                File.WriteAllBytes(strOutputFile, byteInputString);
                Console.WriteLine(Path.GetFileNameWithoutExtension(strInputFile) + " was updated");
            }
            else
            {
                File.WriteAllBytes(strOutputFile, byteInputString);
                Console.WriteLine(Path.GetFileNameWithoutExtension(strInputFile) + " was not modified");
            }
        }

        public static void ProcessRulesFile(string strFilePath, ref string[] astrPattern, ref string[] astrReplace)
        {
            string line;

            List<string> lstrColumnOne = new List<string>();
            List<string> lstrColumnTwo = new List<string>();

            System.IO.StreamReader file = new System.IO.StreamReader(strFilePath);
            while ((line = file.ReadLine()) != null)
                try
                {
                    String[] astrColumnsFromFile = line.Trim().Split('\t');

                    lstrColumnOne.Add(astrColumnsFromFile[0]);
                    lstrColumnTwo.Add(astrColumnsFromFile[1]);
                }
                catch
                {
                    Console.WriteLine("Something went wrong with the rules.txt file.");
                    Console.WriteLine("Might be my fault...probably yours.");
                    Console.WriteLine("Ensure the file matches this pattern: <pattern><tab><replace>.");
                }

            astrPattern = lstrColumnOne.ToArray();
            astrReplace = lstrColumnTwo.ToArray();
        }

        public static void DeleteDirectory(string strPath)
        {
            string[] astrFiles = Directory.GetFiles(strPath);
            string[] astrDirs = Directory.GetDirectories(strPath);

            foreach (string file in astrFiles)
            {
                File.SetAttributes(file, FileAttributes.Normal);
                File.Delete(file);
            }

            foreach (string dir in astrDirs)
            {
                DeleteDirectory(dir);
            }

            Directory.Delete(strPath, false);
        }
    }

}



