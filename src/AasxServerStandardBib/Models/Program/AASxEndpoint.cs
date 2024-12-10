namespace AasxServerStandardBib.Models.Program;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

/* AAS Detail Part 2 Descriptor Definitions BEGIN*/
/* End Point Definition */
public class AASxEndpoint
{
    [XmlElement(ElementName = "address")] public string address = "";

    [XmlElement(ElementName = "type")] public string type = "";
}