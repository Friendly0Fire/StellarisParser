using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace StellarisParser
{
    enum ComparisonOperator
    {
        [Description("")]
        NOT_SET = 0,
        [Description("=")]
        EQ = 2,
        [Description("<")]
        LT = 3,
        [Description(">")]
        GT = 4,
        [Description("<=")]
        LTE = 5,
        [Description(">=")]
        GTE = 6,
        [Description("<>")]
        NEQ = 7
    }

    public static class EnumEx
    {
        public static T GetValueFromDescription<T>(string description)
        {
            var type = typeof(T);
            if (!type.IsEnum) throw new InvalidOperationException();
            foreach (var field in type.GetFields())
            {
                var attribute = Attribute.GetCustomAttribute(field,
                    typeof(DescriptionAttribute)) as DescriptionAttribute;
                if (attribute != null)
                {
                    if (attribute.Description == description)
                        return (T)field.GetValue(null);
                }
                else
                {
                    if (field.Name == description)
                        return (T)field.GetValue(null);
                }
            }
            throw new ArgumentException("Not found.", "description");
        }

        public static string GetEnumDescription(Enum value)
        {
            FieldInfo fi = value.GetType().GetField(value.ToString());

            DescriptionAttribute[] attributes =
                (DescriptionAttribute[])fi.GetCustomAttributes(
                typeof(DescriptionAttribute),
                false);

            if (attributes != null &&
                attributes.Length > 0)
                return attributes[0].Description;
            else
                return value.ToString();
        }
    }

    class StellarisData
    {
        public decimal NumericalValue { get; protected set; } = 0;
        public ComparisonOperator NumericalOperator { get; protected set; } = ComparisonOperator.NOT_SET;
        public string TextValue { get; protected set; } = null;
        public List<StellarisData> SubValues { get; protected set; } = null;
        public string FileName { get; protected set; }

        public string Name { get; set; } = null;

        public StellarisData(string fn, string n, decimal v, ComparisonOperator o = ComparisonOperator.EQ)
        {
            FileName = fn;
            Name = n;
            NumericalValue = v;
            NumericalOperator = o;
        }

        public StellarisData(string fn, string n, string v)
        {
            FileName = fn;
            Name = n;
            TextValue = v;
        }

        public StellarisData(string fn, string n, bool subvalues = false)
        {
            FileName = fn;
            Name = n;
            if(subvalues)
                SubValues = new List<StellarisData>();
        }

        public void InitSubValues()
        {
            if (SubValues == null)
                SubValues = new List<StellarisData>();
        }

        public override string ToString()
        {
            string o = "";
            if (Name != null)
                o += Name;

            if (NumericalOperator != ComparisonOperator.NOT_SET)
                o += " " + EnumEx.GetEnumDescription(NumericalOperator) + " " + NumericalValue;
            else if (TextValue != null)
                o += " = " + TextValue;
            else if(SubValues != null)
            {
                o += " = {" + Environment.NewLine;
                foreach (var d in SubValues)
                    o += "\t" + d.ToString().Replace("\n", "\n\t") + Environment.NewLine;
                o += "}" + Environment.NewLine;
            }

            return o;
        }
    }

    class StellarisDefine
    {
        public string Name { get; protected set; }
        public decimal Value { get; protected set; }
        public string FileName { get; protected set; }

        public StellarisDefine(string fn, string n, decimal v)
        {
            FileName = fn;
            Name = n;
            Value = v;
        }
    }
}
