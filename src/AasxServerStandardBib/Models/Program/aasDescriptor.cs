namespace AasxServerStandardBib.Models.Program;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Xml.Serialization;

/* AAS Descriptor Definiton */
public class aasDescriptor
{
    [XmlElement(ElementName = "administration")]
    [JsonIgnore]
    public AdministrativeInformation administration = null;

    [XmlElement(ElementName = "description")]
    [JsonIgnore]
    public List<ILangStringTextType> description = new(new List<ILangStringTextType>());

    [XmlElement(ElementName = "idShort")] public string idShort = "";

    [XmlElement(ElementName = "identification")]
    [JsonIgnore]
    public string identification = null;

    [XmlElement(ElementName = "assets")] public List<AssetInformation> assets = new List<AssetInformation>();

    [XmlElement(ElementName = "endpoints")]
    public List<AASxEndpoint> endpoints = new List<AASxEndpoint>();

    [XmlElement(ElementName = "submodelDescriptors")]
    public List<SubmodelDescriptors> submodelDescriptors = new List<SubmodelDescriptors>();
}