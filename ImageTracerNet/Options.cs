using System;
using System.Linq;
using System.Reflection;
using ImageTracerNet.OptionTypes;

namespace ImageTracerNet
{
    [Serializable]
    public class Options
    {
        public Tracing Tracing { get; set; } = new Tracing();
        public ColorQuantization ColorQuantization { get; set; } = new ColorQuantization();
        public SvgRendering SvgRendering { get; set; } = new SvgRendering();
        public Blur Blur { get; set; } = new Blur();

        public void SetOptionByName(string optionName, object value)
        {
            var optionType = GetOptionTypeFromName(optionName);
            var property = optionType.GetType()
                .GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Single(i => String.Equals(i.Name, optionName, StringComparison.CurrentCultureIgnoreCase));
            
            property.SetValue(optionType, Convert.ChangeType(value, property.PropertyType));
        }

        private object GetOptionTypeFromName(string optionName)
        {
            object[] options = {Tracing, ColorQuantization, SvgRendering, Blur};
            return options.Single(o => o.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly).Any(i => String.Equals(i.Name, optionName, StringComparison.CurrentCultureIgnoreCase)));
        }
    }
}
