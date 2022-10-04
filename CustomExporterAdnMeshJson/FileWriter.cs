using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Remoting.Contexts;
using System.Text;
using System.Threading.Tasks;

namespace CustomExporterAdnMeshJson
{
    public class FileWriter
    {
        internal static bool WriteFileAsJson(IEnumerable<AdnMeshData> meshData, string outputPath)
        {
            try
            {
                var dir = Path.GetDirectoryName(outputPath);
                var name = Path.GetFileNameWithoutExtension(outputPath);
                outputPath = Path.Combine(dir, $"{name}.json");
                
                using (StreamWriter s = new StreamWriter(outputPath))
                {
                    s.Write("[");
                    int i = 0;

                    foreach (AdnMeshData d in meshData)
                    {
                        if (0 < i) { s.Write(','); }

                        s.Write(d.ToJson());

                        ++i;
                    }
                    s.Write("\n]\n");
                    s.Close();
                }
                return true; 
            }
            catch (Exception e)
            {
                Debug.Write(e.Message);
                return false; 
            }
        }
        internal static bool WriteFileAsGml(string gmlData, string outputPath)
        {
            try
            {
                var dir = Path.GetDirectoryName(outputPath);
                var name = Path.GetFileNameWithoutExtension(outputPath);
                outputPath = Path.Combine(dir, $"{name}.gml");

                using (var writer = new StreamWriter(outputPath))
                {
                    writer.Write(gmlData);
                    writer.Close();
                }
                return true;
            }
            catch (Exception e)
            {
                Debug.Write(e.Message);
                return false;

            }
        }
    }
}
