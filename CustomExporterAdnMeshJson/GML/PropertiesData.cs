using Autodesk.Revit.DB;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Diagnostics;
using System.Linq;
using System.Xml.Linq;

namespace CustomExporterAdnMeshJson.GML
{
    public class PropertiesData
    {
        private readonly Parameter parameter;
        private readonly Document doc;
        public PropertiesData(string name, string value, object type)
        {
            Name = name;
            Value = value;
            DataType = type;
            UnitTypeString = string.Empty;

        }
        public PropertiesData(string name, string value, string type)
        {
            Name = name;
            Value = value;
            DataType = type; 
            UnitTypeString = string.Empty; 

        }
        public PropertiesData(Parameter parameter, Document doc = null)
        {
            this.doc = doc;
            this.parameter = parameter;
          
        }

        internal bool InitDataClass()
        {

            try
            {
                if (parameter == null)
                    throw new NullReferenceException("Parameter er null");
                Name = parameter.Definition.Name;
                UnitTypeString = string.Empty;
                switch (parameter.StorageType)
                {
                    case StorageType.ElementId:
                        var elementId = parameter.AsElementId();
                        if (elementId != ElementId.InvalidElementId && doc != null)
                        {
                            var connectDElemtn = doc.GetElement(elementId);
                            Value = connectDElemtn != null ? connectDElemtn.Name : elementId.IntegerValue.ToString();
                        }
                        else
                            Value = parameter.AsElementId().ToString();
                        DataType = typeof(ElementId);
                        break;
                    case StorageType.Integer:
                        Value = parameter.AsInteger().ToString();
                        DataType = typeof(int);
                        break;
                    case StorageType.String:
                        Value = parameter.AsString();
                        DataType = typeof(string);
                        break;
                    case StorageType.Double:
                        UnitTypeString = parameter.GetUnitTypeId()?.TypeId.Replace("autodesk.unit.unit:", "").Split('-').First();

                        var localvalue = parameter.AsDouble();
                        if (localvalue == 0)
                            Value = "0.0";
                        else
                        {
                            Value = UnitUtils.ConvertFromInternalUnits(parameter.AsDouble(), parameter.GetUnitTypeId())
                            .ToString().Replace(",", ".");
                        }
                        DataType = typeof(double);
                        break;
                }
                return true;
            }
            catch (Exception e)
            {
                Debug.Write(e.Message);
                Debug.Write(e.StackTrace);
                return false; 
            }
        }
        public string Name { get; private set; }
        public string Value { get; private set; }
        public object DataType { get; private set; }
        public string UnitTypeString { get; private set; }

        public override string ToString()
        {
            var jsondata = JsonConvert.SerializeObject(this);
            return jsondata;
        }
    }

}
