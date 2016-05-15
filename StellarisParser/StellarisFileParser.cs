//#define DEBUG_SHOW

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.IO;

namespace StellarisParser
{
    class StellarisFileParser
    {
        List<StellarisData> Data { get; } = new List<StellarisData>();
        List<StellarisDefine> Defines { get; } = new List<StellarisDefine>();

        protected static string _nameBasePattern = @"(?<name>[a-zA-Z0-9_\.""]+)";
        protected static string _numericBasePattern = @"(?<value>(\-|\+)?[0-9]+(\.[0-9]*)?)";
        protected static string _textBasePattern = @"(?<value>[a-zA-Z0-9_\.""]+)";
        protected static string _restBasePattern = @"\s*(?<rest>[^#]*)\s*(#.*)?$";

        protected static Regex _emptyPattern = new Regex(@"^\s*(#.*)?$");

        protected static Regex _definePattern = new Regex(@"^\s*@(?<name>[a-zA-Z0-9_]+)\s*=\s*" + _numericBasePattern + @"\s*(#.*)?$");

        protected static Regex _numericDataPattern = new Regex(@"^\s*" + _nameBasePattern + @"\s*(?<operator>=|<|>|<=|>=|<>)\s*" + _numericBasePattern + _restBasePattern);
        protected static Regex _textDataPattern = new Regex(@"^\s*" + _nameBasePattern + @"\s*=\s*" + _textBasePattern + _restBasePattern);
        protected static Regex _complexDataPattern = new Regex(@"^\s*" + _nameBasePattern + @"\s*=\s*\{\s*" + _restBasePattern);
        protected static Regex _complexDataEndPattern = new Regex(@"^\s*\}\s*" + _restBasePattern);

        protected static Regex _numericOnlyPattern = new Regex(@"^\s*" + _numericBasePattern + _restBasePattern);
        protected static Regex _textOnlyPattern = new Regex(@"^\s*" + _textBasePattern + _restBasePattern);

        protected string CurrentFileName = null;

        protected bool ReadSimpleObject(ref string l, out StellarisData d)
        {
            d = null;

            var m = _numericDataPattern.Match(l);
            if (m.Success)
            {
                d = new StellarisData(CurrentFileName, m.Groups["name"].Value, decimal.Parse(m.Groups["value"].Value), EnumEx.GetValueFromDescription<ComparisonOperator>(m.Groups["operator"].Value));
                l = m.Groups["rest"]?.Value;
                return true;
            }

            m = _textDataPattern.Match(l);
            if (m.Success)
            {
                d = new StellarisData(CurrentFileName, m.Groups["name"].Value, m.Groups["value"].Value);
                l = m.Groups["rest"]?.Value;
                return true;
            }

            m = _numericOnlyPattern.Match(l);
            if (m.Success)
            {
                d = new StellarisData(CurrentFileName, null, decimal.Parse(m.Groups["value"].Value), ComparisonOperator.EQ);
                l = m.Groups["rest"]?.Value;
                return true;
            }

            m = _textOnlyPattern.Match(l);
            if (m.Success)
            {
                d = new StellarisData(CurrentFileName, null, m.Groups["value"].Value);
                l = m.Groups["rest"]?.Value;
                return true;
            }

            return false;
        }

        protected void ReadComplexObject(ref string l, StreamReader sr, int level, List<StellarisData> context)
        {
            while (l != null)
            {
                // First immediately ignore empty lines
                var m = _emptyPattern.Match(l);
                if (m.Success)
                {
                    l = sr.ReadLine();
#if DEBUG_SHOW
                    System.Diagnostics.Debug.WriteLine("(" + level + ")[empty] " + l);
#endif
                    continue;
                }

                // Determine if the current line is a "complex" object
                m = _complexDataPattern.Match(l);
                if (m.Success)
                {
                    // If it is, recurse into it
                    var d = new StellarisData(CurrentFileName, m.Groups["name"].Value, true);
                    string rest = m.Groups["rest"]?.Value;

                    if (rest == null || rest.Length == 0)
                        rest = sr.ReadLine();

                    l = rest;
#if DEBUG_SHOW
                    System.Diagnostics.Debug.WriteLine("(" + level + ")[rest] " + l);
#endif
                    ReadComplexObject(ref l, sr, level + 1, d.SubValues);
#if DEBUG_SHOW
                    System.Diagnostics.Debug.WriteLine("(" + level + ")[complexend] " + l);
#endif

                    context.Add(d);
                }
                else
                {
                    // If it's not a "complex" object, it could be a closing bracket
                    m = _complexDataEndPattern.Match(l);
                    if (m.Success)
                    {
                        l = m.Groups["rest"]?.Value;
#if DEBUG_SHOW
                        System.Diagnostics.Debug.WriteLine("(" + level + ")[end] " + l);
#endif
                        return;
                    }

                    // Otherwise, check for a define
                    if (level == 0)
                    {
                        // Defines only exist at the root of the document
                        m = _definePattern.Match(l);
                        if (m.Success)
                        {
                            Defines.Add(new StellarisDefine(CurrentFileName, m.Groups["name"].Value, decimal.Parse(m.Groups["value"].Value)));
                            l = sr.ReadLine();
#if DEBUG_SHOW
                            System.Diagnostics.Debug.WriteLine("(" + level + ")[define] " + l);
#endif
                            continue;
                        }
                    }

                    // Otherwise we should be looking at a "simple" object
                    StellarisData d;
                    if (ReadSimpleObject(ref l, out d))
                    {
                        context.Add(d);

                        if (l == null || l.Length == 0)
                            l = sr.ReadLine();
#if DEBUG_SHOW
                        System.Diagnostics.Debug.WriteLine("(" + level + ")[simple] " + l);
#endif
                    }
                    else
                    {
                        l = sr.ReadLine();
#if DEBUG_SHOW
                        System.Diagnostics.Debug.WriteLine("(" + level + ")[none] " + l);
#endif
                    }
                }
            }
        }

        public void Load(string filename)
        {
            CurrentFileName = filename;
            StreamReader sr = new StreamReader(filename);
            string l = sr.ReadLine();
            ReadComplexObject(ref l, sr, 0, Data);
        }

        public void Merge()
        {
            for(int i = 1; i < Data.Count; i++)
            {
                for(int j = i - 1; j >= 0; j--)
                {
                    if(Path.GetDirectoryName(Data[i].FileName) == Path.GetDirectoryName(Data[j].FileName))
                    {
                        if(Data[i].SubValues != null)
                        {
                            Data[j].InitSubValues();
                            foreach (var sub in Data[i].SubValues)
                                Data[j].SubValues.Add(sub);
                        }

                        Data.RemoveAt(i);
                        i--;
                        break;
                    }
                }
            }

            foreach(var d in Data)
                if(d.SubValues != null && d.SubValues.Count > 0) SubMerge(d.SubValues);
        }

        protected void SubMerge(List<StellarisData> context)
        {
            for (int i = 1; i < context.Count; i++)
            {
                for (int j = i - 1; j >= 0; j--)
                {
                    if (context[i].Name == context[j].Name)
                    {
                        if (context[i].SubValues != null)
                        {
                            context[j].InitSubValues();
                            foreach (var sub in context[i].SubValues)
                                context[j].SubValues.Add(sub);
                        }

                        context.RemoveAt(i);
                        i--;
                        break;
                    }
                }
            }

            foreach (var d in context)
                if (d.SubValues != null && d.SubValues.Count > 0) SubMerge(d.SubValues);
        }

        public void Write(string path)
        {
            StreamWriter sw = new StreamWriter(path);
            foreach (var d in Data)
                sw.WriteLine(d);
        }
    }
}
