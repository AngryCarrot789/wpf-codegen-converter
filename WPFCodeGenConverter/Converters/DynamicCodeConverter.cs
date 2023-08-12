using Microsoft.CSharp;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Markup;
using WPFCodeGenConverter.Utils;

namespace WPFCodeGenConverter.Converters
{
    public class DynamicCodeConverter : MarkupExtension, IValueConverter
    {
        private static readonly HashSet<string> UsedClassNames = new HashSet<string>();

        private string lastClassName;
        private string lastScript;
        private MethodInfo lastCompiledMethod;

        public DynamicCodeConverter()
        {

        }

        ~DynamicCodeConverter()
        {
            if (this.lastClassName != null)
            {
                lock (UsedClassNames)
                {
                    UsedClassNames.Remove(this.lastClassName);
                }
            }
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(parameter is string script))
                throw new Exception("Parameter must be a C# script");

            if (this.lastCompiledMethod != null && this.lastScript.Equals(script))
                return this.lastCompiledMethod.Invoke(null, new[] { value });

            if (string.IsNullOrWhiteSpace(script))
                throw new Exception("C# script must not be an empty string or consist of only whitespaces");

            this.GenerateMethod(script);
            return this.lastCompiledMethod.Invoke(null, new[] { value });
        }

        private void GenerateMethod(string script)
        {
            // hopefully by setting the method to null, it allows the previous class to be
            // garbage collected... hopefully. The parameter is only really gonna "change" once, which is the
            // first time the converter is run (realistically)
            if (this.lastClassName != null)
            {
                lock (UsedClassNames) {
                    UsedClassNames.Remove(this.lastClassName);
                }
            }

            this.lastCompiledMethod = null;
            this.lastScript = null;
            this.lastClassName = null;

            string klass;
            lock (UsedClassNames)
            {
                // generate a random unused class name
                klass = RandomUtils.RandomStringWhere("Script_", 16, x => !UsedClassNames.Contains(x));
                UsedClassNames.Add(klass);
            }

            const string targetNamespace = "FramePFX.DynamicScript";
            const string parameterName = "x";

            // feel free to modify this to your liking
            string[] code = {
                "using System;" +
                "namespace " + targetNamespace +
                "{" +
                "   public class " + klass +
                "   {" +
                "       static public object Execute(object " + parameterName + ")" +
                "       {" +
                "           return " + script + ";" + // ultra filthy
                "       }" +
                "   }" +
                "}"
            };

            CompilerParameters CompilerParams = new CompilerParameters
            {
                GenerateInMemory = true,
                TreatWarningsAsErrors = false,
                GenerateExecutable = false,
                CompilerOptions = "/optimize"
            };

            CompilerParams.ReferencedAssemblies.AddRange(new[] {
                "Microsoft.CSharp.dll",
                "System.dll",
                "System.Core.dll",
                "System.Data.dll",
                "System.Xaml.dll",
                "System.Xml.dll",

                // https://stackoverflow.com/questions/52346848/c-finding-path-to-presentationcore-dll-and-other-assemblies-in-the-gac
                // Cannot put the below DLL's raw file name here because the compiler has a
                // hard time locating them. So instead, find the full file path through types
                // that are known to be defined in the assemblies

                // PresentationCore.dll
                typeof(Visibility).Assembly.Location,

                // PresentationFramework.dll
                typeof(Button).Assembly.Location,                
            });

            CSharpCodeProvider provider = new CSharpCodeProvider();
            CompilerResults compile = provider.CompileAssemblyFromSource(CompilerParams, code);

            if (compile.Errors.HasErrors)
            {
                string text = "Error during compilation: ";
                foreach (CompilerError ce in compile.Errors)
                    text += "\n" + ce;
                throw new Exception(text);
            }

            Module module = compile.CompiledAssembly.GetModules()[0];
            if (module == null)
                throw new Exception("Error during compilation; could not find compiled module");

            Type type = module.GetType(targetNamespace + "." + klass);
            if (type == null)
                throw new Exception("Error during compilation; could not find compiled class");

            MethodInfo mdInfo = type.GetMethod("Execute");
            if (mdInfo == null)
                throw new Exception("Error during compilation; could not find execute function in compiled class");

            this.lastClassName = klass;
            this.lastCompiledMethod = mdInfo;
            this.lastScript = script;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException("Converting back from C# script is not supported");
        }

        public override object ProvideValue(IServiceProvider serviceProvider) => new DynamicCodeConverter();
    }
}
