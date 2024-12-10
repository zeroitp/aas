namespace AasxServerStandardBib.Models.Program;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Xml.Serialization;

/* Submodel Descriptor Definition */
public class SubmodelDescriptors
{
    [XmlElement(ElementName = "administration")]
    [JsonIgnore]
    //public AdminShell.Administration administration = null;
    public AdministrativeInformation administration = null;

    [XmlElement(ElementName = "description")]
    [JsonIgnore]
    //public AdminShell.Description description = null;
    public List<ILangStringTextType> description = null;

    [XmlElement(ElementName = "idShort")]
    [JsonIgnore]
    public string idShort = "";

    [XmlElement(ElementName = "identification")]
    [JsonIgnore]
    public string identification = null;

    [XmlElement(ElementName = "semanticId")]
    public Reference semanticId = null;

    [XmlElement(ElementName = "endpoints")]
    public List<AASxEndpoint> endpoints = new List<AASxEndpoint>();
}