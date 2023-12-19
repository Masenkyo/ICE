using Microsoft.CSharp;
using System;
using System.CodeDom.Compiler;
using System.Reflection;
using System.Text;
using UnityEngine;

public class ice : MonoBehaviour
{ 
    void Start()
    { 
        Run(@"
         using UnityEngine;
         public class Test
         {
           public static void Foo()
           {
              Debug.Log(""Hello, World!"");
           }
         }"
        );
    }
    
    void Run(string code) 
    {
        Assembly assembly = Compile(code);
       
        var method = assembly.GetType("Test").GetMethod("Foo");
        var del = (Action)Delegate.CreateDelegate(typeof(Action), method);
        del.Invoke(); 
    }
    
    public static Assembly Compile(string source)
    {
      var provider = new CSharpCodeProvider();
      var param = new CompilerParameters();


     bool bruh = true;
     // Add ALL of the assembly references
     foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies()) {

       if (bruh)
       {
         bruh = false;
         continue;
       }
       
       param.ReferencedAssemblies.Add(assembly.Location);
     }
 
     // Add specific assembly references
     //param.ReferencedAssemblies.Add("System.dll");
     //param.ReferencedAssemblies.Add("CSharp.dll");
     //param.ReferencedAssemblies.Add("UnityEngines.dll");
 
     // Generate a dll in memory
     param.GenerateExecutable = false;
     param.GenerateInMemory = true;
     
     // Compile the source
     var result = provider.CompileAssemblyFromSource(param, source);
     
     if (result.Errors.Count > 0) {
       var msg = new StringBuilder();
       foreach (CompilerError error in result.Errors) {
         msg.AppendFormat("Error ({0}): {1}\n",
         error.ErrorNumber, error.ErrorText);
       }
       throw new Exception(msg.ToString());
     }
 
     // Return the assembly
     return result.CompiledAssembly;
   }
}