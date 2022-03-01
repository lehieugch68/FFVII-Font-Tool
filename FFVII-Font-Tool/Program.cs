using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace FFVII_Font_Tool
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.Title = "Final Fantasy VII Remake Tool (PC) by LeHieu - viethoagame.com";
            if (args.Length > 0)
            {
                string type = args[0].ToLower();
                if (type == "tex")
                {
                    string cmd = args[1].ToLower();
                    if (cmd == "-e")
                    {
                        if (args.Length < 4)
                        {
                            Helper();
                            return;
                        }
                        FontExtractor.ExtractDDS(args[2], args[3]);
                    } 
                    else if (cmd == "-i")
                    {
                        if (args.Length < 5)
                        {
                            Helper();
                            return;
                        }
                        FontExtractor.ImportDDS(args[2], args[3], args[4]);
                    }
                }
                else if (type == "glyph")
                {
                    string cmd = args[1].ToLower();
                    if (cmd == "-e")
                    {
                        if (args.Length < 4)
                        {
                            Helper();
                            return;
                        }
                        FontExtractor.ExtractGlyphs(args[2], args[3]);
                    }
                    else if (cmd == "-i")
                    {
                        if (args.Length < 5)
                        {
                            Helper();
                            return;
                        }
                        bool fnt = Array.Exists(args, arg => arg.ToLower() == "-fnt");
                        if (fnt)
                        {
                            short xadv = 0, page = -1;
                            string arg_xadv = Array.Find(args, arg => arg.ToLower().StartsWith("xadv="));
                            string arg_page = Array.Find(args, arg => arg.ToLower().StartsWith("page="));
                            if (arg_xadv != null) xadv = short.Parse(arg_xadv.Split((char)61)[1]);
                            if (arg_page != null) page = short.Parse(arg_page.Split((char)61)[1]);
                            FontExtractor.ImportGlyphs(args[2], args[3], args[4], fnt, page, xadv);
                        }
                        else FontExtractor.ImportGlyphs(args[2], args[3], args[4]);
                            
                    }
                } else if (type == "-help")
                {
                    Helper();
                }
            }
            else
            {
                Helper();
            }
        }
        static void Helper()
        {
            Console.WriteLine("Commands:");
            Console.WriteLine("\nTexture:");
            Console.WriteLine("- Extract: FFVII-Font-Tool.exe tex -e \"[Input File]\" \"[Output File]\"\n- Re-import: FFVII-Font-Tool.exe tex -i \"[Original File]\" \"[Input File]\" \"[Output File]\"");
            Console.WriteLine("\nGlyph:");
            Console.WriteLine("- Extract: FFVII-Font-Tool.exe glyph -e \"[Input File]\" \"[Output File]\"\n- Re-import: FFVII-Font-Tool.exe glyph -i \"[Original File]\" \"[Input File]\" \"[Output File]\"");
        }
    }
}
