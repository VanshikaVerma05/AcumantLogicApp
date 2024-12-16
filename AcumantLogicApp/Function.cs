
//using Microsoft.Azure.Functions.Worker;
//using Microsoft.Extensions.Logging;
//using Microsoft.Extensions.Configuration;
//using Newtonsoft.Json;
//using CsvHelper;
//using System.Globalization;
//using System.IO;
//using System.Collections.Generic;
//using System.Threading.Tasks;
//using System.Xml;
//using System;
//using System.Collections.Generic;
//using System.IO;
//using System.Linq;


//public class Function
//{
//    private readonly ILogger _logger;
//    private readonly IConfiguration _configuration;

//    public Function(ILoggerFactory loggerFactory, IConfiguration configuration)
//    {
//        _logger = loggerFactory.CreateLogger<BlobTriggeredFunction>();
//        _configuration = configuration;
//    }

//    [Function("BlobTriggerFunction")]
//    public async Task RunAsync([BlobTrigger("csvfiles-01/{name}", Connection = "AzureWebJobsStorage")] Stream blobStream, string name)
//    {
//        _logger.LogInformation($"C# Blob trigger function processed blob\n Name: {name} \n Size: {blobStream.Length} Bytes");


//        var records = new List<Dictionary<string, string>>();
//        using (var reader = new StreamReader(blobStream))
//        using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
//        {
//            var csvRecords = csv.GetRecords<dynamic>();
//            foreach (var record in csvRecords)
//            {
//                var recordDict = new Dictionary<string, string>();
//                foreach (var property in record)
//                {
//                    recordDict.Add(property.Key, property.Value?.ToString() ?? string.Empty);
//                }
//                records.Add(recordDict);
//            }
//        }


//        var mappingConfig = await LoadMappingConfigAsync();

        
//        var xmlDoc = CreateXmlFromCsv(records, mappingConfig);


//        string xmlContent = xmlDoc.OuterXml;
//        _logger.LogInformation($"Transformed XML:\n{xmlContent}");
//    }

//    private async Task<Dictionary<string, List<Mapping>>> LoadMappingConfigAsync()
//    {
//        var mappingFilePath = _configuration["MappingFilePath"]; // Ensure this is correctly set in your settings

//        try
//        {
//            using (var stream = new FileStream(mappingFilePath, FileMode.Open))
//            using (var reader = new StreamReader(stream))
//            {
//                var json = await reader.ReadToEndAsync();


//                var mappings = JsonConvert.DeserializeObject<MappingFile>(json);


//                var groupedMappings = mappings?.Mappings
//                    .GroupBy(m => m.TermOfPayment)
//                    .ToDictionary(g => g.Key, g => g.ToList());

//                return groupedMappings ?? new Dictionary<string, List<Mapping>>();
//            }
//        }
//        catch (Exception ex)
//        {
//            _logger.LogError($"Error loading mapping config: {ex.Message}");
//            throw;
//        }
//    }

//    private XmlDocument CreateXmlFromCsv(List<Dictionary<string, string>> records, Dictionary<string, List<Mapping>> mappingConfig)
//    {
//        var xmlDoc = new XmlDocument();
//        XmlDeclaration xmlDeclaration = xmlDoc.CreateXmlDeclaration("1.0", "UTF-8", null);
//        xmlDoc.AppendChild(xmlDeclaration);

//        XmlElement rootElement = xmlDoc.CreateElement("Payments");
//        xmlDoc.AppendChild(rootElement);

//        foreach (var record in records)
//        {
//            XmlElement paymentElement = xmlDoc.CreateElement("Payment");
//            rootElement.AppendChild(paymentElement);

//            string dataValue = record.ContainsKey("Data_value") ? record["Data_value"] : string.Empty;
//            string period = record.ContainsKey("Period") ? record["Period"] : string.Empty;
//            string subject = record.ContainsKey("Subject") ? record["Subject"] : string.Empty;
//            string termOfPayment = record.ContainsKey("Term Of Payment") ? record["Term Of Payment"] : string.Empty;
//            string methodOfPayment = record.ContainsKey("Method Of Payment") ? record["Method Of Payment"] : string.Empty;
//            string status = record.ContainsKey("STATUS") ? record["STATUS"] : string.Empty;

//            _logger.LogInformation($"Extracted Data: DataValue={dataValue}, Period={period}, Subject={subject}, TermOfPayment={termOfPayment}, MethodOfPayment={methodOfPayment}");


//            string transformedTermOfPayment = termOfPayment;
//            string transformedMethodOfPayment = methodOfPayment;

//            if (status == "F")
//            {
//                transformedTermOfPayment = TransformField("TermOfPayment", termOfPayment, methodOfPayment, mappingConfig);
//                transformedMethodOfPayment = TransformField("MethodOfPayment", methodOfPayment, termOfPayment, mappingConfig);
//            }

//            AddXmlElement(xmlDoc, paymentElement, "DataValue", dataValue);
//            AddXmlElement(xmlDoc, paymentElement, "Period", period);
//            AddXmlElement(xmlDoc, paymentElement, "Subject", subject);
//            AddXmlElement(xmlDoc, paymentElement, "TermOfPayment", transformedTermOfPayment);
//            AddXmlElement(xmlDoc, paymentElement, "MethodOfPayment", transformedMethodOfPayment);
//        }

//        return xmlDoc;
//    }

//    private string TransformField(string fieldName, string fieldValue, string otherFieldValue, Dictionary<string, List<Mapping>> mappingConfig)
//    {

//        foreach (var mappingEntry in mappingConfig)
//        {

//            var matchingMapping = mappingEntry.Value.FirstOrDefault(m =>
//                (fieldName == "TermOfPayment" && m.TermOfPayment == fieldValue && m.MethodOfPayment == otherFieldValue) ||
//                (fieldName == "MethodOfPayment" && m.MethodOfPayment == fieldValue && m.TermOfPayment == otherFieldValue)
//            );


//            if (matchingMapping != null)
//            {

//                if (fieldName == "TermOfPayment")
//                    return matchingMapping.MappedTerm;
//                if (fieldName == "MethodOfPayment")
//                    return matchingMapping.MappedMethod;
//            }
//        }


//        return fieldValue;
//    }

//    private void AddXmlElement(XmlDocument xmlDoc, XmlElement parentElement, string elementName, string value)
//    {
//        XmlElement element = xmlDoc.CreateElement(elementName);
//        element.InnerText = value;
//        parentElement.AppendChild(element);
//    }
//}

//public class MappingFile
//{
//    public List<Mapping> Mappings { get; set; }
//}

//public class Mapping
//{
//    public string TermOfPayment { get; set; }
//    public string MethodOfPayment { get; set; }
//    public string MappedTerm { get; set; }
//    public string MappedMethod { get; set; }
//}
